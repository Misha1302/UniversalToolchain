#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import re
import shutil
import sys
from pathlib import Path

TEXT_SUFFIXES = {
    "", ".cs", ".csproj", ".fs", ".fsproj", ".vb", ".vbproj", ".sln", ".slnx",
    ".props", ".targets", ".json", ".jsonl", ".xml", ".config", ".editorconfig",
    ".ruleset", ".runsettings", ".py", ".sh", ".ps1", ".cmd", ".bat", ".yml",
    ".yaml", ".md", ".tex", ".bib", ".txt", ".csv", ".tsv", ".html", ".css",
    ".js", ".ts", ".wcs", ".wistdialect", ".nuspec", ".toml", ".ini", ".dockerfile",
}

EXCLUDED_DIRS = {
    ".git", ".github", ".idea", ".vs", "bin", "obj", "packages", "artifacts",
    "TestResults", "node_modules", "submission",
}
EXCLUDED_SUFFIXES = {
    ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico", ".zip", ".gz",
    ".xz", ".7z", ".nupkg", ".snupkg", ".dll", ".exe", ".pdb", ".so", ".dylib",
}

LITERAL_REPLACEMENTS = (
    ("Misha1302", "anonymous-author"),
    ("misha1302", "anonymous-author"),
    ("Mikhail Razakov", "Anonymous Author"),
    ("Misha Razakov", "Anonymous Author"),
    ("Razakov", "Anonymous"),
    ("WistSharp", "SystemWSharp"),
    ("wistsharp", "systemwsharp"),
    ("Wist2", "SystemW2"),
    ("wist2", "systemw2"),
)

PUBLIC_WORKING_TITLE = "Obligation-Guided Reverification for Composed Compiler Pipelines"
ANONYMOUS_SUBMISSION_TITLE = "Scheduling Due Semantic Checks in Composed Compiler Pipelines"


def is_text_file(path: Path) -> bool:
    name = path.name.lower()
    if name in {"dockerfile", "makefile", "license", "notice"}:
        return True
    return path.suffix.lower() in TEXT_SUFFIXES


def copy_allowed_tree(source: Path, destination: Path) -> None:
    for current, dirs, files in os.walk(source):
        current_path = Path(current)
        dirs[:] = sorted(d for d in dirs if d not in EXCLUDED_DIRS)
        relative = current_path.relative_to(source)
        target_dir = destination / relative
        target_dir.mkdir(parents=True, exist_ok=True)
        for name in sorted(files):
            src = current_path / name
            if src.suffix.lower() in EXCLUDED_SUFFIXES:
                continue
            if not is_text_file(src):
                continue
            shutil.copy2(src, target_dir / name)


def replace_text(text: str) -> str:
    text = text.replace(PUBLIC_WORKING_TITLE, ANONYMOUS_SUBMISSION_TITLE)
    for old, new in LITERAL_REPLACEMENTS:
        text = text.replace(old, new)
    text = re.sub(r"(?<![A-Za-z0-9_])Wist(?=[A-Z0-9_\-]|\b)", "SystemW", text)
    text = re.sub(r"(?<![A-Za-z0-9_])wist(?=[a-z0-9_\-]|\b)", "systemw", text)
    text = re.sub(
        r"https?://(?:www\.)?github\.com/anonymous-author(?:/[A-Za-z0-9_.\-/]+)?",
        "https://example.invalid/anonymous/system",
        text,
        flags=re.IGNORECASE,
    )
    text = re.sub(
        r"https?://anonymous-author\.github\.io(?:/[A-Za-z0-9_.\-/]+)?",
        "https://example.invalid/anonymous/system",
        text,
        flags=re.IGNORECASE,
    )
    text = re.sub(r"(?<![A-Za-z0-9_])/(?:home|mnt)/[^\s'\"`<>]+", "<workspace>", text)
    return text


def rename_component(name: str) -> str:
    result = name
    for old, new in LITERAL_REPLACEMENTS:
        result = result.replace(old, new)
    result = re.sub(r"(?<![A-Za-z0-9_])Wist(?=[A-Z0-9_\-]|\b)", "SystemW", result)
    result = re.sub(r"(?<![A-Za-z0-9_])wist(?=[a-z0-9_\-]|\b)", "systemw", result)
    return result


def sanitize_text_files(root: Path) -> None:
    for path in sorted(p for p in root.rglob("*") if p.is_file()):
        try:
            original = path.read_text(encoding="utf-8")
        except UnicodeDecodeError as error:
            raise ValueError(f"non-UTF-8 file entered anonymous text snapshot: {path}") from error
        sanitized = replace_text(original)
        path.write_text(sanitized, encoding="utf-8", newline="\n")


def rename_paths(root: Path) -> None:
    for path in sorted(root.rglob("*"), key=lambda item: len(item.parts), reverse=True):
        new_name = rename_component(path.name)
        if new_name == path.name:
            continue
        target = path.with_name(new_name)
        if target.exists():
            raise ValueError(f"anonymization path collision: {path} -> {target}")
        path.rename(target)


def validate_project_references(root: Path) -> None:
    reference_pattern = re.compile(r"<ProjectReference\s+Include=\"([^\"]+)\"")
    missing: list[str] = []
    for project in sorted(root.rglob("*.csproj")):
        text = project.read_text(encoding="utf-8")
        for raw in reference_pattern.findall(text):
            if "$" in raw or "*" in raw:
                continue
            reference = (project.parent / raw.replace("\\", os.sep)).resolve()
            if not reference.exists():
                missing.append(f"{project.relative_to(root)} -> {raw}")
    if missing:
        raise ValueError("missing project references after anonymization:\n" + "\n".join(missing[:100]))


def validate_identity(root: Path) -> None:
    forbidden = re.compile(
        r"Misha1302|misha1302|Mikhail\s+Razakov|Misha\s+Razakov|Razakov|Wist2|WistSharp|"
        r"github\.com/Misha1302|misha1302\.github\.io|/(?:home|mnt)/",
        re.IGNORECASE,
    )
    failures: list[str] = []
    for path in sorted(root.rglob("*")):
        if forbidden.search(path.name):
            failures.append(f"path:{path.relative_to(root)}")
        if path.is_file():
            text = path.read_text(encoding="utf-8")
            match = forbidden.search(text)
            if match:
                failures.append(f"text:{path.relative_to(root)}:{match.group(0)}")
        if len(failures) >= 50:
            break
    if failures:
        raise ValueError("identity material remains after anonymization:\n" + "\n".join(failures))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument(
        "--skip-project-reference-validation",
        action="store_true",
        help=(
            "Skip reference-closure validation for non-buildable evidence snapshots. "
            "Identity and path sanitization remain mandatory."
        ),
    )
    args = parser.parse_args()
    source = args.source.resolve()
    destination = args.destination.resolve()
    if destination.exists():
        shutil.rmtree(destination)
    destination.mkdir(parents=True)
    copy_allowed_tree(source, destination)
    sanitize_text_files(destination)
    rename_paths(destination)
    if not args.skip_project_reference_validation:
        validate_project_references(destination)
    validate_identity(destination)
    print(f"ANONYMIZED_SOURCE={destination}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
