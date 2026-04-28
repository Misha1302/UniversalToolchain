#!/usr/bin/env python3
import argparse
import csv
import os
import shutil
import struct
import subprocess
import sys
import tempfile
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Optional

try:
    from evdev import AbsInfo, UInput, ecodes as e
except ImportError:
    AbsInfo = None
    UInput = None
    e = None


SAFE_TARGET_BUTTON_TEXTS = [
    "Create new file",
    "Create New File",
    "Create file",
    "Create File",
    "CreateFile",

    "Create or update file",
    "Create or Update File",
    "Create Or Update File",
    "Create/update file",
    "Create / Update File",
    "Create update file",
    "CreateUpdateFile",
    "CreateOrUpdateFile",

    "Create/update PR",
    "Create / Update PR",
    "Create or Update PR",
    "Create or update PR",
    "CreateOrUpdatePR",
    "CreateUpdatePR",

    "Create/update Pull Request",
    "Create / Update Pull Request",
    "Create or Update Pull Request",
    "Create or update pull request",
    "CreateOrUpdatePullRequest",
    "CreateUpdatePullRequest",

    "Create PR",
    "Create pr",
    "CreatePR",
    "Create Pull Request",
    "Create pull request",
    "CreatePullRequest",

    "Update PR",
    "Update pr",
    "UpdatePR",
    "Update Pull Request",
    "Update pull request",
    "UpdatePullRequest",

    "Update file",
    "Update File",
    "UpdateFile",

    "Apply patch",
    "Apply Patch",
    "ApplyPatch",

    "Commit changes",
    "Commit Changes",
    "CommitChanges",

    "Propose changes",
    "Propose Changes",
    "ProposeChanges",

    "Compare & pull request",
    "Compare and pull request",
    "Compare Pull Request",

    "Create branch",
    "Create Branch",
    "Create new branch",
    "Create a new branch",

    "Accept changes",
    "Accept Changes",
    "AcceptChanges",

    "Save",
    "Submit",
    "Confirm",
    "Continue",
]

DANGEROUS_TARGET_BUTTON_TEXTS = [
    "Delete file",
    "Delete File",
    "DeleteFile",
    "Rename file",
    "Rename File",
    "RenameFile",
    "Move file",
    "Move File",
    "MoveFile",
    "Overwrite file",
    "Overwrite File",
    "OverwriteFile",
]

SCREENSHOT_COMMANDS = [
    ["gnome-screenshot", "-f"],
    ["grim"],
    ["spectacle", "-b", "-n", "-o"],
    ["maim"],
    ["scrot"],
    ["import", "-window", "root"],
]

LEFT_CLICK_CODES = ["0xC0", "0x110"]
WHEEL_DOWN_CODES = ["0xC5", "0x115"]

DEFAULT_SCAN_INTERVAL_SECONDS = 0.55
DEFAULT_AFTER_CLICK_SLEEP_SECONDS = 1.25
DEFAULT_NOT_FOUND_PRINT_SECONDS = 3.0
DEFAULT_SAME_TARGET_COOLDOWN_SECONDS = 8.0
DEFAULT_MIN_CONFIDENCE = 35.0
DEFAULT_MIN_RATIO = 0.78
DEFAULT_MAX_WINDOW_WORDS = 7


@dataclass(frozen=True)
class OcrWord:
    text: str
    normalized_text: str
    confidence: float
    left: int
    top: int
    width: int
    height: int
    line_key: tuple[int, int, int, int]
    word_number: int

    @property
    def right(self) -> int:
        return self.left + self.width

    @property
    def bottom(self) -> int:
        return self.top + self.height


@dataclass(frozen=True)
class TextMatch:
    target_text: str
    matched_text: str
    confidence: float
    ratio: float
    left: int
    top: int
    right: int
    bottom: int

    @property
    def center_x(self) -> int:
        return (self.left + self.right) // 2

    @property
    def center_y(self) -> int:
        return (self.top + self.bottom) // 2

    @property
    def normalized_target(self) -> str:
        return normalize_text(self.target_text)


