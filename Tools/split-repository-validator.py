#!/usr/bin/env python3
from __future__ import annotations
import json, pathlib, sys, xml.etree.ElementTree as ET
root=pathlib.Path(__file__).resolve().parents[1]
m=json.load(open(root/'eng/component.json',encoding='utf-8'))
comp=m['component']; expected=set(m['projects']); ignore={'bin','obj','artifacts','packages','.git','.idea','.vs','TestResults'}; errs=[]
actual={p.relative_to(root).as_posix() for p in root.rglob('*.csproj') if not any(x in p.parts for x in ignore)}
for x in sorted(actual-expected): errs.append('unclassified project: '+x)
for x in sorted(expected-actual): errs.append('missing project: '+x)
def resolve(base,v):
    if not v or '$(' in v or '@(' in v or '%(' in v:return None
    q=pathlib.Path(v.replace('\\','/')); return (q if q.is_absolute() else base/q).resolve(strict=False)
def inside(q):
    try:q.relative_to(root.resolve());return True
    except ValueError:return False
for p in root.rglob('*'):
    if not p.is_file() or p.suffix.lower() not in {'.csproj','.props','.targets'} or any(x in p.parts for x in ignore):continue
    rel=p.relative_to(root).as_posix()
    try:r=ET.parse(p).getroot()
    except Exception as e:errs.append(f'{rel}: XML {e}');continue
    for e in r.iter():
        tag=e.tag.split('}',1)[-1]
        if tag=='PackageReference':
            inc=e.get('Include') or e.get('Update') or ''
            if comp=='PLANFUZZ_RESEARCH' and ('UniversalToolchain.PlanFuzz.Core/' in rel or 'UniversalToolchain.PlanFuzz.Adapter.Acme/' in rel) and 'Wist' in inc:
                errs.append(f'{rel}: Wist package dependency forbidden in Core/Adapter.Acme: {inc}')
        if tag=='ProjectReference':
            v=e.get('Include',''); q=resolve(p.parent,v)
            if comp=='PLANFUZZ_RESEARCH' and ('UniversalToolchain.PlanFuzz.Core/' in rel or 'UniversalToolchain.PlanFuzz.Adapter.Acme/' in rel) and 'Adapter.Wist' in v:
                errs.append(f'{rel}: Adapter.Wist project dependency forbidden in Core/Adapter.Acme: {v}')
            if q is None:continue
            if not inside(q):errs.append(f'{rel}: ProjectReference escapes repository: {v}')
            else:
                rr=q.relative_to(root).as_posix()
                if rr not in expected:errs.append(f'{rel}: ProjectReference to non-owned source: {rr}')
        elif tag in ('Compile','Import','Analyzer'):
            v=e.get('Include') or e.get('Project') or ''; q=resolve(p.parent,v)
            if q is not None and not inside(q):errs.append(f'{rel}: {tag} escapes repository: {v}')
        elif tag=='Reference':
            hint=next((x.text for x in e if x.tag.split('}',1)[-1]=='HintPath' and x.text),None); q=resolve(p.parent,hint) if hint else None
            if q is not None and not inside(q):errs.append(f'{rel}: Reference/HintPath escapes repository: {hint}')
        elif tag=='MSBuild':
            v=e.get('Projects') or ''; q=resolve(p.parent,v)
            if q is not None and not inside(q):errs.append(f'{rel}: MSBuild invocation escapes repository: {v}')
scan_ext={'.cs','.csproj','.props','.targets'}
for p in root.rglob('*'):
    if not p.is_file() or p.suffix.lower() not in scan_ext or any(x in p.parts for x in ignore):continue
    rel=p.relative_to(root).as_posix(); text=p.read_text(errors='replace')
    if comp=='UNIVERSAL':
        for tok in ('UniversalToolchain.Wist','UniversalToolchain.PlanFuzz','PlanFuzz','Wist'):
            if tok in text:errs.append(f'{rel}: forbidden production/build semantic token {tok}')
    elif comp=='WIST_PRODUCT' and 'PlanFuzz' in text:
        errs.append(f'{rel}: PlanFuzz semantic token')
    elif comp=='PLANFUZZ_RESEARCH':
        restricted=('UniversalToolchain.PlanFuzz.Core/' in rel or 'UniversalToolchain.PlanFuzz.Adapter.Acme/' in rel)
        if restricted and 'UniversalToolchain.Wist' in text:
            errs.append(f'{rel}: Wist dependency/semantic token forbidden in Core/Adapter.Acme')
for p in root.rglob('*'):
    if p.is_symlink():
        try:p.resolve(strict=False).relative_to(root.resolve())
        except Exception:errs.append(f'{p.relative_to(root)}: source symlink/path escape')
if errs:
    print('REPOSITORY_ARCHITECTURE=FAIL'); print('\n'.join(errs)); sys.exit(1)
print(f'REPOSITORY_ARCHITECTURE=PASS component={comp} projects={len(expected)}')
