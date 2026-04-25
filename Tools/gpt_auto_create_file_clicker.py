import os
import select
import sys
import time
from collections import deque

if os.geteuid() == 0:
    print("Не запускай этот скрипт через sudo.")
    print("AT-SPI должен работать от обычного пользователя, иначе Firefox не будет виден.")
    print()
    print("Сначала выдай права:")
    print("  sudo modprobe uinput")
    print("  sudo setfacl -m u:$USER:rw /dev/uinput")
    print("  for dev in /dev/input/event*; do sudo setfacl -m u:$USER:rw \"$dev\"; done")
    print()
    print("Потом запускай:")
    print("  /usr/bin/python3 Tools/gpt_auto_create_file_clicker.py")
    sys.exit(1)

try:
    import pyatspi
except ImportError:
    print("Не найден pyatspi.")
    print("Установи:")
    print("  sudo dnf install python3-pyatspi at-spi2-core")
    print()
    print("Запускай:")
    print("  /usr/bin/python3 Tools/gpt_auto_create_file_clicker.py")
    sys.exit(1)

try:
    from evdev import InputDevice, UInput, ecodes as e, list_devices
except ImportError:
    print("Не найден evdev.")
    print("Установи:")
    print("  sudo dnf install python3-evdev")
    sys.exit(1)


TARGET_BUTTON_TEXTS = [
    "Create File",
    "Create file",
    "CreateFile",

    "Create or Update File",
    "Create or update file",
    "Create Or Update File",
    "CreateOrUpdateFile",
    "Create / Update File",
    "Create/update file",
    "Create Update File",
    "CreateUpdateFile",
    "Create or Update",
    "Create or update",
    "Create/update",
    "Create / Update",
    "CreateUpdate",

    "Update File",
    "Update file",
    "UpdateFile",

    "Delete File",
    "Delete file",
    "DeleteFile",

    "Rename File",
    "Rename file",
    "RenameFile",

    "Move File",
    "Move file",
    "MoveFile",

    "Overwrite File",
    "Overwrite file",
    "OverwriteFile",

    "Apply Patch",
    "Apply patch",
    "ApplyPatch",

    "Commit Changes",
    "Commit changes",
    "CommitChanges",

    "Accept Changes",
    "Accept changes",
    "AcceptChanges",

    "Save",
    "Submit",
    "Confirm",
    "Continue",
]

SCAN_INTERVAL_SECONDS = 0.35
CLICK_COOLDOWN_SECONDS = 1.5

SCROLL_INTERVAL_SECONDS = 0.70
SCROLL_STEPS_PER_TICK = 1

MAX_SEARCH_DEPTH = 40

STOP_KEY = e.KEY_Q

PRINT_VISIBLE_BUTTONS = False
PRINT_NOT_FOUND_EVERY_SECONDS = 3.0


def normalize_text(value):
    if not value:
        return ""

    return "".join(ch for ch in value.lower() if ch.isalnum())


NORMALIZED_TARGETS = [
    normalize_text(text)
    for text in TARGET_BUTTON_TEXTS
]


def get_role_name(obj):
    try:
        return obj.getRoleName().lower()
    except Exception:
        return ""


def get_object_text(obj):
    parts = []

    try:
        if obj.name:
            parts.append(obj.name)
    except Exception:
        pass

    try:
        if obj.description:
            parts.append(obj.description)
    except Exception:
        pass

    return " ".join(parts)


def has_state(obj, state_name):
    state_value = getattr(pyatspi, state_name, None)

    if state_value is None:
        return True

    try:
        return obj.getState().contains(state_value)
    except Exception:
        return False


def is_visible_and_enabled(obj):
    required_states = [
        "STATE_SHOWING",
        "STATE_VISIBLE",
        "STATE_SENSITIVE",
        "STATE_ENABLED",
    ]

    for state_name in required_states:
        if not has_state(obj, state_name):
            return False

    return True


def is_button(obj):
    role_name = get_role_name(obj)
    return "button" in role_name


def get_children(obj):
    try:
        child_count = obj.childCount
    except Exception:
        return []

    children = []

    for index in range(child_count):
        try:
            child = obj.getChildAtIndex(index)
        except Exception:
            continue

        if child is not None:
            children.append(child)

    return children


def iter_accessibility_tree():
    try:
        desktop = pyatspi.Registry.getDesktop(0)
    except Exception as ex:
        print(f"Не удалось получить accessibility desktop: {ex}")
        return

    queue = deque()

    for child in get_children(desktop):
        queue.append((child, 0))

    while queue:
        obj, depth = queue.popleft()
        yield obj, depth

        if depth >= MAX_SEARCH_DEPTH:
            continue

        for child in get_children(obj):
            queue.append((child, depth + 1))


def get_matching_target(obj):
    if not is_button(obj):
        return None

    if not is_visible_and_enabled(obj):
        return None

    object_text = get_object_text(obj)
    normalized_object_text = normalize_text(object_text)

    if not normalized_object_text:
        return None

    for original_target, normalized_target in zip(TARGET_BUTTON_TEXTS, NORMALIZED_TARGETS):
        if normalized_target in normalized_object_text:
            return original_target

    return None