@dataclass
class RecentClick:
    normalized_target: str
    x: int
    y: int
    clicked_at: float


def normalize_text(value: str) -> str:
    if not value:
        return ""

    replacements = {
        "0": "o",
        "1": "l",
        "|": "l",
        "ё": "е",
    }

    lowered = value.lower()
    normalized_chars = []

    for ch in lowered:
        normalized_chars.append(replacements.get(ch, ch))

    return "".join(ch for ch in normalized_chars if ch.isalnum())


def similarity(left: str, right: str) -> float:
    if not left or not right:
        return 0.0

    if left in right or right in left:
        smaller = min(len(left), len(right))
        larger = max(len(left), len(right))
        return max(0.88, smaller / larger)

    # Small local implementation to avoid an extra dependency.
    previous = list(range(len(right) + 1))

    for i, left_char in enumerate(left, 1):
        current = [i]

        for j, right_char in enumerate(right, 1):
            insert_cost = current[j - 1] + 1
            delete_cost = previous[j] + 1
            replace_cost = previous[j - 1] + (left_char != right_char)
            current.append(min(insert_cost, delete_cost, replace_cost))

        previous = current

    distance = previous[-1]
    return 1.0 - distance / max(len(left), len(right))


def command_exists(command: str) -> bool:
    return shutil.which(command) is not None


def run_checked(command: list[str], timeout: float = 10.0) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        command,
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        timeout=timeout,
    )


def take_screenshot(output_path: Path) -> str:
    errors = []

    for command_template in SCREENSHOT_COMMANDS:
        binary = command_template[0]

        if not command_exists(binary):
            continue

        command = [*command_template, str(output_path)]

        try:
            run_checked(command, timeout=12.0)
        except Exception as ex:
            errors.append(f"{' '.join(command)} -> {ex}")
            continue

        if output_path.exists() and output_path.stat().st_size > 0:
            return binary

    print("Не удалось сделать скриншот.")
    print("Поставь хотя бы один инструмент:")
    print("  sudo dnf install gnome-screenshot")
    print("или:")
    print("  sudo dnf install grim")

    if errors:
        print()
        print("Ошибки попыток:")
        for error in errors:
            print(f"  - {error}")

    sys.exit(1)


def read_png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as file:
        header = file.read(24)

    if len(header) < 24 or header[:8] != b"\x89PNG\r\n\x1a\n":
        return 0, 0

    width, height = struct.unpack(">II", header[16:24])
    return int(width), int(height)


def run_tesseract_tsv(image_path: Path, language: str, psm: int) -> str:
    if not command_exists("tesseract"):
        print("Не найден tesseract.")
        print("Установи:")
        print("  sudo dnf install tesseract tesseract-langpack-eng tesseract-langpack-rus")
        sys.exit(1)

    command = [
        "tesseract",
        str(image_path),
        "stdout",
        "-l",
        language,
        "--psm",
        str(psm),
        "tsv",
    ]

    try:
        result = run_checked(command, timeout=20.0)
        return result.stdout
    except subprocess.CalledProcessError as ex:
        print("Tesseract не смог распознать скриншот.")
        print(ex.stderr.strip())
        sys.exit(1)
    except subprocess.TimeoutExpired:
        print("Tesseract слишком долго распознавал скриншот.")
        sys.exit(1)


def parse_confidence(value: str) -> float:
    try:
        return float(value)
    except Exception:
        return -1.0


def parse_int(value: str) -> int:
    try:
        return int(float(value))
    except Exception:
        return 0


