#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
gpt_auto_create_file_clicker.py

Small desktop automation helper for Firefox/ChatGPT/Codex pages.

What it does:
- searches visible screen text for target buttons such as:
  "Create File", "Create file", "Create or update file",
  "Create/update PR", "Create PR", "Update PR";
- clicks the first matching visible button;
- scrolls the page when nothing is found;
- presses F5 every N seconds;
- exits when global `q` is pressed, when possible.

Dependencies:
    python -m pip install pyautogui pillow pytesseract pynput

System package for OCR:
    Fedora:
        sudo dnf install tesseract tesseract-langpack-eng
    Ubuntu/Debian:
        sudo apt install tesseract-ocr

Notes:
- Keep the browser window visible and focused.
- On Wayland, screenshots/global hotkeys may be restricted. X11 usually works better.
- This script intentionally does NOT use AT-SPI/accessibility APIs.
"""

from __future__ import annotations

import argparse
import os
import re
import select
import signal
import sys
import time
from dataclasses import dataclass
from difflib import SequenceMatcher
from typing import Iterable, Optional

try:
    import pyautogui
except ImportError as ex:
    raise SystemExit(
        "Missing dependency: pyautogui\n"
        "Install it with:\n"
        "  python -m pip install pyautogui pillow\n"
    ) from ex

try:
    import pytesseract
except ImportError as ex:
    raise SystemExit(
        "Missing dependency: pytesseract\n"
        "Install it with:\n"
        "  python -m pip install pytesseract\n"
        "Also install the system tesseract package."
    ) from ex

try:
    from PIL import ImageOps, ImageFilter
except ImportError as ex:
    raise SystemExit(
        "Missing dependency: pillow\n"
        "Install it with:\n"
        "  python -m pip install pillow\n"
    ) from ex


DEFAULT_BUTTONS: tuple[str, ...] = (
    "Create File",
    "Create file",
    "Create or update file",
    "Create/update file",
    "Create Update file",
    "Create or update PR",
    "Create/update PR",
    "Create update PR",
    "Create PR",
    "Update PR",
    "Create pull request",
    "Update pull request",
    "Create or update pull request",
)


@dataclass(frozen=True)
class OcrWord:
    text: str
    left: int
    top: int
    width: int
    height: int
    conf: float
    block_num: int
    par_num: int
    line_num: int

    @property
    def right(self) -> int:
        return self.left + self.width

    @property
    def bottom(self) -> int:
        return self.top + self.height


@dataclass(frozen=True)
class Match:
    phrase: str
    text: str
    score: float
    left: int
    top: int
    right: int
    bottom: int

    @property
    def center(self) -> tuple[int, int]:
        return ((self.left + self.right) // 2, (self.top + self.bottom) // 2)


class StopFlag:
    def __init__(self) -> None:
        self.stop_requested = False

    def request_stop(self) -> None:
        self.stop_requested = True


def normalize_text(value: str) -> str:
    value = value.lower()
    value = value.replace("/", " ")
    value = value.replace("-", " ")
    value = value.replace("_", " ")
    value = re.sub(r"[^a-zа-я0-9]+", " ", value, flags=re.IGNORECASE)
    value = re.sub(r"\s+", " ", value).strip()
    return value


def similarity(left: str, right: str) -> float:
    left_norm = normalize_text(left)
    right_norm = normalize_text(right)

    if not left_norm or not right_norm:
        return 0.0

    if left_norm in right_norm or right_norm in left_norm:
        return 1.0

    return SequenceMatcher(None, left_norm, right_norm).ratio()


def install_signal_handlers(stop_flag: StopFlag) -> None:
    def handler(_signum: int, _frame: object) -> None:
        stop_flag.request_stop()

    signal.signal(signal.SIGINT, handler)
    signal.signal(signal.SIGTERM, handler)


def start_global_q_listener(stop_flag: StopFlag) -> None:
    """
    Try to catch `q` globally. If this fails, the main loop still checks
    terminal input as a fallback.
    """
    try:
        from pynput import keyboard
    except Exception:
        print("Global hotkey unavailable: install pynput for global `q` support.")
        return

    def on_press(key: object) -> Optional[bool]:
        try:
            char = getattr(key, "char", None)
        except Exception:
            char = None

        if char == "q":
            stop_flag.request_stop()
            return False

        return None

    try:
        listener = keyboard.Listener(on_press=on_press)
        listener.daemon = True
        listener.start()
        print("Global exit hotkey enabled: press q to stop.")
    except Exception as ex:
        print(f"Global hotkey unavailable: {ex}")


def terminal_q_pressed() -> bool:
    """
    Fallback for terminal-focused `q`.
    """
    if not sys.stdin or not sys.stdin.isatty():
        return False

    try:
        ready, _, _ = select.select([sys.stdin], [], [], 0)
    except Exception:
        return False

    if not ready:
        return False

    try:
        char = sys.stdin.read(1)
    except Exception:
        return False

    return char == "q"


def preprocess_for_ocr(image, scale: int):
    """
    Enlarge and lightly normalize screenshot for better OCR.
    Coordinates returned by Tesseract are later divided by scale.
    """
    if scale > 1:
        image = image.resize((image.width * scale, image.height * scale))

    image = ImageOps.grayscale(image)
    image = ImageOps.autocontrast(image)
    image = image.filter(ImageFilter.SHARPEN)

    return image


def read_words(scale: int, tesseract_lang: str) -> list[OcrWord]:
    screenshot = pyautogui.screenshot()
    processed = preprocess_for_ocr(screenshot, scale)

    data = pytesseract.image_to_data(
        processed,
        lang=tesseract_lang,
        output_type=pytesseract.Output.DICT,
        config="--psm 6",
    )

    words: list[OcrWord] = []

    for index, raw_text in enumerate(data.get("text", [])):
        text = (raw_text or "").strip()
        if not text:
            continue

        try:
            conf = float(data["conf"][index])
        except Exception:
            conf = -1.0

        if conf < 0:
            continue

        words.append(
            OcrWord(
                text=text,
                left=int(data["left"][index] / scale),
                top=int(data["top"][index] / scale),
                width=max(1, int(data["width"][index] / scale)),
                height=max(1, int(data["height"][index] / scale)),
                conf=conf,
                block_num=int(data["block_num"][index]),
                par_num=int(data["par_num"][index]),
                line_num=int(data["line_num"][index]),
            )
        )

    return words


def group_words_by_line(words: Iterable[OcrWord]) -> list[list[OcrWord]]:
    groups: dict[tuple[int, int, int], list[OcrWord]] = {}

    for word in words:
        key = (word.block_num, word.par_num, word.line_num)
        groups.setdefault(key, []).append(word)

    lines = list(groups.values())

    for line in lines:
        line.sort(key=lambda word: word.left)

    lines.sort(key=lambda line: (min(word.top for word in line), min(word.left for word in line)))

    return lines


def make_match(phrase: str, words: list[OcrWord], score: float) -> Match:
    left = min(word.left for word in words)
    top = min(word.top for word in words)
    right = max(word.right for word in words)
    bottom = max(word.bottom for word in words)
    text = " ".join(word.text for word in words)

    return Match(
        phrase=phrase,
        text=text,
        score=score,
        left=left,
        top=top,
        right=right,
        bottom=bottom,
    )


def find_match_in_line(
    line: list[OcrWord],
    target_phrases: list[str],
    min_score: float,
) -> Optional[Match]:
    if not line:
        return None

    best: Optional[Match] = None
    line_text = " ".join(word.text for word in line)

    for phrase in target_phrases:
        line_score = similarity(phrase, line_text)

        if line_score >= min_score:
            candidate = make_match(phrase, line, line_score)
            if best is None or candidate.score > best.score:
                best = candidate

    # Try short word windows too. This is useful when OCR detects a whole row
    # containing extra text near the button.
    max_window = min(8, len(line))

    for window_size in range(1, max_window + 1):
        for start in range(0, len(line) - window_size + 1):
            window = line[start:start + window_size]
            window_text = " ".join(word.text for word in window)

            for phrase in target_phrases:
                score = similarity(phrase, window_text)

                if score >= min_score:
                    candidate = make_match(phrase, window, score)
                    if best is None or candidate.score > best.score:
                        best = candidate

    return best


def find_button(
    target_phrases: list[str],
    min_score: float,
    scale: int,
    tesseract_lang: str,
) -> Optional[Match]:
    words = read_words(scale=scale, tesseract_lang=tesseract_lang)
    lines = group_words_by_line(words)

    best: Optional[Match] = None

    for line in lines:
        candidate = find_match_in_line(line, target_phrases, min_score)

        if candidate is None:
            continue

        if best is None or candidate.score > best.score:
            best = candidate

    return best


def click_match(match: Match, dry_run: bool) -> None:
    x, y = match.center

    print(
        f"Found: '{match.text}' ~ '{match.phrase}' "
        f"score={match.score:.2f} at ({x}, {y})"
    )

    if dry_run:
        return

    pyautogui.moveTo(x, y, duration=0.05)
    pyautogui.click(x, y)


def maybe_refresh(last_refresh: float, refresh_every: float, dry_run: bool) -> float:
    if refresh_every <= 0:
        return last_refresh

    now = time.monotonic()

    if now - last_refresh < refresh_every:
        return last_refresh

    print("Refreshing page with F5...")

    if not dry_run:
        pyautogui.press("f5")

    return now


def scroll_page(amount: int, dry_run: bool) -> None:
    direction = "down" if amount < 0 else "up"
    print(f"Button not found. Scrolling {direction}...")

    if not dry_run:
        pyautogui.scroll(amount)


def parse_buttons(values: list[str]) -> list[str]:
    buttons: list[str] = []

    for value in values:
        for part in value.split("|"):
            part = part.strip()
            if part:
                buttons.append(part)

    # Preserve order, remove duplicates case-insensitively.
    result: list[str] = []
    seen: set[str] = set()

    for button in buttons:
        key = normalize_text(button)
        if key not in seen:
            seen.add(key)
            result.append(button)

    return result


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Search and click ChatGPT/Codex create/update buttons on screen."
    )

    parser.add_argument(
        "--button",
        action="append",
        default=[],
        help=(
            "Additional button text to search for. "
            "Can be repeated or separated with '|'."
        ),
    )

    parser.add_argument(
        "--only-buttons",
        action="store_true",
        help="Use only buttons passed through --button, not built-in defaults.",
    )

    parser.add_argument(
        "--interval",
        type=float,
        default=2.0,
        help="Seconds between scans. Default: 2.0",
    )

    parser.add_argument(
        "--after-click-sleep",
        type=float,
        default=3.0,
        help="Seconds to wait after a successful click. Default: 3.0",
    )

    parser.add_argument(
        "--scroll",
        type=int,
        default=-5,
        help="Mouse wheel scroll amount when button is not found. Negative means down. Default: -5",
    )

    parser.add_argument(
        "--refresh-every",
        type=float,
        default=30.0,
        help="Press F5 every N seconds. Use 0 to disable. Default: 30",
    )

    parser.add_argument(
        "--min-score",
        type=float,
        default=0.80,
        help="Fuzzy OCR match threshold from 0 to 1. Default: 0.80",
    )

    parser.add_argument(
        "--ocr-scale",
        type=int,
        default=2,
        help="Screenshot scale for OCR. Higher is slower but often more accurate. Default: 2",
    )

    parser.add_argument(
        "--tesseract-lang",
        default="eng",
        help="Tesseract language. Default: eng",
    )

    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print matches but do not click, scroll, or press F5.",
    )

    return parser.parse_args()


def configure_pyautogui() -> None:
    pyautogui.FAILSAFE = True
    pyautogui.PAUSE = 0.05


def main() -> int:
    args = parse_args()
    configure_pyautogui()

    stop_flag = StopFlag()
    install_signal_handlers(stop_flag)
    start_global_q_listener(stop_flag)

    user_buttons = parse_buttons(args.button)

    if args.only_buttons:
        target_phrases = user_buttons
    else:
        target_phrases = parse_buttons([*DEFAULT_BUTTONS, *user_buttons])

    if not target_phrases:
        print("No target buttons configured.")
        return 2

    print("Searching for buttons:")
    for phrase in target_phrases:
        print(f"  - {phrase}")

    print("")
    print("Press q to stop. Move mouse to top-left corner to trigger pyautogui FAILSAFE.")
    print("Keep Firefox/ChatGPT visible and focused.")
    print("")

    last_refresh = time.monotonic()
    last_scroll_direction = args.scroll

    while not stop_flag.stop_requested:
        if terminal_q_pressed():
            stop_flag.request_stop()
            break

        last_refresh = maybe_refresh(
            last_refresh=last_refresh,
            refresh_every=args.refresh_every,
            dry_run=args.dry_run,
        )

        try:
            match = find_button(
                target_phrases=target_phrases,
                min_score=args.min_score,
                scale=args.ocr_scale,
                tesseract_lang=args.tesseract_lang,
            )
        except pytesseract.TesseractNotFoundError:
            print(
                "Tesseract executable was not found.\n"
                "Install it, for example on Fedora:\n"
                "  sudo dnf install tesseract tesseract-langpack-eng"
            )
            return 3
        except Exception as ex:
            print(f"OCR/search error: {ex}")
            time.sleep(args.interval)
            continue

        if match is not None:
            click_match(match, dry_run=args.dry_run)
            time.sleep(args.after_click_sleep)
            continue

        scroll_page(last_scroll_direction, dry_run=args.dry_run)

        # Slowly alternate scroll direction so the script can recover if it
        # scrolls past the interesting area.
        if int(time.monotonic()) % 60 < 3:
            last_scroll_direction = -last_scroll_direction
        else:
            last_scroll_direction = args.scroll

        time.sleep(args.interval)

    print("Stopped.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
