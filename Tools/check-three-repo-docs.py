#!/usr/bin/env python3
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]
errs=[]
required={
 'docs/architecture/three-repository-split.md':['UniversalToolchain/UniversalToolchain.sln','UniversalToolchain/Wist.sln','UniversalToolchain/PlanFuzz.sln','--component universal','--component wist','--component planfuzz'],
 'docs/CONTRIBUTING.md':['--component universal','--component wist','--component planfuzz','UniversalToolchain/UniversalToolchain.sln','eng/project-ownership.json','eng/repository-partitions.json'],
 'docs/architecture/project-map.md':['UniversalToolchain/PlanFuzz.sln','--component planfuzz','eng/tests/planfuzz.json'],
 'internal-docs/proposals/planfuzz/implementation-status.md':['--component planfuzz','eng/tests/planfuzz.json'],
}
for rel,toks in required.items():
 p=root/rel
 if not p.exists(): errs.append(f'missing {rel}'); continue
 s=p.read_text(errors='replace')
 for t in toks:
  if t not in s: errs.append(f'{rel}: missing {t!r}')
# Scan current contributor/architecture docs. Historical release/review receipts and pinned snapshots may
# describe old topology, but must be explicitly marked historical/pinned rather than presented as live guidance.
scan=[]
for base in [root/'docs',root/'internal-docs/proposals/planfuzz']:
 if not base.exists(): continue
 for p in base.rglob('*.md'):
  rel=p.relative_to(root).as_posix()
  if rel.startswith('docs/releases/') or rel.startswith('docs/evidence/reviews/'):
   continue
  scan.append((rel,p.read_text(errors='replace')))
stale=[
 ('UniversalOnly.sln','temporary generated UniversalOnly.sln'),
 ('Wist.sln is the canonical whole-repo','Wist.sln whole-repo canonical claim'),
 ('build it alongside `Wist.sln` and use the shared test manifest','two-solution/shared-manifest PlanFuzz claim'),
 ('canonical entrypoints build\n`Wist.sln` and `PlanFuzz.sln`','two-solution canonical build claim'),
]
for rel,s in scan:
 for token,label in stale:
  if token in s: errs.append(f'{rel}: stale topology: {label}')
# A pinned snapshot can mention old solution names only when its historical status is explicit.
p=root/'docs/evidence/current-verification.md'
if p.exists():
 s=p.read_text(errors='replace')
 if ('UniversalToolchain/Wist.sln' in s or 'shared test manifest' in s) and 'This is historical evidence, not the current canonical build topology.' not in s:
  errs.append('docs/evidence/current-verification.md: old topology is not explicitly marked historical')
# Root README should expose the current component build surface.
r=(root/'readme.md').read_text(errors='replace') if (root/'readme.md').exists() else ''
for bad in ['Wist.sln is the canonical whole-repo','UniversalOnly.sln is canonical']:
 if bad in r: errs.append(f'readme.md: stale canonical statement: {bad}')
if errs:
 print('THREE_REPO_DOCS=FAIL'); print('\n'.join(errs)); sys.exit(1)
print('THREE_REPO_DOCS=PASS')
