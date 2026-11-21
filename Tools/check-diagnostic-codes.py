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

# The module-contract family uses symbol-shaped codes (`UT-AIR-STACK-001`). They are not declared inline
# at the emission site: one constants file defines the universe and the verifiers reference the constants.
# That shape needs a different check, because scanning for string literals would only ever see the
# declaration file and would count a declared-but-unused code as live.
CONTRACT_DECLARATION = Path('UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractDiagnosticCodes.cs')
CONTRACT_CONSTANT = re.compile(r'public\s+const\s+string\s+(?P<name>\w+)\s*=\s*"(?P<code>UT-[A-Z0-9-]+)"')

# A documented single code, and a documented contiguous range. The dash variants cover the characters
# actually used in the reference (en dash today); the plain hyphen is accepted so a later edit that
# changes the dash does not silently disable range parsing.
CODE = re.compile(r'\b(UTL\d{4})\b')
RANGE = re.compile(r'`(UTL\d{4})`\s*(?:[\u2013\u2014-]|\.\.)\s*`(UTL\d{4})`')

# The facade family (`UTC-*`) is declared in two constants files and documented either inline or on a
# table row. It is checked against declarations the same way, because a facade code that is declared and
# never raised tells a caller to handle a failure that cannot occur.
FACADE_DECLARATIONS = (
    Path('UniversalToolchain/UniversalToolchain.Wist/WistDiagnosticCodes.cs'),
    Path('UniversalToolchain/UniversalToolchain.Diagnostics.Abstractions/ToolchainDiagnosticCodes.cs'),
)
FACADE_CONSTANT = re.compile(r'public\s+const\s+string\s+(?P<name>\w+)\s*=\s*"(?P<code>UTC-[A-Z0-9-]+)"')

# Facade codes that are deliberately declared ahead of the code that will raise them. Each entry must say
# why, and the gate keeps the list honest: an entry that becomes live is stale and must be removed, and a
# dead code that is not listed fails the gate. This is the opposite default from the numeric family -
# there, a code that is not emitted is simply wrong; here, the owner may reserve a name, but only on the
# record.
FACADE_RESERVED = {
    'UTC-FUNC-001': 'unknown function; the resolver reports its own diagnostics today',
    'UTC-FUNC-002': 'function unavailable; no caller distinguishes this from an unknown function yet',
    'UTC-FUNC-003': 'function unsupported on backend; the backend gate reports through its own codes',
    'UTC-BIND-001': 'unknown binding; the expression typing layer reports a context diagnostic instead',
    'UTC-BIND-002': 'binding conflict; no two-binding conflict path exists yet',
    'UTC-TYPE-001': 'type mismatch; used for surface tests of ToolchainDiagnostic, no production site raises it',
}

# A module-contract code counts as documented only inside backticks on a table row. Prose and comments
# discuss codes ("there is no `UT-CONTRACT-004`") and must not read as coverage - the same anchoring
# principle the numeric family uses. Ranges are expanded, not accepted as opaque text, so a range that
# spans a numbering gap fails the "documented but not declared" check instead of inventing a code.
CONTRACT_CODE = re.compile(r'`(UT-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3})`')
CONTRACT_RANGE = re.compile(
    r'`(UT-[A-Z0-9]+(?:-[A-Z0-9]+)*)-(\d{3})`\s*(?:[\u2013\u2014-]|\.\.)\s*`(UT-[A-Z0-9]+(?:-[A-Z0-9]+)*)-(\d{3})`'
)

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


def contract_codes(root: Path) -> tuple[dict[str, str], dict[str, str], list[str]]:
    """Return (declared code -> constant name, code -> representative live use, problems).

    The module-contract family has two failure modes a literal scan cannot see:

      * a code declared in the constants file but referenced nowhere - it documents a diagnostic that
        cannot happen, which is the same lie as a phantom code in a widened range;
      * a constant used by a verifier but never declared - it would emit a code the reference cannot
        know about.
    """
    declaration_path = root / CONTRACT_DECLARATION
    if not declaration_path.exists():
        return {}, {}, [f'{CONTRACT_DECLARATION.as_posix()}: file is missing']

    declared = {
        match.group('code'): match.group('name')
        for match in CONTRACT_CONSTANT.finditer(declaration_path.read_text(encoding='utf-8'))
    }

    used: dict[str, str] = {}
    problems: list[str] = []
    for path in iter_sources(root):
        relative = path.relative_to(root)
        if is_test_path(relative) or path == declaration_path:
            continue
        try:
            text = path.read_text(encoding='utf-8')
        except UnicodeDecodeError:
            continue
        for code, name in declared.items():
            match = re.search(rf'\b{re.escape(name)}\b', text)
            if match is not None:
                line = text.count('\n', 0, match.start()) + 1
                used.setdefault(code, f'{relative.as_posix()}:{line}')

        for match in re.finditer(r'ModuleContractDiagnosticCodes\.(\w+)', text):
            if match.group(1) not in declared.values():
                line = text.count('\n', 0, match.start()) + 1
                problems.append(
                    f'{relative.as_posix()}:{line}: references '
                    f'ModuleContractDiagnosticCodes.{match.group(1)}, which the constants file does not declare'
                )

    for code, name in sorted(declared.items()):
        if code not in used:
            problems.append(
                f'{CONTRACT_DECLARATION.as_posix()}: {code} ({name}) is declared but referenced nowhere; '
                f'a declared code that cannot be emitted documents a diagnostic that does not happen'
            )

    return declared, used, problems