def parse_tsv_words(tsv_text: str, min_confidence: float) -> list[OcrWord]:
    words = []
    reader = csv.DictReader(tsv_text.splitlines(), delimiter="\t")

    for row in reader:
        text = (row.get("text") or "").strip()

        if not text:
            continue

        confidence = parse_confidence(row.get("conf") or "-1")

        if confidence < min_confidence:
            continue

        normalized = normalize_text(text)

        if not normalized:
            continue

        line_key = (
            parse_int(row.get("page_num") or "0"),
            parse_int(row.get("block_num") or "0"),
            parse_int(row.get("par_num") or "0"),
            parse_int(row.get("line_num") or "0"),
        )

        word = OcrWord(
            text=text,
            normalized_text=normalized,
            confidence=confidence,
            left=parse_int(row.get("left") or "0"),
            top=parse_int(row.get("top") or "0"),
            width=parse_int(row.get("width") or "0"),
            height=parse_int(row.get("height") or "0"),
            line_key=line_key,
            word_number=parse_int(row.get("word_num") or "0"),
        )
        words.append(word)

    return words


def group_words_by_line(words: Iterable[OcrWord]) -> list[list[OcrWord]]:
    lines_by_key: dict[tuple[int, int, int, int], list[OcrWord]] = {}

    for word in words:
        lines_by_key.setdefault(word.line_key, []).append(word)

    lines = []

    for line_words in lines_by_key.values():
        ordered = sorted(line_words, key=lambda word: (word.word_number, word.left))
        lines.append(ordered)

    lines.sort(key=lambda line: (min(word.top for word in line), min(word.left for word in line)))
    return lines


def build_match(target_text: str, line_words: list[OcrWord], start: int, end: int, ratio: float) -> TextMatch:
    matched_words = line_words[start:end]
    left = min(word.left for word in matched_words)
    top = min(word.top for word in matched_words)
    right = max(word.right for word in matched_words)
    bottom = max(word.bottom for word in matched_words)
    confidence = sum(word.confidence for word in matched_words) / len(matched_words)
    matched_text = " ".join(word.text for word in matched_words)

    return TextMatch(
        target_text=target_text,
        matched_text=matched_text,
        confidence=confidence,
        ratio=ratio,
        left=left,
        top=top,
        right=right,
        bottom=bottom,
    )


def find_text_matches(
    words: list[OcrWord],
    target_texts: list[str],
    min_ratio: float,
    max_window_words: int,
) -> list[TextMatch]:
    matches = []
    normalized_targets = [(target, normalize_text(target)) for target in target_texts]
    lines = group_words_by_line(words)

    for line_words in lines:
        if not line_words:
            continue

        line_count = len(line_words)
        max_end_distance = min(max_window_words, line_count)

        for start in range(line_count):
            for length in range(1, max_end_distance + 1):
                end = start + length

                if end > line_count:
                    continue

                candidate_norm = "".join(word.normalized_text for word in line_words[start:end])

                if not candidate_norm:
                    continue

                for target_text, target_norm in normalized_targets:
                    if not target_norm:
                        continue

                    ratio = similarity(candidate_norm, target_norm)

                    if ratio >= min_ratio:
                        matches.append(build_match(target_text, line_words, start, end, ratio))

    return deduplicate_matches(matches)


def deduplicate_matches(matches: list[TextMatch]) -> list[TextMatch]:
    best_by_box: dict[tuple[int, int, int, int, str], TextMatch] = {}

    for match in matches:
        box_key = (
            match.left // 8,
            match.top // 8,
            match.right // 8,
            match.bottom // 8,
            match.normalized_target,
        )
        current = best_by_box.get(box_key)

        if current is None:
            best_by_box[box_key] = match
            continue

        current_score = (current.ratio, current.confidence, len(current.matched_text))
        new_score = (match.ratio, match.confidence, len(match.matched_text))

        if new_score > current_score:
            best_by_box[box_key] = match

    return list(best_by_box.values())


