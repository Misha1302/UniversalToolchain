#!/usr/bin/env python3
"""Prove that check-diagnostic-codes.py fails when it should.

A gate that has never been seen to fail is not evidence of anything. Each mutant below is a real,
plausible drift between the diagnostics reference and the production source, and each must be
rejected:

  understated-range   - narrow a documented range so an emitted code falls outside it
  phantom-code        - widen a documented range so a code that nothing emits becomes "documented"
  drop-single-code    - remove coverage for a code documented outside any range
  renaming-drift      - change an emitted code in source without touching the reference

The mutants are applied to a throw-away copy of the tree so the working tree is never modified.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

CHECKER = Path('Tools/check-diagnostic-codes.py')
REFERENCE = Path('docs/reference/diagnostics.md')
EXCLUDED = shutil.ignore_patterns('.git', 'artifacts', 'bin', 'obj', 'node_modules', 'packages', '.cache')


def run(root: Path) -> int:
    return subprocess.run(
        [sys.executable, str(root / CHECKER), '--root', str(root)],
        text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False).returncode


def mutate(source_root: Path, name: str, apply) -> None:
    with tempfile.TemporaryDirectory(prefix='diagnostic-codes-mutant-') as tmp_name:
        mutant_root = Path(tmp_name) / 'repo'
        shutil.copytree(source_root, mutant_root, ignore=EXCLUDED)
        apply(mutant_root)
        if run(mutant_root) == 0:
            raise RuntimeError(f'diagnostic-code gate let a mutant survive: {name}')
        print(f'SURVIVOR=0 mutant={name}')


def edit(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding='utf-8')
    if old not in text:
        raise RuntimeError(f'mutant setup failed: anchor not found in {path}:\n{old}')
    path.write_text(text.replace(old, new, 1), encoding='utf-8')


def understated_range(root: Path) -> None:
    """Document the typed-route family as ending at 2204, as the file once did."""
    edit(root / REFERENCE, '`UTL2201`\u2013`UTL2209`', '`UTL2201`\u2013`UTL2204`')


def phantom_code(root: Path) -> None:
    """Widen a range past the highest code that exists, inventing coverage."""
    edit(root / REFERENCE, '`UTL2201`\u2013`UTL2209`', '`UTL2201`\u2013`UTL2215`')


def drop_single_code(root: Path) -> None:
    """Remove UTL1501, which is documented as a bare code rather than inside a range."""
    edit(root / REFERENCE, '| Toolchain API | `UTL1501` | definition/package API incompatibility |',
         '| Toolchain API | none | definition/package API incompatibility |')


def renaming_drift(root: Path) -> None:
    """Rename one emitted code in production source without updating the reference."""
    target = root / 'UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs'
    edit(target, '"UTL2207"', '"UTL2299"')


# Module-contract family. These exercise the half of the gate that cannot be reached by a literal scan,
# because the codes live in one constants file and are referenced by name.

def contract_undeclared_code(root: Path) -> None:
    """Document a module-contract code that no constant declares."""
    edit(root / REFERENCE, '| backend capabilities |', '| planted | `UT-AIR-PLANTED-001` | invented |\n| backend capabilities |')


def contract_dead_constant(root: Path) -> None:
    """Add a declared-but-never-referenced code: it documents a diagnostic that cannot happen."""
    target = root / 'UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractDiagnosticCodes.cs'
    edit(target, '    public const string MultipleBackendCapabilityFacets = "UT-BACKEND-CAPABILITY-002";',
         '    public const string MultipleBackendCapabilityFacets = "UT-BACKEND-CAPABILITY-002";\n'
         '    public const string DeadDiagnostic = "UT-AIR-DEAD-001";')


def contract_renaming_drift(root: Path) -> None:
    """Rename a declared code in the constants file without touching the reference."""
    target = root / 'UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractDiagnosticCodes.cs'
    edit(target, '"UT-AIR-STACK-001"', '"UT-AIR-STACK-009"')


def contract_reference_drift(root: Path) -> None:
    """Reference a constant that the constants file does not declare."""
    target = root / 'UniversalToolchain/UniversalToolchain.ModuleContracts/AirVerifier.cs'
    edit(target, 'ModuleContractDiagnosticCodes.UnsupportedAirIntrinsic',
         'ModuleContractDiagnosticCodes.UnsupportedAirIntrinsicRenamed')


def contract_range_over_gap(root: Path) -> None:
    """Cover a numbering gap with a range, which would advertise a code that does not exist."""
    edit(root / REFERENCE, '`UT-CONTRACT-001`\u2013`UT-CONTRACT-003`, `UT-CONTRACT-005`',
         '`UT-CONTRACT-001`\u2013`UT-CONTRACT-003`, `UT-CONTRACT-004`, `UT-CONTRACT-005`')


def contract_prose_only(root: Path) -> None:
    """Mention an undeclared code in prose so it reads as documented without a table row."""
    edit(root / REFERENCE, '## Handling pattern',
         'A reader may also meet `UT-AIR-PROSE-001` while debugging.\n\n## Handling pattern')


CONTRACT_MUTANTS = {
    'contract-undeclared-code': contract_undeclared_code,
    'contract-dead-constant': contract_dead_constant,
    'contract-renaming-drift': contract_renaming_drift,
    'contract-reference-drift': contract_reference_drift,
    'contract-range-over-gap': contract_range_over_gap,
    'contract-prose-only': contract_prose_only,
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve()

    mutate(root, 'understated-range', understated_range)
    mutate(root, 'phantom-code', phantom_code)
    mutate(root, 'drop-single-code', drop_single_code)
    mutate(root, 'renaming-drift', renaming_drift)

    for name, apply in CONTRACT_MUTANTS.items():
        mutate(root, name, apply)

    print(f'All {4 + len(CONTRACT_MUTANTS)} diagnostic-code mutants killed.')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
