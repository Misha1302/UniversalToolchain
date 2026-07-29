#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
import subprocess
import tempfile
from pathlib import Path

EXCLUDED = shutil.ignore_patterns('.git', 'artifacts', 'bin', 'obj', 'node_modules', 'packages', '.cache')


def run(checker: Path, root: Path) -> int:
    return subprocess.run(
        ['python3', str(checker), '--root', str(root)],
        text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False).returncode


def mutate_and_expect_failure(source_root: Path, relative: str, addition: str, name: str) -> None:
    with tempfile.TemporaryDirectory(prefix='documentation-mutant-') as tmp_name:
        mutant_root = Path(tmp_name) / 'repo'
        shutil.copytree(source_root, mutant_root, ignore=EXCLUDED)
        path = mutant_root / relative
        path.write_text(path.read_text(encoding='utf-8') + addition, encoding='utf-8')
        if run(mutant_root / 'Tools/check_documentation_status.py', mutant_root) == 0:
            raise RuntimeError(f'documentation mutant survived: {name}')
        print(f'SURVIVOR=0 mutant={name}')


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve()
    mutate_and_expect_failure(root, 'docs/evidence/current-verification.md', '\nCurrent verification: 1508 tests.\n', 'stale-test-total')
    mutate_and_expect_failure(root, 'RELEASE_CHECKLIST.md', '\n- [x] clean-unpack recursive manifest verification passed\n', 'obsolete-source-manifest-claim')
    mutate_and_expect_failure(root, 'RELEASE_NOTES_RU.md', '\nTotal: 1545 passed\n', 'alternative-active-total')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
