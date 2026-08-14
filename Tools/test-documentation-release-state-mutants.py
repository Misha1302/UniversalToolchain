#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import tempfile
from pathlib import Path

EXCLUDED = shutil.ignore_patterns('.git', 'artifacts', 'bin', 'obj', 'node_modules', 'packages', '.cache')


def run(checker: Path, root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ['python3', str(checker), '--root', str(root)],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )


def require_killed(source_root: Path, name: str, mutate) -> None:
    with tempfile.TemporaryDirectory(prefix=f'documentation-release-{name}-') as tmp_name:
        mutant_root = Path(tmp_name) / 'repo'
        shutil.copytree(source_root, mutant_root, ignore=EXCLUDED)
        mutate(mutant_root)
        completed = run(mutant_root / 'Tools/check_documentation_release_state.py', mutant_root)
        if completed.returncode == 0:
            raise RuntimeError(f'{name} mutant survived:\n{completed.stdout}')
        print(f'SURVIVOR=0 mutant={name}')


def replace(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding='utf-8')
    if old not in text:
        raise RuntimeError(f'mutation precondition missing in {path}: {old}')
    path.write_text(text.replace(old, new, 1), encoding='utf-8')


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve()
    checker = root / 'Tools/check_documentation_release_state.py'
    state = json.loads((root / 'eng/documentation-release-state.json').read_text(encoding='utf-8'))
    source_version = state['sourceVersion']
    published_version = state['publishedVersion']
    stability_document = state['stabilityDocument']

    positive = run(checker, root)
    if positive.returncode != 0:
        raise RuntimeError('positive documentation release-state check failed:\n' + positive.stdout)
    print('positive-control=1 documentation-release-state')

    def stale_source_version(mutant: Path) -> None:
        state_path = mutant / 'eng/documentation-release-state.json'
        state = json.loads(state_path.read_text(encoding='utf-8'))
        state['sourceVersion'] = source_version + '.mutant'
        state_path.write_text(json.dumps(state, indent=2) + '\n', encoding='utf-8')

    require_killed(root, 'source-version-drift', stale_source_version)
    require_killed(
        root,
        'current-architecture-source-version-drift',
        lambda mutant: replace(
            mutant / 'docs/CURRENT_ARCHITECTURE_STATUS.md',
            f'`{source_version}`',
            '`0.0.0-mutant`',
        ),
    )
    require_killed(
        root,
        'published-install-drift',
        lambda mutant: replace(
            mutant / 'docs/start/first-program.md',
            f'--version {published_version}',
            '--version 0.0.0-mutant',
        ),
    )
    require_killed(
        root,
        'missing-stability-target',
        lambda mutant: (mutant / stability_document).unlink(),
    )
    require_killed(
        root,
        'unsafe-source-build-command',
        lambda mutant: replace(
            mutant / 'docs/start/installation.md',
            './build.sh --skip-docs --skip-pack',
            './build.sh --skip-docs',
        ),
    )
    require_killed(
        root,
        'workflow-version-literal',
        lambda mutant: (mutant / '.github/workflows/published-package-smoke.yml').write_text(
            (mutant / '.github/workflows/published-package-smoke.yml').read_text(encoding='utf-8')
            + '\nenv:\n  PUBLISHED_WIST_VERSION: 0.1.0-alpha.1\n',
            encoding='utf-8',
        ),
    )
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
