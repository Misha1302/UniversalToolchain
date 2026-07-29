#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import tempfile
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

FIXED_TIME = (1980, 1, 1, 0, 0, 0)
CORE_PREFIX = "package/services/metadata/core-properties/"
CORE_NAME = CORE_PREFIX + "core-properties.psmdcp"
REL_NS = "http://schemas.openxmlformats.org/package/2006/relationships"


def canonicalize_relationships(data: bytes) -> bytes:
    root = ET.fromstring(data)
    relationships = list(root)
    for item in relationships:
        kind = item.attrib.get("Type", "")
        if kind.endswith("/metadata/core-properties"):
            item.set("Target", "/" + CORE_NAME)
            item.set("Id", "R0000000000000002")
        elif kind.endswith("/manifest"):
            item.set("Id", "R0000000000000001")
    relationships.sort(key=lambda item: (item.attrib.get("Type", ""), item.attrib.get("Target", "")))
    root[:] = relationships
    ET.register_namespace("", REL_NS)
    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


def canonical_entries(path: Path) -> list[tuple[str, bytes, int]]:
    entries: dict[str, tuple[bytes, int]] = {}
    with zipfile.ZipFile(path, "r") as source:
        core_data: bytes | None = None
        for item in source.infolist():
            if item.is_dir():
                continue
            name = item.filename.replace("\\", "/")
            data = source.read(item)
            if name.startswith(CORE_PREFIX) and name.endswith(".psmdcp"):
                if core_data is not None:
                    raise ValueError(f"{path}: multiple core-properties entries")
                core_data = data
                continue
            if name == "_rels/.rels":
                data = canonicalize_relationships(data)
            entries[name] = (data, item.compress_type)
        if core_data is not None:
            entries[CORE_NAME] = (core_data, zipfile.ZIP_DEFLATED)
    return [(name, *entries[name]) for name in sorted(entries)]


def repack(path: Path) -> None:
    entries = canonical_entries(path)
    fd, temporary_name = tempfile.mkstemp(prefix=path.name + ".", suffix=".tmp", dir=path.parent)
    os.close(fd)
    temporary = Path(temporary_name)
    try:
        with zipfile.ZipFile(temporary, "w", allowZip64=True) as target:
            for name, data, compression in entries:
                info = zipfile.ZipInfo(name, FIXED_TIME)
                info.create_system = 3
                info.external_attr = 0o100644 << 16
                info.flag_bits = 0x800
                info.compress_type = zipfile.ZIP_DEFLATED if compression != zipfile.ZIP_STORED else zipfile.ZIP_STORED
                target.writestr(info, data, compress_type=info.compress_type, compresslevel=9)
        temporary.replace(path)
    finally:
        temporary.unlink(missing_ok=True)


def main() -> int:
    parser = argparse.ArgumentParser(description="Rewrite NuGet ZIP containers into a deterministic representation.")
    parser.add_argument("packages", nargs="+", type=Path)
    args = parser.parse_args()
    for package in args.packages:
        if package.suffix not in {".nupkg", ".snupkg"}:
            raise SystemExit(f"unsupported package extension: {package}")
        repack(package.resolve())
        print(f"deterministic package: {package}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