def facade_codes(root: Path) -> tuple[dict[str, str], dict[str, str], list[str]]:
    """Return (declared code -> constant name, code -> live reference, problems).

    Same shape as the module-contract family, with one difference: a facade code may be *reserved* rather
    than live, but only if it appears in FACADE_RESERVED with a reason. Reservation is a claim about the
    future; the gate makes it a reviewable claim instead of an accident.
    """
    declared: dict[str, str] = {}
    problems: list[str] = []

    for relative in FACADE_DECLARATIONS:
        path = root / relative
        if not path.exists():
            problems.append(f'{relative.as_posix()}: file is missing')
            continue
        for match in FACADE_CONSTANT.finditer(path.read_text(encoding='utf-8')):
            code, name = match.group('code'), match.group('name')
            if code in declared:
                problems.append(
                    f'{relative.as_posix()}: {code} is declared twice '
                    f'({declared[code]} and {name})'
                )
            declared[code] = name

    live: dict[str, str] = {}
    for path in iter_sources(root):
        relative = path.relative_to(root)
        if is_test_path(relative) or relative in FACADE_DECLARATIONS:
            continue
        try:
            text = path.read_text(encoding='utf-8')
        except UnicodeDecodeError:
            continue
        for code, name in declared.items():
            match = re.search(rf'\b{re.escape(name)}\b', text)
            if match is not None:
                line = text.count('\n', 0, match.start()) + 1
                live.setdefault(code, f'{relative.as_posix()}:{line}')

    for code in sorted(set(declared) - set(live)):
        if code not in FACADE_RESERVED:
            problems.append(
                f'{declared[code]}: {code} is declared but raised nowhere, and it is not listed as '
                f'reserved; remove it, or record why it is being reserved'
            )

    for code in sorted(FACADE_RESERVED):
        if code not in declared:
            problems.append(
                f'FACADE_RESERVED lists {code}, but no facade constants file declares it; '
                f'remove the reservation'
            )
        elif code in live:
            problems.append(
                f'FACADE_RESERVED lists {code} as reserved, but it is raised at {live[code]}; '
                f'the reservation is stale'
            )

    return declared, live, problems


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
    contract_declared, contract_used, contract_problems = contract_codes(root)
    facade_declared, facade_live, facade_problems = facade_codes(root)

    malformed = [description for description in ranges if 'is inverted' in description or 'crosses' in description]
    documented = singles | covered

    errors: list[str] = list(contract_problems) + list(facade_problems)
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

    # Module-contract family: symbol-shaped codes. Ranges are expanded against the declaration file so a
    # range spanning a numbering gap fails instead of silently documenting a code that cannot exist.
    reference_text = reference.read_text(encoding='utf-8')
    contract_documented = set(CONTRACT_CODE.findall(reference_text))
    contract_by_range: set[str] = set()

    for match in CONTRACT_RANGE.finditer(reference_text):
        low_family, low_digits, high_family, high_digits = match.groups()
        if low_family != high_family:
            errors.append(
                f'docs/reference/diagnostics.md: range `{low_family}-{low_digits}`-`{high_family}-{high_digits}` '
                f'crosses a family boundary'
            )
            continue
        low_n, high_n = int(low_digits), int(high_digits)
        if high_n < low_n:
            errors.append(
                f'docs/reference/diagnostics.md: range `{low_family}-{low_digits}`-`{high_family}-{high_digits}` is inverted'
            )
            continue
        for number in range(low_n, high_n + 1):
            contract_by_range.add(f'{low_family}-{number:03d}')
    contract_documented |= contract_by_range

    # A code mentioned in backticks outside a table row (prose discussing a gap) is not coverage. A code
    # covered by a documented range is exempt: the range is its table row.
    for code in sorted(contract_documented - contract_by_range):
        if not re.search(rf'\|[^\n]*`{re.escape(code)}`', reference_text):
            errors.append(
                f'docs/reference/diagnostics.md: {code} appears only in prose, not on a table row; '
                f'a code mentioned while discussing a gap is not documented coverage'
            )

    for code in sorted(set(contract_declared) - contract_documented):
        errors.append(
            f'docs/reference/diagnostics.md: {code} is emitted at {contract_used.get(code, "an unknown site")} '
            f'but no row covers it; add it to the module-contract row'
        )

    for code in sorted(contract_documented - set(contract_declared)):
        errors.append(
            f'docs/reference/diagnostics.md: {code} is documented but '
            f'{CONTRACT_DECLARATION.as_posix()} does not declare it; '
            f'remove it, or declare and reference it if it is genuinely new'
        )

    if errors:
        print('diagnostic-code check failed:')
        for error in errors:
            print(f'- {error}')
        return 1

    print(
        f'Diagnostic-code check passed: {len(emitted)} emitted codes documented '
        f'({len(ranges)} ranges, {len(singles - covered)} single codes), '
        f'{len(contract_declared)} module-contract codes documented and referenced, '
        f'{len(facade_declared)} facade codes declared ({len(facade_live)} raised, '
        f'{len(facade_declared)-len(facade_live)} reserved).'
    )
    return 0


if __name__ == '__main__':
    sys.exit(main())
