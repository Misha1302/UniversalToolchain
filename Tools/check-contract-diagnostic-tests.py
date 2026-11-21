#!/usr/bin/env python3
"""Require every module-contract diagnostic to be either tested or declared untested.

`Tools/check-diagnostic-codes.py` already proves that each declared code is *referenced* by a production
verifier. That is necessary and not sufficient: the check-security-claims lesson applies here too.

PR #418 fixed a concrete instance. `UT-CONTRACT-002` was declared, documented, referenced by
`ContractIdRegistryValidator`, and counted as live by the gate - while the validator itself was called
only from a test, so the code could not be emitted in production at all. A reference is not a reachability
proof, and every gate built on "the constant is mentioned" inherits that blindness.

Full reachability analysis is out of scope for a Python gate: it would need a call graph, and a wrong
call graph produces false confidence, which is worse than none. What *is* cheaply decidable is a weaker
but honest signal: does any test exercise the code? A code with no test has never been observed to fire,
which means its message, severity and trigger condition are all unverified claims.

Three states are legal, and the gate enforces that each code is in exactly one of them:

  tested      a test asserts that this diagnostic *appears*, so some test has observed it firing;
  reserved    the code is declared and deliberately unreachable yet, recorded with a reason below -
              this is the same device `FACADE_RESERVED` uses, and it must stay stale-free;
  (nothing)   everything else is an error, because a code nobody tests and nobody reserves is a
              diagnostic whose behaviour is asserted only by its own source text.

A negative assertion is not a test. The first version of this gate counted *any* mention of the
constant inside a test file, and that reproduced the very failure it exists to catch: `UT-CONTRACT-009`
was "covered" by a single `Does.Not.Contain(MissingFacetKindOrder)` line. A test that asserts a code
*never* appears says nothing about whether it can appear - it passes just as happily on a code that
cannot fire at all. Worse, it is the natural way to write a control for a *different* code, so the
count would rise without anyone ever observing the diagnostic. Only a reference inside an affirmative
assertion counts as evidence, which is what `_is_negative_reference` decides.

The reserved list is checked in both directions on purpose. A reservation for a code that got tested is
stale and must be removed, otherwise the list quietly accumulates exceptions and stops meaning anything.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

CONTRACT_DECLARATION = Path('UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractDiagnosticCodes.cs')
CONTRACT_CONSTANT = re.compile(r'public\s+const\s+string\s+(?P<name>\w+)\s*=\s*"(?P<code>UT-[A-Z0-9-]+)"')

SKIP_DIRS = {'bin', 'obj', 'artifacts', 'node_modules', '.git', '.cache'}

# Codes with no test today, each with the measured reason. This is a record, not a permission slip: the
# reviewer sees the reasoning, and the gate fails the moment the reason stops being true.
CONTRACT_UNTESTED: dict[str, str] = {
    'UT-CONTRACT-009': (
        'unreachable by construction, not merely untested. ValidateComplete() enumerates '
        'Enum.GetValues<ContractFacetKind>() and reports every value missing from the order table; all eight '
        'declared values are present, so it always returns empty. The one way to reach the code - a facet whose '
        'Kind is outside the enum - never gets there, because GetSortKey throws InvalidOperationException while '
        'ordering, several lines before ValidateComplete() runs. Measured: one such facet builds silently with no '
        'diagnostic, two of them throw a raw InvalidOperationException. Fixing this is a production change with '
        'its own PR; reserving it here keeps the gate honest instead of pretending a Does.Not.Contain control '
        'proves the code fires.'
    ),
}


def is_test_path(relative: Path) -> bool:
    return any(part == 'Tests' or part.endswith('.Tests') for part in relative.parts)


def iter_sources(root: Path):
    for path in root.rglob('*.cs'):
        parts = set(path.relative_to(root).parts)
        if parts & SKIP_DIRS:
            continue
        if path.name.endswith('.g.cs') or path.name.endswith('.Designer.cs'):
            continue
        yield path


def declared_codes(root: Path) -> tuple[dict[str, str], list[str]]:
    path = root / CONTRACT_DECLARATION
    if not path.exists():
        return {}, [f'{CONTRACT_DECLARATION.as_posix()}: file is missing']

    declared: dict[str, str] = {}
    problems: list[str] = []
    for match in CONTRACT_CONSTANT.finditer(path.read_text(encoding='utf-8')):
        code, name = match.group('code'), match.group('name')
        if code in declared:
            problems.append(f'{CONTRACT_DECLARATION.as_posix()}: {code} is declared twice ({declared[code]} and {name})')
        declared[code] = name
    return declared, problems


NEGATIVE_ASSERTION = re.compile(
    r'Does\.Not\.|Is\.Not\.|Has\.None\.|Has\.No\.|Does\.Not\.Exist|Expression\.Not|Throws\.Nothing'
)


def _assertion_context(text: str, match: re.Match[str]) -> str:
    """The assertion statement around a reference, which in this codebase never spans more than a few lines.

    Counting a reference per physical line was the first mistake in the sibling security gate: prose wraps,
    so a single statement can be split across lines and a line-local test then misjudges it. Three lines of
    look-back is enough for `Assert.That(\n  x.Code,\n  Is.EqualTo(...))` without reaching into the
    preceding statement and borrowing its negation.
    """
    start = max(0, text.count('\n', 0, match.start()) - 2)
    lines = text.splitlines()
    return '\n'.join(lines[start:start + 4])


def _is_negative_reference(text: str, match: re.Match[str]) -> bool:
    """True when the reference sits inside a "this code must not appear" assertion.

    `Does.Not.Contain(ModuleContractDiagnosticCodes.Foo)` is a control, not evidence: it asserts the code
    is *absent*, so it holds equally well for a code that can never fire. Treating it as coverage is how
    `UT-CONTRACT-009` passed this gate while being unreachable by construction.
    """
    return NEGATIVE_ASSERTION.search(_assertion_context(text, match)) is not None


def tested_codes(root: Path, declared: dict[str, str]) -> dict[str, str]:
    """Map code -> one representative affirmative test location.

    References inside negative assertions are not counted; see `_is_negative_reference`.
    """
    found: dict[str, str] = {}
    for path in iter_sources(root):
        relative = path.relative_to(root)
        if not is_test_path(relative):
            continue
        try:
            text = path.read_text(encoding='utf-8')
        except UnicodeDecodeError:
            continue
        for code, name in declared.items():
            if code in found:
                continue
            for match in re.finditer(rf'\b{re.escape(name)}\b', text):
                if _is_negative_reference(text, match):
                    continue
                line = text.count('\n', 0, match.start()) + 1
                found[code] = f'{relative.as_posix()}:{line}'
                break
    return found


def check(root: Path) -> list[str]:
    declared, problems = declared_codes(root)
    if not declared:
        return problems or [f'{CONTRACT_DECLARATION.as_posix()}: no codes declared, refusing to report success']

    tested = tested_codes(root, declared)

    for code in sorted(set(declared) - set(tested)):
        if code in CONTRACT_UNTESTED:
            continue
        problems.append(
            f'{declared[code]}: {code} is declared but no test references it; add a test that makes the '
            f'verifier emit it, or record why it is deliberately untested in CONTRACT_UNTESTED'
        )

    for code in sorted(CONTRACT_UNTESTED):
        if code not in declared:
            problems.append(f'CONTRACT_UNTESTED lists {code}, but no constants file declares it; remove the entry')
        elif code in tested:
            problems.append(
                f'CONTRACT_UNTESTED lists {code} as untested, but {tested[code]} references it; '
                f'the reservation is stale'
            )

    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve()

    problems = check(root)
    if problems:
        print('contract-diagnostic-test coverage check failed:', file=sys.stderr)
        for problem in problems:
            print(f'- {problem}', file=sys.stderr)
        return 1

    declared, _ = declared_codes(root)
    print(
        f'Contract-diagnostic test coverage passed: {len(declared)} declared codes, '
        f'{len(declared) - len(CONTRACT_UNTESTED)} tested, {len(CONTRACT_UNTESTED)} recorded untested.'
    )
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
