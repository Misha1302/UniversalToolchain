#!/usr/bin/env python3
"""Validate the supported facade boundary and bounded runtime closure of Wist packages."""
from __future__ import annotations

import sys
import zipfile
from pathlib import Path

MAX_RUNTIME_DLLS = 64
EXPECTED_COMPILE_DLLS = {"ref/net10.0/UniversalToolchain.Wist.dll"}
REQUIRED_RUNTIME_DLLS = {
    "lib/net10.0/UniversalToolchain.Wist.dll",
    "lib/net10.0/BasicStdLib.dll",
    "lib/net10.0/UniversalToolchain.Dialects.Wist.dll",
    "lib/net10.0/UniversalToolchain.Ssa.Optimization.dll",
}
FORBIDDEN_FRAGMENTS = ("Tests", "Benchmarks", "ManifestEmitter")


def main() -> int:
    packages = [Path(value) for value in sys.argv[1:]]
    if not packages:
        raise SystemExit("usage: check-wist-package-surface.py <package.nupkg> [...]")

    checked = 0
    for package in packages:
        if package.suffix != ".nupkg" or package.name.endswith((".symbols.nupkg", ".snupkg")):
            continue
        checked += 1
        with zipfile.ZipFile(package) as archive:
            dlls = sorted(name for name in archive.namelist() if name.endswith(".dll"))

        compile_dlls = {name for name in dlls if name.startswith("ref/")}
        runtime_dlls = {name for name in dlls if name.startswith("lib/")}
        other_dlls = set(dlls).difference(compile_dlls, runtime_dlls)

        if compile_dlls != EXPECTED_COMPILE_DLLS:
            raise SystemExit(
                f"{package}: compile asset boundary mismatch: "
                f"expected {sorted(EXPECTED_COMPILE_DLLS)}, got {sorted(compile_dlls)}"
            )
        if other_dlls:
            raise SystemExit(f"{package}: DLLs outside supported asset groups: {sorted(other_dlls)}")

        missing = sorted(REQUIRED_RUNTIME_DLLS.difference(runtime_dlls))
        if missing:
            raise SystemExit(f"{package}: required runtime assemblies are missing: {', '.join(missing)}")

        forbidden = [name for name in dlls if any(fragment in name for fragment in FORBIDDEN_FRAGMENTS)]
        if forbidden:
            raise SystemExit(f"{package}: forbidden assemblies: {', '.join(forbidden)}")
        if len(runtime_dlls) > MAX_RUNTIME_DLLS:
            raise SystemExit(
                f"{package}: runtime DLL count grew beyond the reviewed preview.4 ceiling "
                f"({len(runtime_dlls)} > {MAX_RUNTIME_DLLS}). Review the package boundary explicitly."
            )

        print(
            f"{package}: package surface OK "
            f"({len(compile_dlls)} compile DLL, {len(runtime_dlls)} runtime DLLs; "
            f"runtime ceiling {MAX_RUNTIME_DLLS})"
        )

    if checked == 0:
        raise SystemExit("no primary .nupkg files were supplied")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
