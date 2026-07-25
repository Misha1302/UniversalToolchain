#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
INTERNAL = ROOT / "internal-docs"
EXCLUDED_DIRS = {'.git', '.hg', '.svn', '.cache', '.idea', '.vs', '.vscode', 'artifacts', 'bin', 'dist', 'node_modules', 'obj', 'packages'}
INTERNAL_PUBLIC_NAMES = {'archive', 'reviews', 'proposals', 'talks', 'maintainers', 'contracts', 'vision'}


def iter_markdown_files(base: Path) -> list[Path]:
    if not base.exists():
        return []
    return sorted(
        path for path in base.rglob('*.md')
        if not any(part in EXCLUDED_DIRS for part in path.relative_to(ROOT).parts)
        and '.vitepress' not in path.parts
    )


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def has_heading(text: str) -> bool:
    return any(line.startswith('# ') for line in text.splitlines())


def has_front_matter(text: str) -> bool:
    return text.startswith('---\n') and '\n---\n' in text[4:]


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
    public_files = iter_markdown_files(DOCS)
    internal_files = iter_markdown_files(INTERNAL)

    if not INTERNAL.exists():
        errors.append('internal-docs/: public/internal documentation split is missing')

    for name in sorted(INTERNAL_PUBLIC_NAMES):
        if (DOCS / name).exists():
            errors.append(f'docs/{name}: internal material must live under internal-docs/')

    for path in public_files + internal_files:
        text = path.read_text(encoding='utf-8')
        if not has_heading(text):
            errors.append(f'{rel(path)}: missing top-level Markdown heading')
        try:
            fences = extract_bash_fences(text)
        except RuntimeError as exc:
            errors.append(f'{rel(path)}: {exc}')
            continue
        for start, attrs, content in fences:
            if 'dotnet test UniversalToolchain/Wist.sln' in content and 'ci-run=false' not in attrs:
                errors.append(f'{rel(path)}:{start}: full solution test command must be ci-run=false')
            if 'dotnet build UniversalToolchain/Wist.sln' in content and 'ci-run=false' not in attrs:
                errors.append(f'{rel(path)}:{start}: full solution build command must be ci-run=false')

    required_public = [
        'index.md',
        'start/index.md',
        'start/installation.md',
        'language-authoring/index.md',
        'language-authoring/quickstart.md',
        'language-authoring/package-model.md',
        'language-authoring/contribution-planning.md',
        'language-authoring/artifact-routing.md',
        'language-authoring/runtime-lifecycle.md',
        'language-authoring/testing-and-templates.md',
        'language-authoring/versioning-and-migrations.md',
        'architecture/learning-path.md',
        'architecture/project-map.md',
        'architecture/lowering-walkthrough.md',
        'architecture/composition-explain-plan.md',
        'architecture/external-language-authoring-sdk.md',
        'reference/diagnostics.md',
        'reference/lifecycle-concurrency-privacy.md',
        'reference/performance-model.md',
        'evidence/index.md',
        'evidence/maintainer-guide.md',
        'evidence/current-verification.md',
        'evidence/language-authoring-alpha.md',
        'evidence/wist-stability-v0.1.0-alpha.1.md',
        'CURRENT_ARCHITECTURE_STATUS.md',
        'SECURITY.md',
        'limitations.md',
    ]
    for name in required_public:
        path = DOCS / name
        if not path.exists():
            errors.append(f'docs/{name}: required public documentation page is missing')
        elif not has_front_matter(path.read_text(encoding='utf-8')):
            errors.append(f'docs/{name}: canonical public page must have front matter')

    public_static = DOCS / 'public'
    for path in public_static.rglob('*.md') if public_static.exists() else []:
        errors.append(f'{rel(path)}: Markdown must not live in VitePress public/ static assets')

    package_json = ROOT / 'package.json'
    if not package_json.exists():
        errors.append('package.json: missing')
    else:
        package = json.loads(package_json.read_text(encoding='utf-8'))
        scripts = package.get('scripts', {})
        for script in ('docs:status', 'docs:links', 'docs:build', 'docs:check'):
            if script not in scripts:
                errors.append(f'package.json: missing {script} script')

    verification = ROOT / 'VERIFICATION.md'
    verification_text = verification.read_text(encoding='utf-8') if verification.exists() else ''
    for expected in ('1,473', 'UniversalToolchain.PlanFuzz.Tests', 'UniversalToolchain.PlanFuzz.IntegrationTests'):
        if expected not in verification_text:
            errors.append(f'VERIFICATION.md: integrated verification marker is missing: {expected}')

    public_text = '\n'.join(path.read_text(encoding='utf-8') for path in public_files)
    forbidden = {
        'backend-agnostic artifact handling is not fully generalized': 'implemented neutral/generic runtime is described as future-only',
        'Introduce backend-agnostic compiled/executable artifact contracts': 'implemented artifact contracts are described as future work',
        'Current verification: 1,325 tests': 'stale test count remains',
        'docs/public/what-is-stable-in-alpha.md': 'static-public Markdown path remains',
        'docs/public/performance-model.md': 'static-public Markdown path remains',
        'LogsViewer/server.py': 'removed LogsViewer path remains in public docs',
    }
    for phrase, reason in forbidden.items():
        if phrase in public_text:
            errors.append(f'public docs: {reason}: {phrase}')

    root_readme = (ROOT / 'readme.md').read_text(encoding='utf-8')
    if '](docs/maintainers/' in root_readme or '](docs/talks/' in root_readme:
        errors.append('readme.md: maintainer/talk links still point into public docs/')

    quickstart = (DOCS / 'language-authoring' / 'quickstart.md').read_text(encoding='utf-8')
    if '--no-build --no-restore --project samples/Acme.PricingLanguage' in quickstart:
        errors.append('docs/language-authoring/quickstart.md: clean-start command still skips restore/build')
    if 'UniversalToolchain.Templates::<version>' in public_text:
        errors.append('public docs: template installation still contains a non-runnable <version> placeholder')
    if 'Legacy Wist Module Authoring' in public_text:
        errors.append('public docs: supported Wist module authoring is still labeled Legacy')

    config_text = (DOCS / '.vitepress' / 'config.mts').read_text(encoding='utf-8')
    for prefix in ('/start/', '/language-authoring/', '/build-dsls/', '/write-modules/', '/architecture/', '/reference/', '/evidence/'):
        if f"'{prefix}'" not in config_text:
            errors.append(f'docs/.vitepress/config.mts: missing role/path-specific sidebar for {prefix}')

    home = (DOCS / 'index.md').read_text(encoding='utf-8')
    for role in ('Wist application developer', 'External language author', 'Wist dialect author', 'Wist compiler contributor', 'Security or platform reviewer', 'Maintainer or evaluator'):
        if role not in home:
            errors.append(f'docs/index.md: role card is missing: {role}')

    role_pages = [
        'index.md',
        'start/index.md',
        'language-authoring/index.md',
        'build-dsls/index.md',
        'write-modules/index.md',
        'architecture/learning-path.md',
        'architecture/project-map.md',
        'reference/index.md',
        'evidence/index.md',
        'evidence/maintainer-guide.md',
        'SECURITY.md',
        'limitations.md',
    ]
    for name in role_pages:
        text = (DOCS / name).read_text(encoding='utf-8')
        front = text.split('---', 2)[1] if text.startswith('---') else ''
        for key in ('audience:', 'status:', 'lastVerifiedAgainst:'):
            if key not in front:
                errors.append(f'docs/{name}: missing role/status front-matter field {key[:-1]}')

    if errors:
        print('Documentation status check failed:', file=sys.stderr)
        for error in errors:
            print(f'- {error}', file=sys.stderr)
        return 1

    print(
        f'Documentation status check passed: {len(public_files)} public Markdown files, '
        f'{len(internal_files)} internal Markdown files.'
    )
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
