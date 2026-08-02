#!/usr/bin/env python3
from __future__ import annotations
import argparse
import hashlib
import json
import os
import tarfile
import tempfile
from pathlib import Path
from corpus_common import collect_cases, validate_accounting


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("archive", type=Path)
    parser.add_argument("receipt", type=Path)
    args = parser.parse_args()
    archive_path = args.archive.resolve()
    digest = hashlib.sha256(archive_path.read_bytes()).hexdigest()
    with tempfile.TemporaryDirectory(prefix="cgo27-blind-") as temp:
        root = Path(temp).resolve()
        with tarfile.open(archive_path, "r:gz") as archive:
            for member in archive.getmembers():
                target = (root / member.name).resolve()
                if os.path.commonpath([root, target]) != str(root):
                    raise ValueError(f"unsafe archive path: {member.name}")
                if member.issym() or member.islnk() or member.isdev():
                    raise ValueError(f"unsupported archive member: {member.name}")
            archive.extractall(root, filter="data")
        manifest_path = root / "MANIFEST.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        faults, controls = collect_cases(root)
        validate_accounting(faults, controls)
        expected_ids = sorted(case["caseId"] for case in faults + controls)
        if manifest.get("caseIds") != expected_ids:
            raise ValueError("manifest caseIds do not match imported cases")
        for name, expected in manifest.get("files", {}).items():
            path = (root / name).resolve()
            if os.path.commonpath([root, path]) != str(root) or not path.is_file():
                raise ValueError(f"manifest file missing: {name}")
            actual = hashlib.sha256(path.read_bytes()).hexdigest()
            if actual != expected:
                raise ValueError(f"checksum mismatch: {name}")
        receipt = {
            "schemaVersion": 1,
            "status": "BLIND_CORPUS_IMPORTED_NOT_EXECUTED",
            "archiveSha256": digest,
            "faults": len(faults),
            "controls": len(controls),
            "families": sorted({case["family"] for case in faults}),
            "caseIds": expected_ids,
            "policyResultsInspected": False,
        }
        args.receipt.parent.mkdir(parents=True, exist_ok=True)
        args.receipt.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(json.dumps(receipt, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
