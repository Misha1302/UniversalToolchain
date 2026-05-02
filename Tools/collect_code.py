#!/usr/bin/env python3
from __future__ import annotations

import argparse
import fnmatch
import os
import subprocess
from pathlib import Path
from typing import Iterable


CODE_EXTENSIONS = {
    # C# / .NET
    ".cs", ".csproj", ".sln", ".props", ".targets", ".fs", ".fsproj", ".vb", ".vbproj",

    # Web / docs tooling
    ".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs", ".vue", ".svelte",
    ".html", ".css", ".scss", ".sass", ".less",

    # Config / data useful for project analysis
    ".json", ".jsonc", ".yaml", ".yml", ".toml", ".xml", ".ini",
    ".editorconfig", ".config",

    # Scripts
    ".sh", ".bash", ".zsh", ".ps1", ".bat", ".cmd", ".py", ".rb", ".pl",

    # Other common code
    ".cpp", ".hpp", ".c", ".h", ".rs", ".go", ".java", ".kt", ".kts",
    ".swift", ".php", ".sql", ".dockerfile",
}

IMPORTANT_DOC_NAMES = {
    "README.md",
    "PROJECT_RULES.md",
    "AGENTS.md",
    "CONTRIBUTING.md",
    "ARCHITECTURE.md",
    "CHANGELOG.md",
}

SPECIAL_FILENAMES = {
    "Dockerfile",
    "docker-compose.yml",
    "docker-compose.yaml",
    "Makefile",
    "Directory.Build.props",
    "Directory.Build.targets",
    "global.json",
    "package.json",
    "vite.config.ts",
    "vite.config.js",
    "tsconfig.json",
}

ALWAYS_SKIP_PATTERNS = [
    ".git/*",
    ".idea/*",
    ".vs/*",
    ".vscode/*",

    # Build/cache folders, even if accidentally tracked.
    "bin/*",
    "obj/*",
    "node_modules/*",
    "dist/*",
    "build/*",
    "coverage/*",
    ".vitepress/cache/*",
    ".vitepress/dist/*",

    # Binary/media/archive artifacts.
    "*.dll", "*.exe", "*.pdb", "*.so", "*.dylib", "*.a", "*.lib",
    "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.ico", "*.svg",
    "*.pdf", "*.zip", "*.tar", "*.gz", "*.7z", "*.rar",
    "*.mp4", "*.mov", "*.mp3", "*.wav",

    # Secrets / private material.
    ".env",
    ".env.*",
    "*.pem",
    "*.key",
    "*.pfx",
    "*.p12",
    "id_rsa",
    "id_rsa.*",
    "*secret*",
    "*secrets*",
    "appsettings.Production.json",
]


def run_git(args: list[str], cwd: Path) -> bytes:
    result = subprocess.run(
        ["git", *args],
        cwd=cwd,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )

    if result.returncode != 0:
        raise RuntimeError(result.stderr.decode("utf-8", errors="replace").strip())

    return result.stdout


def get_repo_root(start: Path) -> Path:
    output = run_git(["rev-parse", "--show-toplevel"], start)
    return Path(output.decode("utf-8", errors="replace").strip()).resolve()


def get_git_files(repo_root: Path) -> list[Path]:
    """
    Returns tracked files + untracked files not ignored by .gitignore/.git/info/exclude/global excludes.
    """
    output = run_git(
        ["ls-files", "--cached", "--others", "--exclude-standard", "-z"],
        repo_root,
    )

    files: list[Path] = []

    for raw in output.split(b"\0"):
        if not raw:
            continue

        rel = raw.decode("utf-8", errors="replace")
        files.append(Path(rel))

    return sorted(set(files), key=lambda p: p.as_posix().lower())


def matches_any(path: Path, patterns: Iterable[str]) -> bool:
    normalized = path.as_posix()

    for pattern in patterns:
        if fnmatch.fnmatch(normalized, pattern):
            return True

        # Also match by filename for patterns like "*.cs" or ".env".
        if fnmatch.fnmatch(path.name, pattern):
            return True

    return False


def is_probably_binary(data: bytes) -> bool:
    if b"\0" in data:
        return True

    if not data:
        return False

    sample = data[:4096]
    control_bytes = sum(1 for b in sample if b < 9 or (13 < b < 32))
    return control_bytes / len(sample) > 0.20


