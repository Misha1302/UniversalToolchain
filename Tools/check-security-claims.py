#!/usr/bin/env python3
"""Forbid security claims the runtime cannot back (UT-A052).

The framework runs extension code inside the host process. Restricted composition, deterministic plans
and capability policy constrain *selection*; they do not isolate memory, CPU or the filesystem. Every
current document says so correctly.

That is exactly the state worth protecting with a gate, because the failure is a wording change: someone
describes a restricted dialect as "sandboxed", the sentence reads better, and now a consumer of
third-party packages believes a guarantee that does not exist. Mis-stated security is worse than absent
security - it removes the caution the reader would otherwise apply.

Detection is deliberately narrow to stay honest:

  * only `sandbox*` and `<scope> isolation` are claims - "hardening" in general is not;
  * the affirmative verb must sit within a short window of the claim, in the same line;
  * a denial, a requirement on the host, or a warning context neutralises the line;
  * a bullet that continues a denied list inherits that denial from the preceding lines.

Anything else is left alone. A gate that fires on legitimate prose gets switched off, which is worse
than no gate.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

# A claim-word: something that asserts a security boundary the runtime does not provide.
CLAIM = re.compile(
    r"\b(?:sandbox(?:es|ed|ing)?|(?:memory|cpu|resource|process)\s+isolation)\b",
    re.IGNORECASE,
)

# An affirmative verb close enough to the claim to be a promise about it. Checked on BOTH sides of the
# claim: "provides a sandbox" and "sandboxed execution is provided" make the same promise.
AFFIRM_BEFORE = re.compile(
    r"\b(?:provides?|provided|offers?|guarantees?|guaranteed|ensures?|enforces?|delivers?|gives?|"
    r"acts?\s+as|is|are|becomes?)\b[^.;:]{0,60}$",
    re.IGNORECASE,
)
AFFIRM_AFTER = re.compile(
    r"^[^.;:]{0,60}\b(?:is|are|become[s]?|remains?|provides?|guarantees?|ensures?|enforces?|"
    r"offered|provided|guaranteed|enforced|delivered)\b",
    re.IGNORECASE,
)

# Denial anywhere in the line, or a requirement placed on the host rather than a claim about the runtime.
NEUTRAL = re.compile(
    r"\b(?:not|never|no|none|nothing|isn't|aren't|doesn't|don't|didn't|cannot|can't|nor|neither|"
    r"without|outside|beyond|instead\s+of|rather\s+than|unlike|"
    r"assuming|avoid|required|require[sd]?|must|should|need[sd]?|recommend(?:s|ed)?)\b",
    re.IGNORECASE,
)

# A completion criterion states when isolation would exist, not that it does. "resource isolation is
# complete only when limits are enforced by a separate execution boundary" is a to-do, not a promise.
CRITERION = re.compile(
    r"\b(?:is|are|remains?)\s+(?:complete|done|achieved|satisfied|in\s+place)\s+only\s+(?:when|if|after)\b",
    re.IGNORECASE,
)

# Continuation of a denied list: "- process isolation;" under "does not provide:".
LIST_CONTINUATION = re.compile(r"^\s*(?:[-*+]|\d+\.)\s")

# The unit of judgement. A denial must not excuse a claim made in the next sentence on the same line.
SENTENCE_SPLIT = re.compile(r"(?<=[.!?])\s+")

SCANNED_DIRS = ("docs", "internal-docs")
SCANNED_FILES = ("readme.md", "VERIFICATION.md", "RELEASE_NOTES_RU.md")
DENIAL_LOOKBACK = 4


class SecurityClaimError(RuntimeError):
    pass


def markdown_files(root: Path) -> list[Path]:
    files: list[Path] = []
    for directory in SCANNED_DIRS:
        base = root / directory
        if base.is_dir():
            files.extend(base.rglob("*.md"))
    for name in SCANNED_FILES:
        candidate = root / name
        if candidate.is_file():
            files.append(candidate)
    return sorted({path for path in files if not any(part in {"bin", "obj", "artifacts"} for part in path.parts)})


def logical_units(lines: list[str]) -> list[tuple[int, str, bool]]:
    """Group physical lines into the unit a reader actually parses: a paragraph or a list item.

    Markdown wraps prose across lines, so a physical line is not a sentence and not even a clause.
    Judging lines produced two opposite defects, both measured on this gate:

      * "The preset does not\\nprovide a security sandbox." - a denial split by a wrap. The continuation
        starts with "provide", which reads as affirmative, and the gate reported an unbacked claim in a
        document that says exactly the opposite. A false positive is how a gate gets switched off.
      * An affirmative split the same way is reported against the second line, so the message names a
        fragment and the reader has to reconstruct what was actually claimed.

    Joining the paragraph removes the distinction: the wrap disappears, and the denial or the
    affirmation is judged in the same sentence as the claim it governs. The reported unit carries the
    1-based number of its first line, which is where a reader looks.
    """
    units: list[tuple[int, str, bool]] = []
    buffer: list[str] = []
    first_line = 0
    is_bullet = False

    def flush() -> None:
        nonlocal buffer, is_bullet
        if buffer:
            units.append((first_line + 1, " ".join(buffer), is_bullet))
            buffer = []
            is_bullet = False

    for index, line in enumerate(lines):
        stripped = line.strip()

        if not stripped:
            flush()
            continue

        # A heading is its own unit: it never continues the paragraph above or below it.
        if stripped.startswith("#"):
            flush()
            units.append((index + 1, stripped, False))
            continue

        # A new list item starts a new unit even when no blank line separates the items.
        if LIST_CONTINUATION.match(line):
            flush()
            first_line = index
            is_bullet = True

        if not buffer:
            first_line = index
            is_bullet = is_bullet or bool(LIST_CONTINUATION.match(line))
        buffer.append(stripped)

    flush()
    return units


def violations(text: str, name: str) -> list[str]:
    """Judge each logical unit, then each sentence inside it.

    A unit such as "They are not hardened sandboxing. Hostile input is handled by the runtime, which
    provides memory isolation." states a denial and then an unbacked promise. Splitting the unit into
    sentences keeps those apart, so the denial cannot excuse the promise - while the wrap between
    "does not" and "provide" no longer splits a denial from the verb that makes it one.
    """
    found: list[str] = []
    lines = text.splitlines()
    for line_number, block, is_bullet in logical_units(lines):
        for sentence in SENTENCE_SPLIT.split(block):
            collapsed = " ".join(sentence.split())
            if not collapsed:
                continue
            match = CLAIM.search(collapsed)
            if not match:
                continue

            before = collapsed[: match.start()]
            after = collapsed[match.end() :]
            if not (AFFIRM_BEFORE.search(before) or AFFIRM_AFTER.search(after)):
                # A bare noun phrase in a list or heading asserts nothing on its own.
                continue

            if NEUTRAL.search(collapsed) or CRITERION.search(collapsed):
                continue

            # A bullet continuing a denied/required list inherits that context from the lines above it.
            if is_bullet:
                context = " ".join(lines[max(0, line_number - 1 - DENIAL_LOOKBACK) : line_number - 1])
                if NEUTRAL.search(context):
                    continue

            found.append(f"{name}:{line_number}: unbacked security claim: {collapsed[:180]}")
    return found


def check(root: Path) -> list[str]:
    files = markdown_files(root)
    if not files:
        raise SecurityClaimError(
            "No Markdown documents were found to check. Refusing to report success on an empty scan."
        )
    problems: list[str] = []
    for path in files:
        problems.extend(violations(path.read_text(encoding="utf-8", errors="strict"), str(path.relative_to(root))))
    return problems


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path("."))
    args = parser.parse_args(argv)
    root = args.root.resolve()
    try:
        problems = check(root)
    except (SecurityClaimError, UnicodeDecodeError) as error:
        print(f"SECURITY-CLAIM: FAIL - {error}", file=sys.stderr)
        return 1
    if problems:
        print("SECURITY-CLAIM: FAIL", file=sys.stderr)
        for problem in problems:
            print(f"  {problem}", file=sys.stderr)
        print(
            "  Deny the guarantee explicitly (\"is not a hardened sandbox\") or move the requirement to a "
            "\"required host control\" sentence.",
            file=sys.stderr,
        )
        return 1
    print(f"SECURITY-CLAIM: PASS ({len(markdown_files(root))} documents, no unbacked claim)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
