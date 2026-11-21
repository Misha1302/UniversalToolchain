#!/usr/bin/env python3
"""Structural validation of PowerShell scripts that no Linux job would otherwise parse.

`build.ps1` is the canonical entrypoint on Windows, but every Linux check exercises `build.sh`
instead. A PowerShell parse error therefore passes the whole Linux suite and only surfaces in the
Windows job - which is how a trailing comma after the last array element survived local verification
and reached `master`, where it broke the Windows build outright.

This checker is not a PowerShell parser and does not claim to be one. It enforces the two properties
that actually fail in practice and that a parser rejects immediately:

  1. no dangling separator before a closing bracket;
  2. every bracket closed, in order, outside strings, comments and here-strings.

Anything it cannot classify is a failure rather than a silent pass, so an unusual construct surfaces
as a review question instead of a broken release.

Known gaps, stated rather than hidden: strings are treated as opaque content, so a bracket imbalance
inside a string never affects the result (correct here), and there is no type checking. It is a syntax
sanity gate, not a PowerShell implementation.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

OPENERS = {"(": ")", "[": "]", "{": "}"}
CLOSERS = {")": "(", "]": "[", "}": "{"}


class ScriptError(RuntimeError):
    pass


def _mask(block: str) -> str:
    """Replace every non-newline character with a neutral placeholder.

    The placeholder must be non-blank: blanking a string entirely would make an array element look
    like an empty line, which is precisely the mistake this checker exists to catch in the other
    direction. Newlines are preserved so reported line numbers stay meaningful.
    """
    return "".join("\n" if char == "\n" else "_" for char in block)


def strip_strings_and_comments(text: str, path: Path) -> str:
    """Return the script with string and comment content masked.

    Strings keep their delimiters and become `"___"`-shaped tokens so that an element is still an
    element; only its content is hidden. Comments become blanks.
    """
    result: list[str] = []
    index = 0
    length = len(text)
    while index < length:
        char = text[index]

        # Here-strings: @" ... "@ and @' ... '@. The terminator must be at line start.
        if char == "@" and index + 1 < length and text[index + 1] in "\"'":
            quote = text[index + 1]
            terminator = f"\n{quote}@"
            end = text.find(terminator, index + 2)
            if end < 0:
                raise ScriptError(f"{path}: unterminated here-string")
            block = text[index:end + len(terminator)]
            result.append(_mask(block))
            index = end + len(terminator)
            continue

        # Block comment: <# ... #>
        if char == "<" and text.startswith("<#", index):
            end = text.find("#>", index + 2)
            if end < 0:
                raise ScriptError(f"{path}: unterminated block comment")
            block = text[index:end + 2]
            result.append("".join("\n" if c == "\n" else " " for c in block))
            index = end + 2
            continue

        # Line comment.
        if char == "#":
            end = text.find("\n", index)
            end = length if end < 0 else end
            result.append(" " * (end - index))
            index = end
            continue

        # Single-quoted string: '' is an escaped quote.
        if char == "'":
            start = index
            index += 1
            while True:
                if index >= length:
                    raise ScriptError(f"{path}: unterminated single-quoted string")
                current = text[index]
                if current == "'":
                    if index + 1 < length and text[index + 1] == "'":
                        index += 2
                        continue
                    index += 1
                    break
                index += 1
            result.append(_mask(text[start:index]))
            continue

        # Double-quoted string: backtick escapes the next character.
        if char == '"':
            start = index
            index += 1
            while True:
                if index >= length:
                    raise ScriptError(f"{path}: unterminated double-quoted string")
                current = text[index]
                if current == "`" and index + 1 < length:
                    index += 2
                    continue
                if current == '"':
                    index += 1
                    break
                index += 1
            result.append(_mask(text[start:index]))
            continue

        result.append(char)
        index += 1

    return "".join(result)


def require_balanced(path: Path, code: str) -> None:
    stack: list[tuple[str, int]] = []
    line = 1
    for char in code:
        if char == "\n":
            line += 1
            continue
        if char in OPENERS:
            stack.append((char, line))
            continue
        if char in CLOSERS:
            if not stack:
                raise ScriptError(f"{path}:{line}: closing '{char}' has no opener")
            opener, opener_line = stack.pop()
            if opener != CLOSERS[char]:
                raise ScriptError(
                    f"{path}:{line}: '{char}' closes '{opener}' opened at line {opener_line}"
                )
    if stack:
        opener, opener_line = stack[-1]
        raise ScriptError(f"{path}:{opener_line}: '{opener}' is never closed")


def require_no_dangling_separator(path: Path, code: str) -> None:
    """A separator directly before a closing bracket is a parse error in PowerShell.

    This is the exact shape that broke the Windows build: deleting the last array element left
    `"-p:UseSharedCompilation=...",` followed by `))`.
    """
    lines = code.splitlines()
    for number, raw in enumerate(lines, start=1):
        trimmed = raw.rstrip()
        if not trimmed.endswith(","):
            continue
        for offset in range(number, len(lines)):
            candidate = lines[offset].strip()
            if not candidate:
                continue
            if candidate.startswith((")", "}")):
                raise ScriptError(
                    f"{path}:{number}: dangling ',' before '{candidate[0]}' on line {offset + 1} - "
                    "PowerShell rejects a trailing element separator (\"Missing expression after ','\")"
                )
            break


def script_files(root: Path) -> list[Path]:
    ignored = {"bin", "obj", "artifacts", ".git", "node_modules", "dist", ".vitepress"}
    return sorted(
        path for path in root.rglob("*.ps1")
        if not any(part in ignored for part in path.relative_to(root).parts)
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.root.resolve()

    files = script_files(root)
    if not files:
        print("powershell-syntax: FAIL - no .ps1 files found; the gate would be vacuous", file=sys.stderr)
        return 1

    failures: list[str] = []
    for path in files:
        relative = Path(path.relative_to(root).as_posix())
        try:
            code = strip_strings_and_comments(path.read_text(encoding="utf-8-sig"), relative)
            require_balanced(relative, code)
            require_no_dangling_separator(relative, code)
        except ScriptError as error:
            failures.append(str(error))

    if failures:
        for failure in failures:
            print(f"powershell-syntax: {failure}", file=sys.stderr)
        return 1

    print(f"powershell-syntax: PASS ({len(files)} scripts)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
