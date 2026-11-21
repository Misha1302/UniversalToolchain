#!/usr/bin/env python3
"""Keep the diagnostics reference honest about which codes exist.

`docs/reference/diagnostics.md` is the only user-facing statement of which diagnostic families a
language author can expect. It described the typed-route family as `UTL2201`-`UTL2204` while the code
emitted nine codes, and the slot family as `UTL2101`-`UTL2103` while the code emitted six. Nobody
noticed, because nothing connected the table to the source.

That is the failure this gate exists to prevent, and it is a *false* failure mode rather than a
missing one: a table that understates the codes teaches a reader that `UTL2205` does not exist. A
reader who then meets `UTL2205` in a build log has been given wrong information by the reference.

Both directions are checked:

  * every code the production source can emit must be documented;
  * every code inside a documented range must actually be emitted.

The second half is what stops a "fix" that widens a range to cover a stray code and thereby invents
codes that do not exist. If a code is genuinely reserved for future use, it does not belong in a
range claimed as current.

Scope decisions that keep the gate from firing on legitimate code:

  * only string literals `"UTLnnnn"` count as emission - a code mentioned in a comment is not a
    diagnostic, and comments here deliberately discuss codes while explaining history;
  * test projects are excluded, because a test assertion mentions a code without emitting it and
    would otherwise mask a gap in production;
  * `bin`, `obj`, `artifacts`, `node_modules` are excluded.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

# `"UTL2212"` as a string literal. Deliberately anchored to the quotes: a bare `UTL2212` in prose or a
# comment is a discussion, not an emission site.
EMITTED = re.compile(r'["\'](UTL\d{4})["\']')

# A documented single code, and a documented contiguous range. The dash variants cover the characters
# actually used in the reference (en dash today); the plain hyphen is accepted so a later edit that
# changes the dash does not silently disable range parsing.
CODE = re.compile(r'\b(UTL\d{4})\b')
RANGE = re.compile(r'`(UTL\d{4})`\s*(?:[\u2013\u2014-]|\.\.)\s*`(UTL\d{4})`')

SKIP_DIRS = {'bin', 'obj', 'artifacts', 'node_modules', '.git', 'packages', '.cache'}


def is_test_path(relative: Path) -> bool:
    """A code mentioned in a test is an expectation, not a production emission.

    Test projects in this repository are named both `Tests` and `<Project>.Tests`, so a single exact
    directory name is not enough: a check that only skipped `Tests` counted
    `UniversalToolchain.LanguageSdk.Tests` as production and reported the code from an assertion site.
    """
    return any(part == 'Tests' or part.endswith('.Tests') for part in relative.parts)


def iter_sources(root: Path):
    for path in root.rglob('*.cs'):
        parts = set(path.relative_to(root).parts)
        if parts & SKIP_DIRS:
            continue
        if path.name.endswith('.g.cs') or path.name.endswith('.Designer.cs'):
            continue
        yield path


def emitted_codes(root: Path) -> dict[str, str]:
    """Map code -> one representative source location, for actionable error messages."""
    found: dict[str, str] = {}
    for path in iter_sources(root):
        relative = path.relative_to(root)
        if is_test_path(relative):
            continue
        try:
            text = path.read_text(encoding='utf-8')
        except UnicodeDecodeError:
            continue
        for match in EMITTED.finditer(text):
            code = match.group(1)
            line = text.count('\n', 0, match.start()) + 1
            found.setdefault(code, f'{relative.as_posix()}:{line}')
    return found


def documented_codes(reference: Path) -> tuple[set[str], set[str], list[str]]:
    """Return (single codes, codes covered by a range, range descriptions)."""
    text = reference.read_text(encoding='utf-8')
    singles = {match.group(1) for match in CODE.finditer(text)}

    covered: set[str] = set()
    ranges: list[str] = []
    for match in RANGE.finditer(text):
        low, high = match.group(1), match.group(2)
        low_n, high_n = int(low[3:]), int(high[3:])
        if high_n < low_n:
            ranges.append(f'{low}-{high} is inverted')
            continue
        if low[:3] != high[:3]:
            ranges.append(f'{low}-{high} crosses a family boundary')
            continue
        for number in range(low_n, high_n + 1):
            covered.add(f'UTL{number:04d}')
        ranges.append(f'{low}-{high}')
    return singles, covered, ranges


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve()

    reference = root / 'docs' / 'reference' / 'diagnostics.md'
    if not reference.exists():
        print(f'diagnostic-code check failed:\n- {reference.relative_to(root).as_posix()}: file is missing')
        return 1

    emitted = emitted_codes(root)
    singles, covered, ranges = documented_codes(reference)

    malformed = [description for description in ranges if 'is inverted' in description or 'crosses' in description]
    documented = singles | covered

    errors: list[str] = []
    for description in malformed:
        errors.append(f'docs/reference/diagnostics.md: documented range {description}')

    for code in sorted(set(emitted) - documented):
        errors.append(
            f'docs/reference/diagnostics.md: {code} is emitted at {emitted[code]} but no row covers it; '
            f'add it to the matching family row'
        )

    for code in sorted(documented - set(emitted)):
        errors.append(
            f'docs/reference/diagnostics.md: {code} is documented but no production source emits it; '
            f'narrow the range or remove the code'
        )

    if errors:
        print('diagnostic-code check failed:')
        for error in errors:
            print(f'- {error}')
        return 1

    print(
        f'Diagnostic-code check passed: {len(emitted)} emitted codes documented '
        f'({len(ranges)} ranges, {len(singles - covered)} single codes).'
    )
    return 0


if __name__ == '__main__':
    sys.exit(main())
