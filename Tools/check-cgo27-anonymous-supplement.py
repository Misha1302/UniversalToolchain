#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

FORBIDDEN = (
    re.compile(r"misha(?:1302)?", re.IGNORECASE),
    re.compile(r"razakov", re.IGNORECASE),
    re.compile(r"\bwist2?\b", re.IGNORECASE),
    re.compile(r"github\.com", re.IGNORECASE),
    re.compile(r"(?:^|[\s\"'])/(?:home|mnt|Users|runner)/"),
    re.compile(r"\b[0-9a-f]{40}\b"),
    re.compile(r"BEGIN (?:RSA|OPENSSH|EC) PRIVATE KEY"),
    re.compile(r"gh[pousr]_[A-Za-z0-9]{20,}"),
)
TEXT_SUFFIXES = {
    ".cs", ".csproj", ".csv", ".json", ".jsonl", ".md", ".py",
    ".sh", ".tex", ".txt", ".yml", ".yaml",
}


def main() -> int:
    root = Path(sys.argv[1]).resolve()
    failures: list[str] = []
    for path in sorted(root.rglob("*")):
        relative = path.relative_to(root)
        if path.is_dir():
            if path.name in {".git", "bin", "obj", "__pycache__", ".idea"}:
                failures.append(f"forbidden directory: {relative}")
            continue
        if relative == Path("analysis/check_anonymity.py"):
            continue
        if path.name == "MANIFEST.sha256" or path.suffix not in TEXT_SUFFIXES:
            continue
        text = path.read_text(encoding="utf-8")
        for pattern in FORBIDDEN:
            match = pattern.search(text)
            if match:
                failures.append(
                    f"{relative}: forbidden token matched {pattern.pattern!r}"
                )
                break
    if failures:
        print("\n".join(failures), file=sys.stderr)
        return 1
    print("CGO27_ANONYMITY_SCAN=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
