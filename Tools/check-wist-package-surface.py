#!/usr/bin/env python3
"""Fail-closed validation of the reviewed UniversalToolchain.Wist package surface."""
from __future__ import annotations

import argparse
import hashlib
import json
import struct
import sys
import zipfile
from pathlib import Path, PurePosixPath
from typing import Iterable


class SurfaceError(RuntimeError):
    pass


def _read_manifest(path: Path) -> dict:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise SurfaceError(f"cannot read package surface manifest '{path}': {exc}") from exc
    if data.get("schemaVersion") != 4:
        raise SurfaceError(f"unsupported package surface schemaVersion: {data.get('schemaVersion')!r}")
    for retired_field in ("runtimeManifests", "runtimeManifestSha256"):
        if retired_field in data:
            raise SurfaceError(f"retired runtime-manifest field returned: {retired_field}")
    preset_ids = data.get("shippedPresetIds")
    preset_hashes = data.get("shippedPresetSha256")
    if not isinstance(preset_ids, list) or not isinstance(preset_hashes, dict):
        raise SurfaceError("shipped preset IDs and SHA-256 map are required")
    if set(preset_hashes) != set(preset_ids):
        raise SurfaceError("shippedPresetSha256 keys must exactly match shippedPresetIds")
    for name, digest in preset_hashes.items():
        if not isinstance(digest, str) or len(digest) != 64 or any(char not in "0123456789abcdef" for char in digest):
            raise SurfaceError(f"invalid shipped preset SHA-256 for {name!r}: {digest!r}")
    return data


def _validate_archive_paths(names: Iterable[str]) -> None:
    seen: set[str] = set()
    duplicates: list[str] = []
    for name in names:
        path = PurePosixPath(name)
        if path.is_absolute() or ".." in path.parts or "" in path.parts:
            raise SurfaceError(f"unsafe archive entry path: {name!r}")
        if name in seen:
            duplicates.append(name)
        seen.add(name)
    if duplicates:
        raise SurfaceError(f"duplicate archive entries: {sorted(set(duplicates))}")


def _rva_to_offset(data: bytes, sections_offset: int, section_count: int, rva: int) -> int:
    for index in range(section_count):
        offset = sections_offset + index * 40
        if offset + 40 > len(data):
            break
        virtual_size, virtual_address, raw_size, raw_pointer = struct.unpack_from("<IIII", data, offset + 8)
        span = max(virtual_size, raw_size)
        if virtual_address <= rva < virtual_address + span:
            mapped = raw_pointer + (rva - virtual_address)
            if mapped >= len(data):
                break
            return mapped
    raise SurfaceError("managed metadata RVA is outside all PE sections")


def _validate_managed_pe(payload: bytes, owner: str) -> None:
    try:
        if len(payload) < 0x40 or payload[:2] != b"MZ":
            raise SurfaceError("missing DOS/PE header")
        pe_offset = struct.unpack_from("<I", payload, 0x3C)[0]
        if pe_offset + 24 > len(payload) or payload[pe_offset:pe_offset + 4] != b"PE\0\0":
            raise SurfaceError("missing PE signature")
        coff_offset = pe_offset + 4
        section_count = struct.unpack_from("<H", payload, coff_offset + 2)[0]
        optional_size = struct.unpack_from("<H", payload, coff_offset + 16)[0]
        optional_offset = coff_offset + 20
        if optional_offset + optional_size > len(payload):
            raise SurfaceError("truncated optional header")
        magic = struct.unpack_from("<H", payload, optional_offset)[0]
        if magic == 0x10B:
            directory_count_offset, directories_offset = optional_offset + 92, optional_offset + 96
        elif magic == 0x20B:
            directory_count_offset, directories_offset = optional_offset + 108, optional_offset + 112
        else:
            raise SurfaceError(f"unsupported PE optional-header magic 0x{magic:04x}")
        directory_count = struct.unpack_from("<I", payload, directory_count_offset)[0]
        if directory_count <= 14 or directories_offset + 15 * 8 > optional_offset + optional_size:
            raise SurfaceError("PE does not contain a CLI data directory")
        cli_rva, cli_size = struct.unpack_from("<II", payload, directories_offset + 14 * 8)
        if cli_rva == 0 or cli_size < 0x48:
            raise SurfaceError("invalid CLI header directory")
        sections_offset = optional_offset + optional_size
        cli_offset = _rva_to_offset(payload, sections_offset, section_count, cli_rva)
        if cli_offset + 16 > len(payload):
            raise SurfaceError("truncated CLI header")
        metadata_rva, metadata_size = struct.unpack_from("<II", payload, cli_offset + 8)
        if metadata_rva == 0 or metadata_size < 16:
            raise SurfaceError("invalid CLI metadata directory")
        metadata_offset = _rva_to_offset(payload, sections_offset, section_count, metadata_rva)
        if payload[metadata_offset:metadata_offset + 4] != b"BSJB":
            raise SurfaceError("missing CLR metadata signature")
    except (struct.error, IndexError) as exc:
        raise SurfaceError("truncated managed PE structure") from exc
    except SurfaceError as exc:
        raise SurfaceError(f"{owner}: {exc}") from exc