def is_recently_clicked(match: TextMatch, recent_clicks: list[RecentClick], cooldown_seconds: float) -> bool:
    now = time.time()

    for recent_click in recent_clicks:
        if now - recent_click.clicked_at > cooldown_seconds:
            continue

        if recent_click.normalized_target != match.normalized_target:
            continue

        dx = abs(recent_click.x - match.center_x)
        dy = abs(recent_click.y - match.center_y)

        if dx <= 35 and dy <= 25:
            return True

    return False


def prune_recent_clicks(recent_clicks: list[RecentClick], cooldown_seconds: float) -> list[RecentClick]:
    now = time.time()
    return [click for click in recent_clicks if now - click.clicked_at <= cooldown_seconds]


def choose_match(matches: list[TextMatch], mode: str) -> Optional[TextMatch]:
    if not matches:
        return None

    if mode == "topmost":
        return min(matches, key=lambda match: (match.center_y, match.center_x, -match.ratio, -match.confidence))

    if mode == "best":
        return max(matches, key=lambda match: (match.ratio, match.confidence, match.center_y))

    return max(matches, key=lambda match: (match.center_y, match.ratio, match.confidence, match.center_x))


def print_matches(matches: list[TextMatch], limit: int = 12) -> None:
    if not matches:
        print("OCR не нашёл подходящих текстов на экране.")
        return

    print("OCR нашёл совпадения:")
    ordered = sorted(matches, key=lambda match: (match.center_y, match.center_x))

    for match in ordered[-limit:]:
        print(
            "  - "
            f"{match.matched_text!r} -> target={match.target_text!r}, "
            f"box=({match.left},{match.top})-({match.right},{match.bottom}), "
            f"center=({match.center_x},{match.center_y}), "
            f"conf={match.confidence:.1f}, ratio={match.ratio:.2f}"
        )


def try_ydotool_click(x: int, y: int) -> bool:
    if not command_exists("ydotool"):
        return False

    move_commands = [
        ["ydotool", "mousemove", "--absolute", str(x), str(y)],
        ["ydotool", "mousemove", "-a", str(x), str(y)],
    ]

    move_ok = False
    last_error = None

    for command in move_commands:
        try:
            run_checked(command, timeout=3.0)
            move_ok = True
            break
        except Exception as ex:
            last_error = ex

    if not move_ok:
        print(f"ydotool не смог передвинуть мышь в абсолютные координаты: {last_error}")
        return False

    for code in LEFT_CLICK_CODES:
        try:
            run_checked(["ydotool", "click", code], timeout=3.0)
            return True
        except Exception:
            continue

    return False


def try_ydotool_scroll_down() -> bool:
    if not command_exists("ydotool"):
        return False

    for code in WHEEL_DOWN_CODES:
        try:
            run_checked(["ydotool", "click", code], timeout=3.0)
            return True
        except Exception:
            continue

    return False


def try_ydotool_page_down() -> bool:
    if not command_exists("ydotool"):
        return False

    # Linux input key code for PageDown is 109.
    try:
        run_checked(["ydotool", "key", "109:1", "109:0"], timeout=3.0)
        return True
    except Exception:
        return False


def create_uinput_device(width: int, height: int) -> Optional[UInput]:
    if UInput is None or e is None:
        return None

    capabilities = {
        e.EV_KEY: [
            e.BTN_LEFT,
            e.KEY_F5,
            e.KEY_PAGEDOWN,
        ],
        e.EV_REL: [
            e.REL_WHEEL,
        ],
    }

    if AbsInfo is not None and width > 0 and height > 0:
        capabilities[e.EV_ABS] = [
            (e.ABS_X, AbsInfo(value=0, min=0, max=max(1, width - 1), fuzz=0, flat=0, resolution=0)),
            (e.ABS_Y, AbsInfo(value=0, min=0, max=max(1, height - 1), fuzz=0, flat=0, resolution=0)),
        ]

    try:
        return UInput(capabilities, name="gpt-ocr-text-clicker")
    except PermissionError:
        print("Нет прав на /dev/uinput для запасного способа клика/прокрутки.")
        print("Выполни:")
        print("  sudo modprobe uinput")
        print("  sudo setfacl -m u:$USER:rw /dev/uinput")
        return None
    except Exception as ex:
        print(f"Не удалось создать виртуальное input-устройство: {ex}")
        return None


