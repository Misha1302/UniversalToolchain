#!/usr/bin/env python3
"""Validate the executable bodies of GitHub workflow steps.

A workflow that parses is not a workflow that runs. Two defects of exactly that kind reached
`master` in the same change:

  - `build.ps1` gained a trailing separator and stopped parsing at all (no Linux job parsed it);
  - `.github/workflows/wist-rollout-sample-smoke.yml` had a `-p:NuGetAudit=false` argument replaced by
    a whitespace-only line inside a `run: >` folded block. A blank line terminates a folded block, so
    `--verbosity quiet` became a second shell command:

        /home/runner/work/_temp/....sh: line 2: --verbosity: command not found
        ##[error]Process completed with exit code 127.

Both came from deleting an argument and leaving its line behind. This checker addresses the shape
rather than the incident.

Design notes, because they are load-bearing:

  * No YAML library is used. PyYAML is not a declared dependency of this repository, and a gate that
    silently needs an undeclared package is a gate that fails on a fresh machine - or worse, gets
    skipped. The subset understood here (`run:` with an inline scalar, `|`, or `>`) is exactly what
    the workflows use.
  * A `run:` form this checker cannot classify is a failure, not a skip. An unusual construct
    therefore surfaces as a review question instead of a silently unguarded command.
"""
from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path

# `run:` may appear after a `- ` list marker. Modifiers such as `|-` and `>-` are accepted.
RUN_KEY = re.compile(r"^(?P<indent>\s*)(?:-\s+)?run:\s*(?P<value>.*?)\s*$")
BLOCK_MARKERS = {"|", "|-", "|+", ">", ">-", ">+"}