def find_target_button():
    visible_buttons = []

    for obj, _ in iter_accessibility_tree():
        if is_button(obj) and is_visible_and_enabled(obj):
            text = get_object_text(obj)

            if text:
                visible_buttons.append(text)

        matching_target = get_matching_target(obj)

        if matching_target is not None:
            return obj, matching_target

    if PRINT_VISIBLE_BUTTONS and visible_buttons:
        print("Видимые кнопки:")
        for button_text in sorted(set(visible_buttons)):
            print(f"  - {button_text}")

    return None, None


def click_accessible_object(obj):
    try:
        action = obj.queryAction()
    except Exception:
        return False

    preferred_actions = [
        "click",
        "press",
        "activate",
    ]

    try:
        action_count = action.nActions
    except Exception:
        return False

    for preferred_action in preferred_actions:
        for index in range(action_count):
            try:
                action_name = action.getName(index).lower()
            except Exception:
                continue

            if preferred_action in action_name:
                try:
                    return bool(action.doAction(index))
                except Exception:
                    return False

    if action_count > 0:
        try:
            return bool(action.doAction(0))
        except Exception:
            return False

    return False


def find_keyboard_devices():
    keyboards = []

    for path in list_devices():
        try:
            device = InputDevice(path)
            capabilities = device.capabilities()
        except PermissionError:
            raise
        except Exception:
            continue

        key_capabilities = capabilities.get(e.EV_KEY, [])

        if STOP_KEY in key_capabilities:
            keyboards.append(device)
        else:
            device.close()

    return keyboards


def should_stop_from_keyboard(keyboards):
    if not keyboards:
        return False

    readable_devices, _, _ = select.select(keyboards, [], [], 0)

    for device in readable_devices:
        try:
            events = device.read()
        except OSError:
            continue

        for event in events:
            if event.type != e.EV_KEY:
                continue

            if event.code == STOP_KEY and event.value == 1:
                return True

    return False


def create_scroll_device():
    capabilities = {
        e.EV_REL: [
            e.REL_WHEEL,
        ],
    }

    return UInput(capabilities, name="gpt-auto-scroll-wheel")


def scroll_down(ui):
    for _ in range(SCROLL_STEPS_PER_TICK):
        ui.write(e.EV_REL, e.REL_WHEEL, -1)
        ui.syn()


def print_permission_help():
    print()
    print("Не хватает прав на /dev/uinput или /dev/input/event*.")
    print("Выполни:")
    print("  sudo modprobe uinput")
    print("  sudo setfacl -m u:$USER:rw /dev/uinput")
    print("  for dev in /dev/input/event*; do sudo setfacl -m u:$USER:rw \"$dev\"; done")
    print()
    print("Потом запускай БЕЗ sudo:")
    print("  /usr/bin/python3 Tools/gpt_auto_create_file_clicker.py")
    print()


def main():
    print("Автокликер кнопок + автопрокрутка запущен.")
    print("Рабочая схема:")
    print("  1. ищет кнопку через Firefox accessibility")
    print("  2. если нашёл — кликает")
    print("  3. если не нашёл — крутит колесо вниз")
    print("  4. q останавливает скрипт глобально")
    print()

    print("Ищу кнопки:")
    for text in TARGET_BUTTON_TEXTS:
        print(f"  - {text}")

    print()
    print("Наведи курсор на Firefox/страницу, чтобы прокрутка шла в браузер.")
    print("Нажми q где угодно, чтобы остановить.")
    print()

    try:
        keyboards = find_keyboard_devices()
    except PermissionError:
        print_permission_help()
        sys.exit(1)

    if not keyboards:
        print("Не нашёл клавиатуру через evdev.")
        print_permission_help()
        sys.exit(1)

    print("Найдены устройства клавиатуры:")
    for keyboard in keyboards:
        print(f"  - {keyboard.path}: {keyboard.name}")

    print()

    last_click_time = 0
    last_scroll_time = 0
    last_not_found_print = 0

    try:
        with create_scroll_device() as ui:
            while True:
                if should_stop_from_keyboard(keyboards):
                    print("Остановлено по q.")
                    return

                button, target_name = find_target_button()
                now = time.time()

                if button is not None:
                    if now - last_click_time >= CLICK_COOLDOWN_SECONDS:
                        actual_text = get_object_text(button)

                        print(f"Найдена кнопка: {actual_text!r}")
                        print(f"Совпала с целью: {target_name!r}")
                        print("Кликаю...")

                        clicked = click_accessible_object(button)

                        if clicked:
                            print("Клик выполнен.")
                        else:
                            print("Кнопка найдена, но действие click/press/activate не сработало.")

                        last_click_time = now

                    time.sleep(SCAN_INTERVAL_SECONDS)
                    continue

                if now - last_not_found_print >= PRINT_NOT_FOUND_EVERY_SECONDS:
                    print("Поддерживаемые кнопки пока не найдены. Листаю вниз...")
                    last_not_found_print = now

                if now - last_scroll_time >= SCROLL_INTERVAL_SECONDS:
                    scroll_down(ui)
                    last_scroll_time = now

                time.sleep(SCAN_INTERVAL_SECONDS)

    except PermissionError:
        print_permission_help()
        sys.exit(1)

    except KeyboardInterrupt:
        print("\nОстановлено через Ctrl+C.")

    finally:
        for keyboard in keyboards:
            try:
                keyboard.close()
            except Exception:
                pass


if __name__ == "__main__":
    main()
