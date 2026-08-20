#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,sys,xml.etree.ElementTree as ET
from pathlib import Path

def norm(p): return str(p).replace('\\','/').lstrip('./')
def local(t): return t.split('}')[-1]

def main():
 ap=argparse.ArgumentParser(); ap.add_argument('--root',default='.'); a=ap.parse_args(); root=Path(a.root).resolve()
 ownm=json.load(open(root/'eng/project-ownership.json')); own={p:o for o,ps in ownm['owners'].items() for p in ps}
 detected=[]
 for p in root.glob('**/*.csproj'):
  if 'bin' in p.parts or 'obj' in p.parts: continue
  rel=norm(p.relative_to(root)); tree=ET.parse(p)
  istest=False
  for e in tree.getroot().iter():
   if local(e.tag)=='IsTestProject' and (e.text or '').strip().lower()=='true': istest=True
   if local(e.tag)=='PackageReference' and (e.attrib.get('Include') or '').lower()=='microsoft.net.test.sdk': istest=True
  if istest: detected.append(rel)
 manifests={
  'UNIVERSAL':root/'eng/tests/universal.json',
  'WIST_PRODUCT':root/'eng/tests/wist.json',
  'PLANFUZZ_RESEARCH':root/'eng/tests/planfuzz.json'}
 declared=[]; errors=[]
 for owner,path in manifests.items():
  if not path.exists(): errors.append(f'missing test manifest: {path.relative_to(root)}'); continue
  d=json.load(open(path)); ps=d.get('projects',[])
  if len(ps)!=len(set(ps)): errors.append(f'duplicate test registration inside {path.relative_to(root)}')
  for p in ps:
   declared.append((p,owner,path))
   if not (root/p).exists(): errors.append(f'declared test project missing: {p}')
   if own.get(p)!=owner: errors.append(f'test owner mismatch: {p} manifest={owner} project_owner={own.get(p)}')
 counts={p:0 for p in detected}
 for p,_,_ in declared: counts[p]=counts.get(p,0)+1
 for p in detected:
  if counts.get(p,0)!=1: errors.append(f'test project must be registered exactly once: {p} registrations={counts.get(p,0)}')
 for p,c in counts.items():
  if p not in detected: errors.append(f'manifest entry is not a detected test project: {p}')
  if c>1: errors.append(f'duplicate test registration across manifests: {p} registrations={c}')
 if errors:
  print('\n'.join(errors),file=sys.stderr); print(f'THREE_REPO_TEST_INVENTORY=FAIL errors={len(errors)}',file=sys.stderr); return 1
 print(f'THREE_REPO_TEST_INVENTORY=PASS tests={len(detected)} universal={sum(1 for p in detected if own[p]=="UNIVERSAL")} wist={sum(1 for p in detected if own[p]=="WIST_PRODUCT")} planfuzz={sum(1 for p in detected if own[p]=="PLANFUZZ_RESEARCH")}')
 return 0
if __name__=='__main__': raise SystemExit(main())