# A line may start with: an assignment, a command or path, a control/substitution/comment/quote, or a
# shell no-op. Anything else is almost certainly a stray argument that lost its command.
COMMAND_START = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*=|[A-Za-z_.~/][\w./~-]*|[-$@#({\[!\"']|:|\\)")
# A line that consists only of long options cannot start a command.
DETACHED_OPTIONS = re.compile(r"^--[A-Za-z][\w-]*(\s+\S+)?$")


class WorkflowError(RuntimeError):
    pass


@dataclass
class RunBlock:
    label: str
    style: str  # "inline" | "literal" | "folded"
    lines: list[tuple[int, str]]


def indent_of(line: str) -> int:
    return len(line) - len(line.lstrip(" "))


def extract_run_blocks(path: Path) -> list[RunBlock]:
    """Extract every `run:` body by YAML indentation, without parsing the rest of the document."""
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    blocks: list[RunBlock] = []
    index = 0

    while index < len(lines):
        match = RUN_KEY.match(lines[index])
        if match is None:
            index += 1
            continue

        key_indent = len(match.group("indent"))
        value = match.group("value")
        line_number = index + 1

        if value and value not in BLOCK_MARKERS:
            # Inline scalar, e.g. `run: mkdir -p UniversalToolchain/packages`.
            blocks.append(RunBlock(f"{path.name}:{line_number}", "inline", [(line_number, value)]))
            index += 1
            continue

        if value not in BLOCK_MARKERS:
            raise WorkflowError(
                f"{path.name}:{line_number}: `run:` has neither an inline value nor a block marker"
            )

        style = "folded" if value.startswith(">") else "literal"
        body: list[tuple[int, str]] = []
        cursor = index + 1
        while cursor < len(lines):
            candidate = lines[cursor]
            if candidate.strip() and indent_of(candidate) <= key_indent:
                break
            body.append((cursor + 1, candidate))
            cursor += 1

        # Trailing blank lines belong to the block per YAML, but they are noise for this check.
        while body and not body[-1][1].strip():
            body.pop()

        blocks.append(RunBlock(f"{path.name}:{line_number}", style, body))
        index = cursor

    return blocks


def require_no_blank_line_split(block: RunBlock) -> None:
    """A blank line terminates a folded `>` block, silently splitting the command.

    Literal blocks (`|`) preserve blank lines verbatim and are often meaningful - an embedded C# body
    or here-document - so they are not subject to this rule.
    """
    if block.style != "folded":
        return
    for offset, (number, line) in enumerate(block.lines):
        if line.strip():
            continue
        following = [text for _, text in block.lines[offset + 1:] if text.strip()]
        if following:
            raise WorkflowError(
                f"{block.label}:{number}: blank line inside a folded `run: >` block. YAML terminates "
                "the block there, so the remaining arguments are executed as a separate command "
                '("--verbosity: command not found", exit code 127)'
            )


def command_starts(block: RunBlock) -> list[tuple[int, str]]:
    """Lines that begin a shell command, with continuations filtered out.

    A folded block (`>`) is a single command by definition - YAML joins its lines with spaces - so it
    is returned as one entry. A literal block (`|`) keeps its line structure, and there a line inside
    `$( ... )`, an open quote, or after a trailing backslash continues the previous command. Without
    that distinction every multi-line substitution reads as a stray option, and the gate would be
    switched off in practice rather than fixed.
    """
    if block.style == "folded":
        joined = " ".join(text.strip() for _, text in block.lines if text.strip())
        return [(block.lines[0][0], joined)]

    starts: list[tuple[int, str]] = []
    depth = 0
    pending_quote: str | None = None
    continuation = False

    for number, line in block.lines:
        stripped = line.strip()
        if continuation:
            continuation = False
        elif stripped and not stripped.startswith("#") and depth == 0 and pending_quote is None:
            starts.append((number, stripped))

        position = 0
        while position < len(line):
            char = line[position]
            if pending_quote is not None:
                if char == "\\" and pending_quote == '"':
                    position += 2
                    continue
                if char == pending_quote:
                    pending_quote = None
                position += 1
                continue
            if char == "\\":
                position += 2
                continue
            if char in "\"'":
                pending_quote = char
                position += 1
                continue
            if char in "([{":
                depth += 1
            elif char in ")]}" and depth > 0:
                depth -= 1
            position += 1

        if line.rstrip().endswith("\\"):
            continuation = True

    return starts


def require_commands_are_well_formed(block: RunBlock) -> None:
    if not block.lines:
        raise WorkflowError(f"{block.label}: `run:` body is empty")

    starts = command_starts(block)
    if not starts:
        first_number = block.lines[0][0]
        raise WorkflowError(
            f"{block.label}:{first_number}: `run:` body contains no command (only comments or "
            "continuations)"
        )

    first_number, first = starts[0]
    if not COMMAND_START.match(first):
        raise WorkflowError(
            f"{block.label}:{first_number}: line does not begin a command: {first!r}. A stray argument "
            'reaching the shell produces "command not found" and exit code 127'
        )

    for number, line in starts:
        if DETACHED_OPTIONS.match(line):
            raise WorkflowError(
                f"{block.label}:{number}: line is only a command-line option: {line!r}. Options cannot "
                "start a command; an earlier blank line most likely split the intended command"
            )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.root.resolve()

    workflow_dir = root / ".github" / "workflows"
    if not workflow_dir.is_dir():
        print("workflow-commands: FAIL - no workflow directory found", file=sys.stderr)
        return 1

    workflows = sorted(workflow_dir.glob("*.yml"))
    if not workflows:
        print("workflow-commands: FAIL - no workflows found; the gate would be vacuous", file=sys.stderr)
        return 1

    checked = 0
    try:
        for workflow in workflows:
            for block in extract_run_blocks(workflow):
                require_no_blank_line_split(block)
                require_commands_are_well_formed(block)
                checked += 1
    except (WorkflowError, OSError) as error:
        print(f"workflow-commands: {error}", file=sys.stderr)
        return 1

    print(f"workflow-commands: PASS ({checked} run blocks in {len(workflows)} workflows)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