def uinput_click(ui: Optional[UInput], x: int, y: int) -> bool:
    if ui is None or e is None:
        return False

    try:
        ui.write(e.EV_ABS, e.ABS_X, x)
        ui.write(e.EV_ABS, e.ABS_Y, y)
        ui.syn()
        time.sleep(0.05)
        ui.write(e.EV_KEY, e.BTN_LEFT, 1)
        ui.syn()
        time.sleep(0.06)
        ui.write(e.EV_KEY, e.BTN_LEFT, 0)
        ui.syn()
        return True
    except Exception as ex:
        print(f"Запасной UInput-клик не сработал: {ex}")
        return False


def uinput_scroll_down(ui: Optional[UInput], steps: int) -> bool:
    if ui is None or e is None:
        return False

    try:
        for _ in range(steps):
            ui.write(e.EV_REL, e.REL_WHEEL, -1)
            ui.syn()
            time.sleep(0.03)
        return True
    except Exception:
        return False


def uinput_page_down(ui: Optional[UInput]) -> bool:
    if ui is None or e is None:
        return False

    try:
        ui.write(e.EV_KEY, e.KEY_PAGEDOWN, 1)
        ui.syn()
        time.sleep(0.05)
        ui.write(e.EV_KEY, e.KEY_PAGEDOWN, 0)
        ui.syn()
        return True
    except Exception:
        return False


def uinput_press_f5(ui: Optional[UInput]) -> bool:
    if ui is None or e is None:
        return False

    try:
        ui.write(e.EV_KEY, e.KEY_F5, 1)
        ui.syn()
        time.sleep(0.05)
        ui.write(e.EV_KEY, e.KEY_F5, 0)
        ui.syn()
        return True
    except Exception:
        return False


def click_at(ui: Optional[UInput], x: int, y: int) -> bool:
    if try_ydotool_click(x, y):
        return True

    return uinput_click(ui, x, y)


def scroll_down(ui: Optional[UInput], wheel_steps: int) -> None:
    ydotool_ok = try_ydotool_scroll_down()
    uinput_ok = uinput_scroll_down(ui, wheel_steps)
    page_down_ok = try_ydotool_page_down() or uinput_page_down(ui)

    if not ydotool_ok and not uinput_ok and not page_down_ok:
        print("Не смог пролистать страницу. Наведи фокус на браузер или листай вручную.")


def press_f5(ui: Optional[UInput]) -> None:
    if command_exists("ydotool"):
        try:
            # Linux input key code for F5 is 63.
            run_checked(["ydotool", "key", "63:1", "63:0"], timeout=3.0)
            return
        except Exception:
            pass

    uinput_press_f5(ui)


