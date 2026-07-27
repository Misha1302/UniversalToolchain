#!/usr/bin/env python3
from __future__ import annotations
import argparse, hashlib, json
from pathlib import Path

SCHEMA = "universaltoolchain.release-integrity.v1"

def sha256(path: Path) -> str:
    digest=hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(1024*1024), b''):
            digest.update(chunk)
    return digest.hexdigest()

def canonical_bytes(document: dict) -> bytes:
    return (json.dumps(document, ensure_ascii=False, sort_keys=True, separators=(',', ':'))+'\n').encode('utf-8')

def resolve_files(base: Path, values: list[str]) -> list[Path]:
    files=[]
    for value in values:
        path=(base/value).resolve() if not Path(value).is_absolute() else Path(value).resolve()
        if not path.is_file():
            raise SystemExit(f"release artifact does not exist: {path}")
        try: path.relative_to(base.resolve())
        except ValueError: raise SystemExit(f"release artifact escapes base directory: {path}")
        files.append(path)
    if len(set(files)) != len(files):
        raise SystemExit('duplicate release artifact')
    return sorted(files, key=lambda p: p.relative_to(base.resolve()).as_posix())

def write(ns: argparse.Namespace) -> int:
    base=Path(ns.base).resolve()
    manifest=Path(ns.manifest).resolve()
    root_output=Path(ns.root_output).resolve()
    files=resolve_files(base, ns.files)
    document={
        'schema':SCHEMA,
        'artifacts':[
            {'path':p.relative_to(base).as_posix(),'size':p.stat().st_size,'sha256':sha256(p)}
            for p in files
        ]
    }
    data=canonical_bytes(document)
    root=hashlib.sha256(data).hexdigest()
    manifest.parent.mkdir(parents=True, exist_ok=True)
    root_output.parent.mkdir(parents=True, exist_ok=True)
    manifest.write_bytes(data)
    root_output.write_text(root+'\n', encoding='ascii')
    print(f"release integrity manifest: {manifest}")
    print(f"detached provenance root: {root}")
    return 0

def verify(ns: argparse.Namespace) -> int:
    base=Path(ns.base).resolve()
    manifest=Path(ns.manifest).resolve()
    data=manifest.read_bytes()
    actual_root=hashlib.sha256(data).hexdigest()
    expected=(ns.expected_root or Path(ns.expected_root_file).read_text(encoding='ascii')).strip().lower()
    if len(expected)!=64 or any(c not in '0123456789abcdef' for c in expected):
        raise SystemExit('expected root must be one lowercase/uppercase SHA-256 value')
    if actual_root != expected:
        raise SystemExit(f"release integrity root mismatch: expected {expected}, actual {actual_root}")
    try: document=json.loads(data)
    except json.JSONDecodeError as ex: raise SystemExit(f"invalid integrity manifest JSON: {ex}")
    if canonical_bytes(document) != data:
        raise SystemExit('integrity manifest is not canonical JSON')
    if document.get('schema') != SCHEMA:
        raise SystemExit(f"unsupported integrity schema: {document.get('schema')!r}")
    artifacts=document.get('artifacts')
    if not isinstance(artifacts,list) or not artifacts:
        raise SystemExit('integrity manifest has no artifacts')
    seen=set()
    for entry in artifacts:
        if set(entry) != {'path','size','sha256'}:
            raise SystemExit(f"invalid artifact entry: {entry!r}")
        relative=entry['path']
        if relative in seen: raise SystemExit(f"duplicate artifact path: {relative}")
        seen.add(relative)
        path=(base/relative).resolve()
        try: path.relative_to(base)
        except ValueError: raise SystemExit(f"artifact path escapes base: {relative}")
        if not path.is_file(): raise SystemExit(f"artifact missing: {relative}")
        if path.stat().st_size != entry['size']:
            raise SystemExit(f"artifact size mismatch: {relative}")
        actual=sha256(path)
        if actual != entry['sha256']:
            raise SystemExit(f"artifact SHA-256 mismatch: {relative}")
    print(f"release integrity OK: {len(artifacts)} artifacts, root {expected}")
    return 0

def main() -> int:
    ap=argparse.ArgumentParser()
    sub=ap.add_subparsers(dest='command', required=True)
    w=sub.add_parser('write')
    w.add_argument('--base', required=True)
    w.add_argument('--manifest', required=True)
    w.add_argument('--root-output', required=True)
    w.add_argument('files', nargs='+')
    v=sub.add_parser('verify')
    v.add_argument('--base', required=True)
    v.add_argument('--manifest', required=True)
    group=v.add_mutually_exclusive_group(required=True)
    group.add_argument('--expected-root')
    group.add_argument('--expected-root-file')
    ns=ap.parse_args()
    return write(ns) if ns.command=='write' else verify(ns)

if __name__=='__main__':
    raise SystemExit(main())
