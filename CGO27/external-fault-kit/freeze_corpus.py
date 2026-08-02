#!/usr/bin/env python3
from __future__ import annotations
import argparse
import gzip
import hashlib
import io
import json
import tarfile
from pathlib import Path
from corpus_common import collect_cases, validate_accounting


def canonical(value: object) -> bytes:
    return (json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False) + "\n").encode("utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("author_directory", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    source = args.author_directory.resolve()
    output = args.output.resolve()
    faults, controls = collect_cases(source)
    validate_accounting(faults, controls)

    files: dict[str, bytes] = {}
    for case in faults:
        files[f"faults/{case['caseId']}.json"] = canonical({key: value for key, value in case.items() if key != "caseRole"})
    for case in controls:
        files[f"controls/{case['caseId']}.json"] = canonical({key: value for key, value in case.items() if key != "caseRole"})
    author = source / "AUTHOR.md"
    if author.is_file():
        files["AUTHOR.md"] = author.read_bytes()
    manifest = {
        "schemaVersion": 1,
        "status": "FROZEN_BLIND_CORPUS",
        "faults": len(faults),
        "controls": len(controls),
        "families": sorted({case["family"] for case in faults}),
        "caseIds": sorted(case["caseId"] for case in faults + controls),
        "files": {name: hashlib.sha256(data).hexdigest() for name, data in sorted(files.items())},
    }
    files["MANIFEST.json"] = canonical(manifest)

    tar_buffer = io.BytesIO()
    with tarfile.open(fileobj=tar_buffer, mode="w", format=tarfile.PAX_FORMAT) as archive:
        for name, data in sorted(files.items()):
            info = tarfile.TarInfo(name)
            info.size = len(data)
            info.mtime = 0
            info.uid = info.gid = 0
            info.uname = info.gname = ""
            info.mode = 0o644
            archive.addfile(info, io.BytesIO(data))
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("wb") as raw:
        with gzip.GzipFile(filename="", mode="wb", fileobj=raw, mtime=0, compresslevel=9) as compressed:
            compressed.write(tar_buffer.getvalue())
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    output.with_suffix(output.suffix + ".sha256").write_text(f"{digest}  {output.name}\n", encoding="utf-8")
    print(json.dumps({"archive": str(output), "sha256": digest, **manifest}, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
