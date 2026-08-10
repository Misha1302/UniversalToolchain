#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

EXCLUDED = {".git", "artifacts", "bin", "obj", "packages", "node_modules", "Experiments"}


def production_sources(root: Path):
    base = root / "UniversalToolchain"
    for path in base.rglob("*.cs"):
        relative = path.relative_to(root)
        if any(part in EXCLUDED for part in relative.parts):
            continue
        normalized = relative.as_posix()
        if ".Tests/" in normalized or "/Tests/" in normalized or ".Testing.Infrastructure/" in normalized:
            continue
        yield path


CURRENT_TEXT_SUFFIXES_BY_ROOT = {
    ".github/workflows": frozenset({".yml", ".yaml"}),
    "UniversalToolchain": frozenset({".csproj", ".props", ".targets", ".sln", ".slnx", ".md"}),
    "samples": frozenset({".csproj", ".props", ".targets", ".md"}),
    "docs": frozenset({".md", ".mts", ".ts"}),
}
CURRENT_DOC_EXCLUDED = frozenset({"archive", "reviews", "proposals", "talks", "maintainers", "vision"})


def current_text_surfaces(root: Path):
    for relative in ("build.sh", "build.ps1"):
        path = root / relative
        if path.is_file():
            yield path
    for relative_root, suffixes in CURRENT_TEXT_SUFFIXES_BY_ROOT.items():
        base = root / relative_root
        if not base.is_dir():
            continue
        for path in sorted(base.rglob("*")):
            if not path.is_file() or path.suffix.lower() not in suffixes:
                continue
            relative = path.relative_to(root)
            if any(part in EXCLUDED for part in relative.parts):
                continue
            if relative.parts and relative.parts[0] == "docs" and any(
                part in CURRENT_DOC_EXCLUDED for part in relative.parts[1:]
            ):
                continue
            yield path


def validate(root: Path, registry_path: Path) -> list[str]:
    data = json.loads(registry_path.read_text(encoding="utf-8"))
    if data.get("schemaVersion") != 1:
        raise ValueError("unsupported retired-surface registry schema")
    violations: list[str] = []
    for relative in data.get("paths", []):
        path = root / relative
        if path.exists():
            violations.append(f"retired path returned: {relative}")
    patterns = [(entry["name"], re.compile(entry["pattern"])) for entry in data.get("symbols", [])]
    for path in production_sources(root):
        source = path.read_text(encoding="utf-8")
        for name, pattern in patterns:
            if pattern.search(source):
                violations.append(f"{name} returned in {path.relative_to(root).as_posix()}")
    text_entries = data.get("currentTextPatterns", [])
    if not isinstance(text_entries, list):
        raise ValueError("currentTextPatterns must be a list")
    text_patterns = [
        (entry["name"], re.compile(entry["pattern"]))
        for entry in text_entries
    ]
    for path in current_text_surfaces(root):
        source = path.read_text(encoding="utf-8-sig")
        for name, pattern in text_patterns:
            if pattern.search(source):
                violations.append(
                    f"{name} returned in current text surface {path.relative_to(root).as_posix()}"
                )
    return violations


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--registry", type=Path, default=Path("eng/retired-surface.json"))
    args = parser.parse_args()
    root = args.root.resolve()
    registry = args.registry if args.registry.is_absolute() else root / args.registry
    try:
        violations = validate(root, registry)
    except (OSError, ValueError, json.JSONDecodeError, re.error) as exc:
        print(f"ERROR: {exc}")
        return 1
    if violations:
        for violation in violations:
            print(f"ERROR: {violation}")
        return 1
    print("retired surface OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
