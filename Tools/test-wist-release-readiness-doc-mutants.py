#!/usr/bin/env python3
from __future__ import annotations

import json
import shutil
import subprocess
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXCLUDED = shutil.ignore_patterns(
    '.git', 'artifacts', 'bin', 'obj', 'node_modules', 'packages', '.cache'
)


def run_validator(root: Path) -> int:
    return subprocess.run(
        ['python3', str(root / 'Tools' / 'check-wist-release-readiness-docs.py')],
        cwd=root,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    ).returncode


def clone_for_mutant() -> tuple[tempfile.TemporaryDirectory[str], Path]:
    temporary = tempfile.TemporaryDirectory(prefix='wist-readiness-mutant-')
    mutant_root = Path(temporary.name) / 'repo'
    shutil.copytree(ROOT, mutant_root, ignore=EXCLUDED)
    return temporary, mutant_root


def stale_release_evidence_total_must_fail() -> None:
    temporary, mutant_root = clone_for_mutant()
    try:
        release_state = json.loads(
            (mutant_root / 'eng' / 'documentation-release-state.json').read_text(encoding='utf-8')
        )
        stability = mutant_root / release_state['stabilityDocument']
        text = stability.read_text(encoding='utf-8')
        canonical_total = json.loads(
            (mutant_root / 'eng' / 'test-counts.json').read_text(encoding='utf-8')
        )['totalPassed']
        formatted = f'{canonical_total:,}'
        if formatted not in text:
            raise RuntimeError('test setup failed: canonical total is not present in stability evidence')
        stability.write_text(text.replace(formatted, '9,999', 1), encoding='utf-8')
        if run_validator(mutant_root) == 0:
            raise RuntimeError('Wist release-readiness mutant survived: stale-release-evidence-total')
        print('SURVIVOR=0 mutant=stale-release-evidence-total')
    finally:
        temporary.cleanup()


def rollout_formula_drift_must_fail() -> None:
    temporary, mutant_root = clone_for_mutant()
    try:
        recipe = mutant_root / 'docs' / 'start' / 'use-case-recipes.md'
        text = recipe.read_text(encoding='utf-8')
        original = 'wist-rollout-formula-contract: usage * 0.7 + reliability * 0.3 - incidents * 15.0'
        mutant = 'wist-rollout-formula-contract: usage * 0.6 + reliability * 0.4 - incidents * 15.0'
        if original not in text:
            raise RuntimeError('test setup failed: rollout formula marker is not present')
        recipe.write_text(text.replace(original, mutant, 1), encoding='utf-8')
        if run_validator(mutant_root) == 0:
            raise RuntimeError('Wist release-readiness mutant survived: rollout-formula-drift')
        print('SURVIVOR=0 mutant=rollout-formula-drift')
    finally:
        temporary.cleanup()


def main() -> int:
    stale_release_evidence_total_must_fail()
    rollout_formula_drift_must_fail()
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