def build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Скриншот -> OCR -> поиск текста кнопки -> клик по найденному тексту."
    )
    parser.add_argument(
        "--target",
        action="append",
        help="Добавить свой текст для поиска. Можно указать несколько раз.",
    )
    parser.add_argument(
        "--include-dangerous",
        action="store_true",
        help="Также искать Delete/Rename/Move/Overwrite. По умолчанию они отключены.",
    )
    parser.add_argument(
        "--lang",
        default="eng+rus",
        help="Языки Tesseract. По умолчанию eng+rus.",
    )
    parser.add_argument(
        "--psm",
        type=int,
        default=11,
        help="Page segmentation mode Tesseract. 11 обычно лучше для UI.",
    )
    parser.add_argument(
        "--min-confidence",
        type=float,
        default=DEFAULT_MIN_CONFIDENCE,
        help="Минимальная уверенность OCR для слова.",
    )
    parser.add_argument(
        "--min-ratio",
        type=float,
        default=DEFAULT_MIN_RATIO,
        help="Минимальная похожесть OCR-текста на цель.",
    )
    parser.add_argument(
        "--choose",
        choices=["bottommost", "topmost", "best"],
        default="bottommost",
        help="Как выбирать среди нескольких совпадений. По умолчанию bottommost.",
    )
    parser.add_argument(
        "--once",
        action="store_true",
        help="Сделать один цикл OCR и выйти.",
    )
    parser.add_argument(
        "--no-click",
        action="store_true",
        help="Только показать найденные совпадения, не кликать.",
    )
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Печатать все найденные совпадения и оставлять последний скриншот/tsv.",
    )
    parser.add_argument(
        "--scan-interval",
        type=float,
        default=DEFAULT_SCAN_INTERVAL_SECONDS,
        help="Пауза между циклами.",
    )
    parser.add_argument(
        "--after-click-sleep",
        type=float,
        default=DEFAULT_AFTER_CLICK_SLEEP_SECONDS,
        help="Пауза после клика.",
    )
    parser.add_argument(
        "--same-target-cooldown",
        type=float,
        default=DEFAULT_SAME_TARGET_COOLDOWN_SECONDS,
        help="Сколько секунд не кликать повторно в тот же текст/координаты.",
    )
    parser.add_argument(
        "--scroll-steps",
        type=int,
        default=3,
        help="Количество шагов колеса при отсутствии кнопки.",
    )
    parser.add_argument(
        "--refresh-seconds",
        type=float,
        default=0.0,
        help="Если > 0, нажимать F5 раз в N секунд.",
    )
    return parser


def collect_targets(args: argparse.Namespace) -> list[str]:
    targets = list(SAFE_TARGET_BUTTON_TEXTS)

    if args.include_dangerous:
        targets.extend(DANGEROUS_TARGET_BUTTON_TEXTS)

    if args.target:
        targets.extend(args.target)

    result = []
    seen = set()

    for target in targets:
        normalized = normalize_text(target)

        if not normalized or normalized in seen:
            continue

        result.append(target)
        seen.add(normalized)

    return result


def ensure_runtime_dependencies() -> None:
    if not command_exists("tesseract"):
        print("Не найден tesseract.")
        print("Установи:")
        print("  sudo dnf install tesseract tesseract-langpack-eng tesseract-langpack-rus")
        sys.exit(1)

    if not any(command_exists(command[0]) for command in SCREENSHOT_COMMANDS):
        print("Не найден инструмент для скриншота.")
        print("Установи:")
        print("  sudo dnf install gnome-screenshot")
        sys.exit(1)

    if not command_exists("ydotool") and UInput is None:
        print("Не найден ydotool и не установлен python3-evdev.")
        print("Установи:")
        print("  sudo dnf install ydotool python3-evdev")
        sys.exit(1)


def print_startup_info(targets: list[str], args: argparse.Namespace) -> None:
    print("OCR-кликер запущен.")
    print("Схема: скриншот -> Tesseract OCR -> поиск текста -> клик по координатам текста.")
    print(f"Выбор совпадения: {args.choose}.")
    print("Остановка: Ctrl+C.")
    print()
    print("Ищу тексты:")

    for target in targets:
        print(f"  - {target}")

    print()

    if command_exists("ydotool"):
        print("Клик: ydotool, fallback через /dev/uinput если доступен.")
    else:
        print("Клик: fallback через /dev/uinput. Для Wayland лучше установить ydotool.")

    print()


