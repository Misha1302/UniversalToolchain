#!/usr/bin/env python3

import time
import threading
import subprocess

import pyautogui
import keyboard


SCROLL_DOWN = -1
SCROLL_UP = 1

STEPS_IN_DOWN_DIRECTION = 20
STEPS_IN_UP_DIRECTION = 10

DELAY_AFTER_SCROLL = 0.1
DELAY_BETWEEN_STEPS = 0.5

REFRESH_INTERVAL_SECONDS = 90
WAIT_AFTER_F5_SECONDS = 1.0

AGGRESSIVE_SCROLL_DURATION_SECONDS = 5.0
AGGRESSIVE_SCROLL_AMOUNT = -10
AGGRESSIVE_SCROLL_DELAY = 0.02

stop_event = threading.Event()
action_lock = threading.Lock()


def enable_xhost():
    subprocess.run(["xhost", "+"], check=False)


def patch_pyautogui_do_not_move_mouse():
    """
    На Linux pyautogui перед кликом/скроллом может делать moveTo(x, y).
    Нам это не нужно: курсор должен оставаться там, где он реально сейчас.
    """

    def do_nothing_move_to(x=None, y=None):
        return None

    if hasattr(pyautogui, "platformModule") and hasattr(pyautogui.platformModule, "_moveTo"):
        pyautogui.platformModule._moveTo = do_nothing_move_to

    pyautogui._mouseMoveDrag = lambda *args, **kwargs: None


def stop_script():
    stop_event.set()


def sleep_or_stop(seconds: float):
    stop_event.wait(seconds)


def raw_scroll(amount: int):
    """
    Скролл без перемещения курсора.
    """
    pyautogui.platformModule._scroll(amount, None, None)


def raw_left_click():
    """
    Клик без перемещения курсора.
    """
    pyautogui.platformModule._click(None, None, "left")


def raw_f5():
    """
    Нажатие F5.
    """
    pyautogui.press("f5")
    time.sleep(5)


def aggressive_scroll_to_bottom():
    """
    Агрессивно листает вниз в течение AGGRESSIVE_SCROLL_DURATION_SECONDS.
    """
    end_time = time.monotonic() + AGGRESSIVE_SCROLL_DURATION_SECONDS

    while not stop_event.is_set() and time.monotonic() < end_time:
        raw_scroll(AGGRESSIVE_SCROLL_AMOUNT)
        time.sleep(AGGRESSIVE_SCROLL_DELAY)


def refresh_worker():
    """
    Раз в 90 секунд нажимает F5,
    затем агрессивно листает страницу вниз 5 секунд.
    """
    while not stop_event.is_set():
        if stop_event.wait(REFRESH_INTERVAL_SECONDS):
            return

        with action_lock:
            if stop_event.is_set():
                return

            print("F5 + агрессивная прокрутка вниз...")
            raw_f5()

            if stop_event.wait(WAIT_AFTER_F5_SECONDS):
                return

            aggressive_scroll_to_bottom()


def scroll_and_click(direction: int):
    steps = STEPS_IN_DOWN_DIRECTION if direction < 0 else STEPS_IN_UP_DIRECTION

    for _ in range(steps):
        if stop_event.is_set():
            return

        with action_lock:
            raw_scroll(direction)

        sleep_or_stop(DELAY_AFTER_SCROLL)

        if stop_event.is_set():
            return

        with action_lock:
            raw_left_click()

        sleep_or_stop(DELAY_BETWEEN_STEPS)


def main():
    enable_xhost()
    patch_pyautogui_do_not_move_mouse()

    keyboard.add_hotkey("q", stop_script)

    refresh_thread = threading.Thread(target=refresh_worker, daemon=True)
    refresh_thread.start()

    print("Запущено.")
    print("20 раз вниз: маленький скролл + клик.")
    print("Потом 10 раз вверх: маленький скролл + клик.")
    print("Раз в 90 секунд: F5, потом агрессивная прокрутка вниз 5 секунд.")
    print("Курсор НЕ должен возвращаться на старую позицию.")
    print("Остановить: q в любом месте.")

    try:
        while not stop_event.is_set():
            scroll_and_click(SCROLL_DOWN)

            if stop_event.is_set():
                break

            scroll_and_click(SCROLL_UP)

    except KeyboardInterrupt:
        pass
    finally:
        stop_event.set()
        keyboard.unhook_all()
        print("Остановлено.")


if __name__ == "__main__":
    main()
