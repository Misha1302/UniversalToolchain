#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
from pathlib import Path


def digest(path: Path) -> str:
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    return value.hexdigest()


def collect(path: Path) -> dict[str, str]:
    return {p.name: digest(p) for p in sorted(path.glob("*.nupkg")) + sorted(path.glob("*.snupkg"))}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("left", type=Path)
    parser.add_argument("right", type=Path)
    args = parser.parse_args()
    left, right = collect(args.left), collect(args.right)
    if left.keys() != right.keys():
        print(f"ERROR: package sets differ; left-only={sorted(left.keys()-right.keys())}; right-only={sorted(right.keys()-left.keys())}")
        return 1
    different = [name for name in left if left[name] != right[name]]
    if different:
        for name in different:
            print(f"ERROR: {name}: {left[name]} != {right[name]}")
        return 1
    for name in sorted(left):
        print(f"{left[name]}  {name}")
    print(f"reproducibility OK: {len(left)} package artifacts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
