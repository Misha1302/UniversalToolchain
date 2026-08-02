#!/usr/bin/env python3
from __future__ import annotations

import os
import re
import sys
from pathlib import Path

root = Path(sys.argv[1]).resolve()
reference_pattern = re.compile(r"<ProjectReference\s+Include=\"([^\"]+)\"")
absolute_path_pattern = re.compile(r"/(?:home|mnt)/")
failures: list[str] = []
for path in sorted(root.rglob("*")):
    if path.name in {".git", ".github", "bin", "obj", ".idea"}:
        failures.append(f"forbidden directory: {path.relative_to(root)}")
    if not path.is_file():
        continue
    try:
        text = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        failures.append(f"non-UTF-8 file: {path.relative_to(root)}")
        continue
    if absolute_path_pattern.search(text):
        failures.append(f"local absolute path: {path.relative_to(root)}")
    if path.suffix == ".csproj":
        for raw in reference_pattern.findall(text):
            if "$" in raw or "*" in raw:
                continue
            target = (path.parent / raw.replace("\\", os.sep)).resolve()
            if not target.exists():
                failures.append(f"missing ProjectReference: {path.relative_to(root)} -> {raw}")
if failures:
    print("\n".join(failures[:100]), file=sys.stderr)
    raise SystemExit(1)
print("ANONYMOUS_SOURCE_STATIC_VALIDATION=PASS")
