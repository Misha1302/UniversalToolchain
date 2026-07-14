#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXCLUDED_DIRS = {'.git', '.hg', '.svn', '.cache', '.idea', '.vs', '.vscode', 'artifacts', 'bin', 'dist', 'node_modules', 'obj', 'packages'}


def iter_markdown_files() -> list[Path]:
    files: list[Path] = []
    for path in ROOT.rglob('*.md'):
        rel = path.relative_to(ROOT)
        if any(part in EXCLUDED_DIRS for part in rel.parts):
            continue
        files.append(path)
    return sorted(files)


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def has_heading(text: str) -> bool:
    return any(line.startswith('# ') for line in text.splitlines())


def extract_bash_fences(text: str) -> list[tuple[int, str, str]]:
    fences: list[tuple[int, str, str]] = []
    lines = text.splitlines()
    in_fence = False
    start = 0
    attrs = ''
    content: list[str] = []
    for index, line in enumerate(lines, start=1):
        stripped = line.strip()
        if not in_fence:
            if stripped.startswith('```bash'):
                in_fence = True
                start = index
                attrs = stripped[len('```bash'):].strip()
                content = []
            continue
        if stripped == '```':
            fences.append((start, attrs, '\n'.join(content)))
            in_fence = False
            continue
        content.append(line)
    if in_fence:
        raise RuntimeError(f'Unterminated bash fence starting at line {start}')
    return fences


def main() -> int:
    errors: list[str] = []
    markdown_files = iter_markdown_files()

    for path in markdown_files:
        text = path.read_text(encoding='utf-8')
        relative = rel(path)
        if not has_heading(text):
            errors.append(f'{relative}: missing top-level markdown heading')
        try:
            fences = extract_bash_fences(text)
        except RuntimeError as exc:
            errors.append(f'{relative}: {exc}')
            continue
        for start, attrs, content in fences:
            if 'dotnet test UniversalToolchain/Wist.sln' in content and 'ci-run=false' not in attrs:
                errors.append(f'{relative}:{start}: full solution test command must be ci-run=false in docs')
            if 'dotnet build UniversalToolchain/Wist.sln' in content and 'ci-run=false' not in attrs:
                errors.append(f'{relative}:{start}: full solution build command must be ci-run=false in docs')

    required_files = [
        ROOT / 'Tools/check_documentation_status.py',
        ROOT / '.github/scripts/run-markdown-bash-blocks.py',
        ROOT / 'docs/start/installation.md',
        ROOT / 'docs/CURRENT_ARCHITECTURE_STATUS.md',
        ROOT / 'docs/public/what-is-stable-in-alpha.md',
        ROOT / 'UniversalToolchain/Dialects/examples/wist/function-calls-safe-math/README.md',
    ]
    for path in required_files:
        if not path.exists():
            errors.append(f'{rel(path)}: required documentation/status file is missing')

    package_json = ROOT / 'package.json'
    if package_json.exists() and '"docs:status"' not in package_json.read_text(encoding='utf-8'):
        errors.append('package.json: missing docs:status script')

    forbidden_legacy_stage_phrases = [
        'what-is-stable-in-preview',
        'public preview',
        'current preview',
        'preview package',
        'Wist facade preview',
        'Preview status',
    ]
    for path in markdown_files:
        text = path.read_text(encoding='utf-8')
        for phrase in forbidden_legacy_stage_phrases:
            if phrase in text:
                errors.append(f'{rel(path)}: legacy preview-stage wording remains: {phrase}')

    docs_index = ROOT / 'docs/index.md'
    if docs_index.exists():
        text = docs_index.read_text(encoding='utf-8')
        if 'CompileFunc<double, double, double>' in text.split('<!-- langdev-2026-site:start -->', 1)[0]:
            errors.append('docs/index.md: first demo still uses CompileFunc instead of Compile<TDelegate>')

    if errors:
        print('Documentation status check failed:', file=sys.stderr)
        for error in errors:
            print(f'- {error}', file=sys.stderr)
        return 1

    print(f'Documentation status check passed for {len(markdown_files)} Markdown files.')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
