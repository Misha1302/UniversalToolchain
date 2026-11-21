#!/usr/bin/env python3
"""Prove that check-contract-diagnostic-tests.py fails when it should.

The gate claims that every declared module-contract diagnostic is either observed by a test or
explicitly recorded as untested. A gate like that is only worth its output if it has been *seen* to
reject the situations it forbids, so each mutant below is a plausible way to make a code look covered
while nothing actually observes it.

  drop-affirmative-assertion - remove the assertion that makes a diagnostic appear
  negative-only-coverage     - cover a code exclusively with `Does.Not.Contain`, which is the exact
                               failure this gate was written to stop (UT-CONTRACT-009)
  delete-test-file           - delete a whole test file, so its codes become unobserved
  stale-reservation          - record a code as deliberately untested after it acquired a real test
  reservation-not-declared   - reserve a code that no constants file declares
  no-codes-at-all            - empty the constants file, which must not be reported as success

The last one matters because an empty declared set would otherwise make every loop trivially pass. That
is the same shape as the tautological `ValidateComplete()` in `ContractFacetKindOrder`, where the
enumeration can never produce a value the table is missing.

Mutants are applied to a throw-away copy of the tree so the working tree is never modified.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

CHECKER = Path('Tools/check-contract-diagnostic-tests.py')
DECLARATION = Path('UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractDiagnosticCodes.cs')
ARTIFICIAL_TAG = Path('UniversalToolchain/Tests/Internal/ModuleContracts/ContractFacetDuplicateIdTests.cs')
EXCLUDED = shutil.ignore_patterns('.git', 'artifacts', 'bin', 'obj', 'node_modules', 'packages', '.cache')


def run(root: Path) -> tuple[int, str]:
    completed = subprocess.run(
        [sys.executable, str(root / CHECKER), '--root', str(root)],
        text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False)
    return completed.returncode, completed.stdout


def mutate(source_root: Path, name: str, apply) -> None:
    with tempfile.TemporaryDirectory(prefix='contract-diag-tests-mutant-') as tmp_name:
        mutant_root = Path(tmp_name) / 'repo'
        shutil.copytree(source_root, mutant_root, ignore=EXCLUDED)
        apply(mutant_root)
        code, output = run(mutant_root)
        if code == 0:
            raise RuntimeError(f'contract-diagnostic-test gate let a mutant survive: {name}\n{output}')
        print(f'SURVIVOR=0 mutant={name}')


def edit(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding='utf-8')
    if old not in text:
        raise RuntimeError(f'mutant setup failed: anchor not found in {path}:\n{old}')
    path.write_text(text.replace(old, new, 1), encoding='utf-8')


# UT-AIR-BRANCH-002 has exactly one affirmative assertion in the suite, so removing it makes the code
# unobserved. A code covered by two tests would not move, which is why this targets a single-witness code
# on purpose: a mutant that changes nothing measures nothing.
AIR_DUPLICATE_TEST = 'UniversalToolchain/Tests/Internal/ModuleContracts/AirVerifierTests.cs'
AIR_DUPLICATE_ASSERTION = '''
        Assert.That(result.Diagnostics.Select(static x => x.Code),
            Has.Exactly(1).EqualTo(ModuleContractDiagnosticCodes.DuplicateAirLabel));'''


def drop_affirmative_assertion(root: Path) -> None:
    """Remove the only assertion that observes UT-AIR-BRANCH-002 firing."""
    edit(
        root / AIR_DUPLICATE_TEST,
        AIR_DUPLICATE_ASSERTION,
        '''
        Assert.That(result.Diagnostics, Is.Not.Null);''')


def negative_only_coverage(root: Path) -> None:
    """Replace the real assertion with a `Does.Not.Contain`, exactly as UT-CONTRACT-009 was covered.

    The gate must reject this: asserting a code is absent is compatible with the code never firing.
    """
    edit(
        root / AIR_DUPLICATE_TEST,
        AIR_DUPLICATE_ASSERTION,
        '''
        Assert.That(
            result.Diagnostics.Select(static x => x.Code),
            Does.Not.Contain(ModuleContractDiagnosticCodes.DuplicateAirLabel));''')


def delete_test_file(root: Path) -> None:
    """Delete the file whose tests observe the pipeline-effect codes."""
    (root / 'UniversalToolchain/Tests/Internal/ModuleContracts/PipelineEffectContractValidatorTests.cs').unlink()


def stale_reservation(root: Path) -> None:
    """Reserve a code that does have a real, affirmative test."""
    target = root / CHECKER
    edit(target, 'CONTRACT_UNTESTED: dict[str, str] = {',
         "CONTRACT_UNTESTED: dict[str, str] = {'UT-MOD-OWNERSHIP-003': 'stale', ")


def reservation_not_declared(root: Path) -> None:
    """Reserve a code that nothing declares, which is a record about a non-existent diagnostic."""
    target = root / CHECKER
    edit(target, 'CONTRACT_UNTESTED: dict[str, str] = {',
         "CONTRACT_UNTESTED: dict[str, str] = {'UT-CONTRACT-MISSING-001': 'invented', ")


def no_codes_at_all(root: Path) -> None:
    """Empty the constants file; an empty set must fail closed, not pass vacuously."""
    path = root / DECLARATION
    text = path.read_text(encoding='utf-8')
    lines = [
        '    public const string Placeholder = "NOT-A-CONTRACT-CODE";' if 'public const string' in line else line
        for line in text.splitlines()
    ]
    path.write_text('\n'.join(lines) + '\n', encoding='utf-8')


MUTANTS = {
    'drop-affirmative-assertion': drop_affirmative_assertion,
    'negative-only-coverage': negative_only_coverage,
    'delete-test-file': delete_test_file,
    'stale-reservation': stale_reservation,
    'reservation-not-declared': reservation_not_declared,
    'no-codes-at-all': no_codes_at_all,
}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve()

    # A gate whose baseline already fails proves nothing about the mutants.
    code, output = run(root)
    if code != 0:
        print('baseline already failing; mutants would be meaningless:', file=sys.stderr)
        print(output, file=sys.stderr)
        return 1

    for name, apply in MUTANTS.items():
        mutate(root, name, apply)

    print(f'All {len(MUTANTS)} contract-diagnostic-test mutants killed.')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
