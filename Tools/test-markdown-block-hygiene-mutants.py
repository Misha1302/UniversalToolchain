#!/usr/bin/env python3
"""Prove that check-markdown-block-hygiene.py fails when it should.

A gate that has only been seen to pass is not evidence. Each mutant below is a plausible way the
original defect could return, and each must be rejected:

  unmarked-regeneration     - the exact historical defect: a regeneration recipe with no marker
  unmarked-source-write     - the same failure in another shape: a block redirecting into a tracked file
  marker-inside-body        - the marker placed in the code instead of the fence attributes, where the
                              runner cannot see it
  mixed-case-marker         - a marker that reads correctly to a human but not, depending on the parser,
                              to the runner

The last two matter because the gate and the runner must agree about what "marked" means. A gate that
accepts a marker the runner ignores would bless a block that still executes in CI.

Mutants are applied to a throw-away copy of the tree so the working tree is never modified.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

CHECKER = Path('Tools/check-markdown-block-hygiene.py')
EXCLUDED = shutil.ignore_patterns('.git', 'artifacts', 'bin', 'obj', 'node_modules', 'packages', '.cache')

DOC = Path('docs/architecture/block-hygiene-fixture.md')

BASELINE = '# Fixture\n\n## Regenerating\n\n```bash ci-run=false\nWIST_REGENERATE_PARITY_CORPUS=1 dotnet test\n```\n'

MUTANTS = {
    'unmarked-regeneration': BASELINE.replace('```bash ci-run=false', '```bash'),
    'unmarked-source-write': BASELINE + '\n```bash\ndotnet run > UniversalToolchain/Generated.json\n```\n',
    'marker-inside-body': BASELINE.replace('```bash ci-run=false', '```bash') .replace(
        'WIST_REGENERATE_PARITY_CORPUS=1 dotnet test', '# ci-run=false\nWIST_REGENERATE_PARITY_CORPUS=1 dotnet test'),
    'mixed-case-marker': BASELINE.replace('```bash ci-run=false', '```bash CI-RUN=FALSE'),
}


def run(root: Path) -> int:
    return subprocess.run(
        [sys.executable, str(root / CHECKER), '--root', str(root)],
        text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False).returncode


def try_mutant(source_root: Path, name: str, content: str) -> bool:
    with tempfile.TemporaryDirectory(prefix='block-hygiene-mutant-') as tmp_name:
        mutant_root = Path(tmp_name) / 'repo'
        shutil.copytree(source_root, mutant_root, ignore=EXCLUDED)
        fixture = mutant_root / DOC
        fixture.parent.mkdir(parents=True, exist_ok=True)
        fixture.write_text(content, encoding='utf-8')

        killed = run(mutant_root) != 0
        print(f'  {"KILLED " if killed else "SURVIVED"} {name}')
        return killed


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', default='.')
    arguments = parser.parse_args()
    root = Path(arguments.root).resolve()

    if run(root) != 0:
        print('the baseline tree already fails the gate; fix that before running mutants', file=sys.stderr)
        return 1

    print('Applying markdown block hygiene mutants:')
    survived = [name for name, content in MUTANTS.items() if not try_mutant(root, name, content)]

    if survived:
        print(f'Surviving mutants: {", ".join(survived)}', file=sys.stderr)
        return 1

    print(f'All {len(MUTANTS)} markdown block hygiene mutants killed.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