def run_one_cycle(
    targets: list[str],
    args: argparse.Namespace,
    ui: Optional[UInput],
    recent_clicks: list[RecentClick],
    temp_dir: Path,
) -> tuple[bool, list[RecentClick]]:
    screenshot_path = temp_dir / "screen.png"
    tsv_path = temp_dir / "ocr.tsv"

    screenshot_tool = take_screenshot(screenshot_path)
    tsv_text = run_tesseract_tsv(screenshot_path, args.lang, args.psm)

    if args.debug:
        tsv_path.write_text(tsv_text, encoding="utf-8")
        print(f"Скриншот: {screenshot_path}")
        print(f"OCR TSV: {tsv_path}")
        print(f"Скриншот сделан через: {screenshot_tool}")

    words = parse_tsv_words(tsv_text, args.min_confidence)
    matches = find_text_matches(
        words=words,
        target_texts=targets,
        min_ratio=args.min_ratio,
        max_window_words=DEFAULT_MAX_WINDOW_WORDS,
    )

    recent_clicks = prune_recent_clicks(recent_clicks, args.same_target_cooldown)
    matches = [
        match
        for match in matches
        if not is_recently_clicked(match, recent_clicks, args.same_target_cooldown)
    ]

    if args.debug:
        print_matches(matches)

    chosen = choose_match(matches, args.choose)

    if chosen is None:
        return False, recent_clicks

    print(
        f"Найден текст: {chosen.matched_text!r} "
        f"-> цель {chosen.target_text!r}, "
        f"центр=({chosen.center_x},{chosen.center_y}), "
        f"conf={chosen.confidence:.1f}, ratio={chosen.ratio:.2f}"
    )

    if args.no_click:
        return True, recent_clicks

    clicked = click_at(ui, chosen.center_x, chosen.center_y)

    if clicked:
        print("Клик выполнен.")
        recent_clicks.append(
            RecentClick(
                normalized_target=chosen.normalized_target,
                x=chosen.center_x,
                y=chosen.center_y,
                clicked_at=time.time(),
            )
        )
        time.sleep(args.after_click_sleep)
        return True, recent_clicks

    print("Текст найден, но клик не сработал.")
    print("Для Wayland обычно нужно:")
    print("  sudo dnf install ydotool")
    print("  systemctl --user start ydotoold")
    print("или права на /dev/uinput.")
    return True, recent_clicks


def main() -> None:
    args = build_arg_parser().parse_args()
    ensure_runtime_dependencies()
    targets = collect_targets(args)
    print_startup_info(targets, args)

    temp_context = tempfile.TemporaryDirectory(prefix="gpt-ocr-clicker-") if not args.debug else None
    temp_dir = Path(temp_context.name) if temp_context is not None else Path.cwd() / "gpt_ocr_clicker_debug"
    temp_dir.mkdir(parents=True, exist_ok=True)

    width = 0
    height = 0
    probe_path = temp_dir / "screen-probe.png"

    try:
        take_screenshot(probe_path)
        width, height = read_png_size(probe_path)
    except Exception:
        pass

    ui = create_uinput_device(width, height)
    recent_clicks: list[RecentClick] = []
    last_not_found_print = 0.0
    last_refresh = time.time()

    try:
        while True:
            now = time.time()

            if args.refresh_seconds > 0 and now - last_refresh >= args.refresh_seconds:
                print("Нажимаю F5 для обновления страницы...")
                press_f5(ui)
                last_refresh = now
                time.sleep(1.0)

            found, recent_clicks = run_one_cycle(
                targets=targets,
                args=args,
                ui=ui,
                recent_clicks=recent_clicks,
                temp_dir=temp_dir,
            )

            if args.once:
                return

            if not found:
                if now - last_not_found_print >= DEFAULT_NOT_FOUND_PRINT_SECONDS:
                    print("Подходящий текст не найден. Листаю вниз...")
                    last_not_found_print = now

                scroll_down(ui, args.scroll_steps)

            time.sleep(args.scan_interval)

    except KeyboardInterrupt:
        print("\nОстановлено через Ctrl+C.")
    finally:
        if ui is not None:
            ui.close()

        if temp_context is not None:
            temp_context.cleanup()


if __name__ == "__main__":
    main()
