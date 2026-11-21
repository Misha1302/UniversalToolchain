#!/usr/bin/env python3
"""Keep the markdown smoke check from overwriting the evidence it is supposed to check.

`run-markdown-bash-blocks.py` executes every `bash` fenced block it finds unless the block is marked
`ci-run=false`. That is the right default for quickstart snippets: a documented command should be
demonstrably runnable, not merely plausible.

The default is wrong for one kind of block, though: a *regeneration recipe*. Setting
`WIST_REGENERATE_PARITY_CORPUS=1` makes the parity fixture write the committed corpus instead of
comparing against it, so a CI run of that block replaces the golden with whatever the current code
produces and then passes. The check would erase its own evidence.

That is not hypothetical. It happened: the regeneration recipe in `docs/architecture/cross-backend-parity.md`
was unmarked, so CI executed it. The committed file happened to be in sync, so nothing broke - but the
corpus only stayed honest because the measurement agreed that day. The block also sat at the 60 s
timeout boundary (51 s in CI, 91 s locally), which is a flaky gate on top of a false one.

The fix is a marker, and a marker is a promise. This gate holds the promise:

  * every fenced bash block whose body carries a regeneration switch must say `ci-run=false`;
  * the same applies to blocks that redirect output into a tracked source file.

Scope decisions:

  * only fenced ```bash blocks are considered - the runner ignores every other fence;
  * `ci-run=false` is read from the fence attributes, exactly as the runner reads it;
  * binary and build output directories are skipped;
  * the recipe is not deleted. A maintainer still needs the exact command; it just must not run in CI.
"""
from __future__ import annotations

import argparse
import re
import shlex
import sys
from pathlib import Path

EXCLUDED_DIRECTORIES = {
    '.git', '.cache', '.venv', 'artifacts', 'bin', 'dist', 'node_modules', 'obj', 'packages',
}

FENCE = re.compile(r'```bash(?P<attrs>[^\n]*)\n(?P<body>.*?)```', re.DOTALL)

# Environment switches whose whole purpose is to rewrite committed data. A smoke check that sets one
# of these is not checking; it is regenerating.
REGENERATION_SWITCHES = ('WIST_REGENERATE_PARITY_CORPUS',)

# Writing into a tracked file from a doc block is the same failure in another shape.
REDIRECT_INTO_SOURCE = re.compile(r'(?<![0-9&])>{1,2}\s*(?P<target>[A-Za-z0-9_./\\-]+\.(?:json|cs|csproj|md|yml|yaml|txt|props|targets|sln))')


def isMarkedSkip(raw_attributes: str) -> bool:
    """Read the fence attributes exactly the way run-markdown-bash-blocks.py reads them.

    The runner splits on shell quoting and matches the key case-sensitively, and only then treats the
    value as a boolean. A gate that is more permissive than the runner would bless a block that still
    executes: `CI-RUN=FALSE` looks marked to a human, but the runner does not recognise the key at all
    and fails the build with "Unsupported bash fence attribute". Matching the runner's rule here keeps
    the two from disagreeing about what "marked" means.
    """
    try:
        tokens = shlex.split(raw_attributes)
    except ValueError:
        return False

    for token in tokens:
        if '=' not in token:
            continue
        key, value = token.split('=', 1)
        if key != 'ci-run':
            continue
        return value.strip().lower() in {'0', 'false', 'no'}

    return False


def isExcluded(path: Path) -> bool:
    return any(part in EXCLUDED_DIRECTORIES for part in path.parts)


def findBlocks(root: Path):
    for markdown in sorted(root.rglob('*.md')):
        relative = markdown.relative_to(root)
        if isExcluded(relative):
            continue
        try:
            text = markdown.read_text(encoding='utf-8')
        except (UnicodeDecodeError, OSError):
            continue
        for match in FENCE.finditer(text):
            yield markdown, relative, match, text


def check(root: Path) -> list[str]:
    problems: list[str] = []

    for markdown, relative, match, text in findBlocks(root):
        attrs = match.group('attrs')
        body = match.group('body')

        reasons: list[str] = []
        for switch in REGENERATION_SWITCHES:
            if switch in body:
                reasons.append(f'sets {switch}')

        for redirect in REDIRECT_INTO_SOURCE.finditer(body):
            reasons.append(f'writes into {redirect.group("target")}')

        if not reasons:
            continue

        if isMarkedSkip(attrs):
            continue

        line = text[:match.start()].count('\n') + 1
        problems.append(
            f'{relative}:{line}: bash block {" and ".join(sorted(set(reasons)))} '
            f'but is not marked ci-run=false'
        )

    return problems


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', default='.')
    arguments = parser.parse_args()

    root = Path(arguments.root).resolve()
    problems = check(root)

    if problems:
        print('Markdown block hygiene check failed:')
        for problem in problems:
            print(f'- {problem}')
        print()
        print('A block that regenerates committed data must not run in CI: it would pass by replacing the')
        print('evidence with current behaviour. Mark the fence ci-run=false and explain why in the prose.')
        return 1

    print('Markdown block hygiene check passed: no unmarked regeneration or source-writing block.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