def should_include_file(
    rel_path: Path,
    *,
    output_path: Path,
    include_docs: bool,
    all_text: bool,
) -> bool:
    if rel_path == output_path:
        return False

    if matches_any(rel_path, ALWAYS_SKIP_PATTERNS):
        return False

    if all_text:
        return True

    if rel_path.name in SPECIAL_FILENAMES:
        return True

    if rel_path.name in IMPORTANT_DOC_NAMES:
        return True

    suffix = rel_path.suffix.lower()

    if suffix in CODE_EXTENSIONS:
        return True

    if include_docs and suffix in {".md", ".mdx", ".rst", ".txt"}:
        return True

    return False


def read_text_file(path: Path) -> str:
    data = path.read_bytes()

    if is_probably_binary(data):
        raise ValueError("binary file")

    for encoding in ("utf-8-sig", "utf-8", "cp1251"):
        try:
            return data.decode(encoding)
        except UnicodeDecodeError:
            pass

    return data.decode("utf-8", errors="replace")


def detect_language(path: Path) -> str:
    suffix = path.suffix.lower()

    mapping = {
        ".cs": "csharp",
        ".csproj": "xml",
        ".props": "xml",
        ".targets": "xml",
        ".sln": "text",
        ".ts": "typescript",
        ".tsx": "tsx",
        ".js": "javascript",
        ".jsx": "jsx",
        ".json": "json",
        ".jsonc": "jsonc",
        ".yaml": "yaml",
        ".yml": "yaml",
        ".xml": "xml",
        ".md": "markdown",
        ".py": "python",
        ".sh": "bash",
        ".ps1": "powershell",
        ".html": "html",
        ".css": "css",
        ".scss": "scss",
        ".sql": "sql",
        ".rs": "rust",
        ".go": "go",
        ".java": "java",
        ".cpp": "cpp",
        ".c": "c",
        ".h": "c",
        ".hpp": "cpp",
    }

    if path.name == "Dockerfile":
        return "dockerfile"

    return mapping.get(suffix, "text")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Collect repository code into one text file using gitignore-aware git file listing."
    )

    parser.add_argument(
        "-o",
        "--output",
        default="project_code_bundle.md",
        help="Output file path relative to repository root. Default: project_code_bundle.md",
    )

    parser.add_argument(
        "--include-docs",
        action="store_true",
        help="Include all Markdown/text docs, not only important root docs.",
    )

    parser.add_argument(
        "--all-text",
        action="store_true",
        help="Include every git-visible text file, not only code/config files.",
    )

    parser.add_argument(
        "--max-file-bytes",
        type=int,
        default=1_000_000,
        help="Skip files larger than this size. Default: 1000000.",
    )

    args = parser.parse_args()

    repo_root = get_repo_root(Path.cwd())
    output_rel = Path(args.output)
    output_abs = repo_root / output_rel

    files = get_git_files(repo_root)

    included: list[Path] = []
    skipped: list[str] = []

    for rel_path in files:
        abs_path = repo_root / rel_path

        if not abs_path.is_file():
            continue

        if not should_include_file(
            rel_path,
            output_path=output_rel,
            include_docs=args.include_docs,
            all_text=args.all_text,
        ):
            continue

        size = abs_path.stat().st_size
        if size > args.max_file_bytes:
            skipped.append(f"{rel_path.as_posix()} - skipped: too large ({size} bytes)")
            continue

        try:
            read_text_file(abs_path)
        except Exception as ex:
            skipped.append(f"{rel_path.as_posix()} - skipped: {ex}")
            continue

        included.append(rel_path)

    output_abs.parent.mkdir(parents=True, exist_ok=True)

    with output_abs.open("w", encoding="utf-8", newline="\n") as out:
        out.write("# Project Code Bundle\n\n")
        out.write(f"Repository root: `{repo_root}`\n\n")
        out.write(f"Included files: `{len(included)}`\n\n")

        if skipped:
            out.write("## Skipped files\n\n")
            for item in skipped:
                out.write(f"- {item}\n")
            out.write("\n")

        out.write("## Files\n\n")

        for rel_path in included:
            abs_path = repo_root / rel_path
            language = detect_language(rel_path)
            content = read_text_file(abs_path).rstrip()

            out.write("\n---\n\n")
            out.write(f"## `{rel_path.as_posix()}`\n\n")
            out.write(f"```{language}\n")
            out.write(content)
            out.write("\n```\n")

    print(f"Done.")
    print(f"Repository: {repo_root}")
    print(f"Output: {output_abs}")
    print(f"Included files: {len(included)}")
    print(f"Skipped files: {len(skipped)}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
