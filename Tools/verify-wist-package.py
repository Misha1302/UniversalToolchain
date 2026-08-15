#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import sys
import zipfile
from pathlib import Path

TABLE_DLL = re.compile(r"^\| `([^`]+\.dll)` \|", re.MULTILINE)


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify the Wist package runtime closure against reviewed evidence.")
    parser.add_argument("package", type=Path)
    parser.add_argument("--evidence", type=Path, default=Path("docs/evidence/wist-package-closure.md"))
    parser.add_argument("--write-closure", type=Path)
    args = parser.parse_args()

    expected = sorted(set(TABLE_DLL.findall(args.evidence.read_text(encoding="utf-8"))))
    if not expected:
        print("WIST_PACKAGE_CLOSURE=FAIL: evidence contains no runtime DLL table", file=sys.stderr)
        return 1

    with zipfile.ZipFile(args.package) as archive:
        actual = sorted({Path(name).name for name in archive.namelist() if name.startswith("lib/net10.0/") and name.endswith(".dll")})

    if args.write_closure:
        args.write_closure.parent.mkdir(parents=True, exist_ok=True)
        args.write_closure.write_text("\n".join(actual) + "\n", encoding="utf-8")

    missing = sorted(set(expected) - set(actual))
    extra = sorted(set(actual) - set(expected))
    if missing or extra:
        print(f"WIST_PACKAGE_CLOSURE=FAIL expected={len(expected)} actual={len(actual)}", file=sys.stderr)
        if missing:
            print("missing=" + ",".join(missing), file=sys.stderr)
        if extra:
            print("extra=" + ",".join(extra), file=sys.stderr)
        return 1

    print(f"WIST_PACKAGE_CLOSURE=PASS dlls={len(actual)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
