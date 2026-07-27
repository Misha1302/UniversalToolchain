#!/usr/bin/env python3
from __future__ import annotations
import argparse, hashlib, json, shutil, subprocess, sys, tempfile
from pathlib import Path


def run(args: list[str], ok: bool) -> subprocess.CompletedProcess[str]:
    result=subprocess.run(args, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if (result.returncode==0) != ok:
        raise SystemExit(f"unexpected command status {result.returncode}: {' '.join(args)}\n{result.stdout}")
    return result


def main() -> int:
    ap=argparse.ArgumentParser()
    ap.add_argument('--root', required=True)
    ap.add_argument('artifact')
    ns=ap.parse_args()
    repo=Path(ns.root).resolve()
    artifact=Path(ns.artifact).resolve()
    tool=repo/'Tools/release-integrity.py'
    with tempfile.TemporaryDirectory(prefix='wist-integrity-mutant-') as raw:
        base=Path(raw)
        copy=base/artifact.name
        shutil.copy2(artifact,copy)
        manifest=base/'RELEASE-INTEGRITY.json'
        root_file=base/'EXPECTED.root.sha256'
        run([sys.executable,str(tool),'write','--base',str(base),'--manifest',str(manifest),'--root-output',str(root_file),copy.name],True)
        expected=root_file.read_text().strip()
        run([sys.executable,str(tool),'verify','--base',str(base),'--manifest',str(manifest),'--expected-root',expected],True)

        with copy.open('ab') as f: f.write(b'hostile-repack')
        # A payload-only tamper must fail against the detached root.
        run([sys.executable,str(tool),'verify','--base',str(base),'--manifest',str(manifest),'--expected-root',expected],False)

        # Regenerating the mutable manifest does not help when the verifier pins the old detached root.
        document={
            'schema':'universaltoolchain.release-integrity.v1',
            'artifacts':[{'path':copy.name,'size':copy.stat().st_size,'sha256':hashlib.sha256(copy.read_bytes()).hexdigest()}]
        }
        manifest.write_text(json.dumps(document,sort_keys=True,separators=(',',':'))+'\n')
        run([sys.executable,str(tool),'verify','--base',str(base),'--manifest',str(manifest),'--expected-root',expected],False)
    print('release-integrity mutants rejected')
    return 0

if __name__=='__main__':
    raise SystemExit(main())