def _expected_paths(surface: dict) -> tuple[set[str], set[str], set[str]]:
    tfm = surface["targetFramework"]
    compile_paths = {f"ref/{tfm}/{name}" for name in surface["compileAssemblies"]}
    runtime_paths = {f"lib/{tfm}/{name}" for name in surface["runtimeAssemblies"]}
    preset_paths = {
        f"contentFiles/any/{tfm}/Dialects/examples/wist/{preset}/dialect.wistdialect"
        for preset in surface["shippedPresetIds"]
    }
    return compile_paths, runtime_paths, preset_paths


def validate_package(
    package: Path,
    surface_manifest: Path,
    reference_dir: Path | None = None,
    compile_reference: Path | None = None,
) -> None:
    surface = _read_manifest(surface_manifest)
    expected_compile, expected_runtime, expected_presets = _expected_paths(surface)
    forbidden = tuple(surface.get("forbiddenAssemblyNameFragments", []))

    try:
        with zipfile.ZipFile(package) as archive:
            infos = archive.infolist()
            names = [info.filename for info in infos]
            _validate_archive_paths(names)
            all_names = set(names)
            dll_paths = {name for name in all_names if name.lower().endswith(".dll")}
            compile_paths = {name for name in dll_paths if name.startswith("ref/")}
            runtime_paths = {name for name in dll_paths if name.startswith("lib/")}
            other_dlls = dll_paths - compile_paths - runtime_paths
            legacy_runtime_manifests = {name for name in all_names if name.endswith(".dialect.runtime.json")}
            actual_presets = {name for name in all_names if name.endswith("/dialect.wistdialect")}

            checks = [
                ("compile assembly boundary", expected_compile, compile_paths),
                ("runtime assembly closure", expected_runtime, runtime_paths),
                ("shipped preset closure", expected_presets, actual_presets),
            ]
            for label, expected, actual in checks:
                if actual != expected:
                    missing = sorted(expected - actual)
                    unexpected = sorted(actual - expected)
                    raise SurfaceError(f"{label} mismatch; missing={missing}; unexpected={unexpected}")
            if legacy_runtime_manifests:
                raise SurfaceError(
                    f"retired dialect runtime manifests returned: {sorted(legacy_runtime_manifests)}"
                )
            if other_dlls:
                raise SurfaceError(f"DLLs outside supported asset groups: {sorted(other_dlls)}")
            bad_names = sorted(name for name in dll_paths if any(fragment in PurePosixPath(name).name for fragment in forbidden))
            if bad_names:
                raise SurfaceError(f"forbidden assemblies: {bad_names}")

            for path in sorted(expected_compile | expected_runtime):
                info = archive.getinfo(path)
                if info.file_size <= 0:
                    raise SurfaceError(f"{path}: assembly payload is empty")
                payload = archive.read(path)
                _validate_managed_pe(payload, path)
                reference: Path | None = None
                if path in expected_compile and compile_reference is not None:
                    if len(expected_compile) != 1:
                        raise SurfaceError("a single compile reference was supplied for multiple compile assemblies")
                    reference = compile_reference
                elif path in expected_runtime and reference_dir is not None:
                    reference = reference_dir / PurePosixPath(path).name
                if reference is not None:
                    if not reference.is_file():
                        raise SurfaceError(f"{path}: trusted build output is missing: {reference}")
                    expected_payload = reference.read_bytes()
                    if payload != expected_payload:
                        raise SurfaceError(
                            f"{path}: package assembly payload differs from trusted build output "
                            f"(package={hashlib.sha256(payload).hexdigest()}, build={hashlib.sha256(expected_payload).hexdigest()})"
                        )

            preset_hashes = surface["shippedPresetSha256"]
            for path in sorted(expected_presets):
                preset_id = PurePosixPath(path).parent.name
                payload = archive.read(path)
                actual_digest = hashlib.sha256(payload).hexdigest()
                expected_digest = preset_hashes[preset_id]
                if actual_digest != expected_digest:
                    raise SurfaceError(
                        f"{path}: preset SHA-256 mismatch; expected {expected_digest}, actual {actual_digest}"
                    )

    except (OSError, zipfile.BadZipFile, KeyError) as exc:
        raise SurfaceError(f"cannot validate package '{package}': {exc}") from exc


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("packages", nargs="+")
    parser.add_argument(
        "--surface-manifest",
        default=str(Path(__file__).resolve().parents[1] / "eng" / "wist-package-surface.json"),
    )
    parser.add_argument(
        "--reference-dir",
        required=True,
        help="Trusted runtime build-output directory containing the exact lib DLL closure.",
    )
    parser.add_argument(
        "--compile-reference",
        required=True,
        help="Trusted compiler-produced reference assembly packed into ref/<TFM>.",
    )
    args = parser.parse_args(argv)
    surface_manifest = Path(args.surface_manifest).resolve()
    reference_dir = Path(args.reference_dir).resolve()
    compile_reference = Path(args.compile_reference).resolve()
    if not reference_dir.is_dir():
        raise SurfaceError(f"reference directory does not exist: {reference_dir}")
    if not compile_reference.is_file():
        raise SurfaceError(f"compile reference does not exist: {compile_reference}")
    checked = 0
    for value in args.packages:
        package = Path(value).resolve()
        if package.suffix != ".nupkg" or package.name.endswith((".symbols.nupkg", ".snupkg")):
            continue
        validate_package(package, surface_manifest, reference_dir, compile_reference)
        checked += 1
        print(f"{package}: exact managed package surface OK")
    if checked == 0:
        raise SurfaceError("no primary .nupkg files were supplied")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except SurfaceError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
