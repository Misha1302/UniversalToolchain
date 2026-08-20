#!/usr/bin/env python3
import pathlib,sys
root=pathlib.Path(__file__).resolve().parents[1]; errs=[]
for rel in ['README.md','docs/architecture.md','docs/CONTRIBUTING.md','docs/package-boundary.md']:
    p=root/rel
    if not p.exists() or p.stat().st_size<40:errs.append('missing/empty '+rel)
for p in list((root/'docs').rglob('*.md'))+[root/'README.md']:
    if not p.exists():continue
    text=p.read_text(errors='replace')
    for bad in ('UniversalOnly.sln is canonical','Wist.sln is the canonical whole-repo','Wist.sln as the whole-repository'):
        if bad in text:errs.append(f'{p.relative_to(root)}: stale topology statement {bad!r}')
if errs:print('REPOSITORY_DOCS=FAIL');print('\n'.join(errs));sys.exit(1)
print('REPOSITORY_DOCS=PASS')
