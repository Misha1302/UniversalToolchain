#!/usr/bin/env python3
"""Seeded negative tests for the Wist package-surface gate."""
from __future__ import annotations

import argparse
import importlib.util
import json
import tempfile
import zipfile
from pathlib import Path


def load_checker(root: Path):
    path = root / "Tools" / "check-wist-package-surface.py"
    spec = importlib.util.spec_from_file_location("wist_package_surface_checker", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load checker from {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def rewrite_package(source: Path, destination: Path, mutation) -> None:
    with zipfile.ZipFile(source) as src, zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED) as dst:
        for info in src.infolist():
            action, payload = mutation(info.filename, src.read(info.filename))
            if action == "drop":
                continue
            dst.writestr(info.filename, payload)
        extras = mutation("", b"")[1]
        for name, payload in extras:
            dst.writestr(name, payload)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("package")
    parser.add_argument("--root", default=str(Path(__file__).resolve().parents[1]))
    parser.add_argument("--reference-dir", required=True)
    parser.add_argument("--compile-reference", required=True)
    args = parser.parse_args()
    root = Path(args.root).resolve()
    package = Path(args.package).resolve()
    checker = load_checker(root)
    surface = root / "eng" / "wist-package-surface.json"
    reference_dir = Path(args.reference_dir).resolve()
    compile_reference = Path(args.compile_reference).resolve()
    checker.validate_package(package, surface, reference_dir, compile_reference)

    manifest = json.loads(surface.read_text())
    tfm = manifest["targetFramework"]
    first_runtime = f"lib/{tfm}/{manifest['runtimeAssemblies'][0]}"
    arithmetic = f"lib/{tfm}/ArithmeticModule.dll"
    first_runtime_manifest = f"contentFiles/any/{tfm}/{manifest['runtimeManifests'][0]}"
    first_preset = f"contentFiles/any/{tfm}/Dialects/examples/wist/{manifest['shippedPresetIds'][0]}/dialect.wistdialect"
    replacement_preset = f"contentFiles/any/{tfm}/Dialects/examples/wist/{manifest['shippedPresetIds'][1]}/dialect.wistdialect"
    with zipfile.ZipFile(package) as canonical_archive:
        replacement_preset_payload = canonical_archive.read(replacement_preset)
        replacement_runtime_payload = canonical_archive.read(f"lib/{tfm}/CommentsModule.dll")

    def zero(name: str, payload: bytes):
        return ("keep", b"" if name == first_runtime else payload) if name else ("extras", [])

    def missing(name: str, payload: bytes):
        return ("drop", payload) if name == arithmetic else (("keep", payload) if name else ("extras", []))

    def unexpected(name: str, payload: bytes):
        if name:
            return "keep", payload
        return "extras", [(f"lib/{tfm}/Unexpected.Runtime.dll", b"MZ")]

    def alias_drift(name: str, payload: bytes):
        if name == first_runtime_manifest:
            document = json.loads(payload)
            document["components"][0]["aliases"] = ["seeded-mutant-alias"]
            return "keep", json.dumps(document).encode()
        return ("keep", payload) if name else ("extras", [])


    def identity_swap(name: str, payload: bytes):
        return ("keep", replacement_runtime_payload if name == arithmetic else payload) if name else ("extras", [])

    def preset_swap(name: str, payload: bytes):
        return ("keep", replacement_preset_payload if name == first_preset else payload) if name else ("extras", [])

    mutations = {
        "zero-byte-managed-assembly": zero,
        "missing-required-runtime-assembly": missing,
        "unexpected-runtime-assembly": unexpected,
        "manifest-alias-drift": alias_drift,
        "managed-assembly-identity-swap": identity_swap,
        "preset-semantic-swap": preset_swap,
    }

    with tempfile.TemporaryDirectory(prefix="wist-package-mutants-") as tmp:
        for name, mutation in mutations.items():
            mutant = Path(tmp) / f"{name}.nupkg"
            rewrite_package(package, mutant, mutation)
            try:
                checker.validate_package(mutant, surface, reference_dir, compile_reference)
            except checker.SurfaceError:
                print(f"SURVIVOR=0 mutant={name}")
            else:
                raise RuntimeError(f"seeded mutant survived package gate: {name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
