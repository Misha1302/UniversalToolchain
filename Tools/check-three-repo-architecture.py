#!/usr/bin/env python3
from __future__ import annotations
import argparse,json,os,pathlib,re,sys,xml.etree.ElementTree as ET
OWNERS=('UNIVERSAL','WIST_PRODUCT','PLANFUZZ_RESEARCH')
XML_EXT={'.csproj','.props','.targets'}
IGN={'bin','obj','artifacts','packages','.git','.idea','.vs','TestResults'}
def norm(p): return p.as_posix() if hasattr(p,'as_posix') else str(p).replace('\\','/')
def local(tag): return tag.split('}',1)[-1]
def readj(p): return json.load(open(p,encoding='utf-8'))
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('--root',default='.'); ap.add_argument('--quiet',action='store_true'); a=ap.parse_args(); root=pathlib.Path(a.root).resolve()
 man=readj(root/'eng/project-ownership.json'); part=readj(root/'eng/repository-partitions.json'); errors=[]
 owners=man['owners']; actual={norm(p.relative_to(root)) for p in root.rglob('*.csproj') if not any(x in p.parts for x in IGN)}
 proj_owner={}
 for o in OWNERS:
  for p in owners.get(o,[]):
   if p in proj_owner: errors.append(f'{p}: project ownership count must be 1 (overlap {proj_owner[p]}, {o})')
   proj_owner[p]=o
 for p in sorted(actual):
  c=sum(p in owners.get(o,[]) for o in OWNERS)
  if c!=1: errors.append(f'{p}: project ownership count must be 1, got {c}')
 for p in sorted(set(proj_owner)-actual): errors.append(f'{p}: manifest project does not exist')
 # Project ownership is inherited lexically from the nearest registered project directory.
 # Walking path parents is both exact for nested projects and far cheaper than resolving against
 # every project directory for every source file.
 project_dir_owner={norm(pathlib.Path(p).parent):o for p,o in proj_owner.items()}
 file_owner={rel:o for o in OWNERS for rel in part.get('files',{}).get(o,[])}
 shared_lookup=set(part.get('shared_metadata',[]))
 def owner_for_rel(rel:str):
  q=pathlib.PurePosixPath(norm(rel))
  cur=q.parent if q.suffix else q
  while True:
   key=cur.as_posix()
   if key in project_dir_owner:return project_dir_owner[key]
   if cur==pathlib.PurePosixPath('.') or cur.parent==cur:break
   cur=cur.parent
  if norm(rel) in file_owner:return file_owner[norm(rel)]
  if norm(rel) in shared_lookup:return 'SHARED'
  return None
 def owner_for_path(path:pathlib.Path):
  try: rel=norm(path.relative_to(root)) if path.is_absolute() else norm(path)
  except ValueError:return None
  return owner_for_rel(rel)
 # explicit non-project file manifest total unique
 shared=set(part.get('shared_metadata',[])); file_lists={o:set(part.get('files',{}).get(o,[])) for o in OWNERS}
 overlap={x for o in OWNERS for x in file_lists[o] if sum(x in file_lists[q] for q in OWNERS)>1}
 for x in sorted(overlap): errors.append(f'{x}: file ownership count must be 1 (overlap)')
 for x in sorted(set().union(*file_lists.values()) & shared): errors.append(f'{x}: file cannot be both owned and shared')
 current=[]
 for p in root.rglob('*'):
  if not p.is_file() or any(x in p.parts for x in IGN): continue
  rel=norm(p.relative_to(root))
  if rel in shared: continue
  # inside the nearest explicit project directory => project-owned source/artifact.
  if owner_for_rel(rel) in OWNERS and any(
      pathlib.PurePosixPath(rel).is_relative_to(pathlib.PurePosixPath(d))
      for d in project_dir_owner):
   continue
  current.append(rel)
 for rel in sorted(current):
  c=sum(rel in file_lists[o] for o in OWNERS)
  if c!=1: errors.append(f'{rel}: non-project file ownership count must be 1, got {c}')
 for o in OWNERS:
  for rel in sorted(file_lists[o]):
   if not (root/rel).exists(): errors.append(f'{rel}: file manifest entry missing')
 for bad in shared:
  if bad.endswith(('.cs','.csproj','.props','.targets')): errors.append(f'{bad}: shared_metadata must not contain production/build source')
 # package target lookup
 package_owner=dict(man.get('packageOwners',{}))
 def po(pkg):
  if pkg in package_owner:return package_owner[pkg]
  lo=pkg.lower()
  if 'planfuzz' in lo:return 'PLANFUZZ_RESEARCH'
  if '.wist' in lo or lo.endswith('wist'):return 'WIST_PRODUCT'
  return None
 def allowed(so,to,src):
  if to in (None,'SHARED') or so==to:return True
  if so=='UNIVERSAL':return False
  if so=='WIST_PRODUCT':return to=='UNIVERSAL'
  if so=='PLANFUZZ_RESEARCH':
   if to=='UNIVERSAL':return True
   if to=='WIST_PRODUCT':return ('PlanFuzz.Adapter.Wist' in src or 'PlanFuzz.IntegrationTests' in src)
  return False
 def edge(src,target,so,to,rule): return f'{src} -> {target}\nsource_owner={so}\ntarget_owner={to}\nrule={rule}'
 def resolve(base,v):
  if not v or '$(' in v or '@(' in v or '%(' in v:return None
  q=pathlib.Path(v.replace('\\','/'))
  if q.is_absolute():return q.resolve(strict=False)
  return (base/q).resolve(strict=False)
 # assembly map
 asm={}
 for rel in actual:
  try:r=ET.parse(root/rel).getroot(); n=next((e.text.strip() for e in r.iter() if local(e.tag)=='AssemblyName' and e.text),pathlib.Path(rel).stem); asm[n]=rel
  except:pass
 # all dependency-bearing MSBuild XML, including Directory.Build.*
 for xp in root.rglob('*'):
  if not xp.is_file() or xp.suffix.lower() not in XML_EXT or any(x in xp.parts for x in IGN):continue
  src=norm(xp.relative_to(root)); so=owner_for_path(xp)
  if so is None:
   errors.append(f'{src}: dependency-bearing XML has no owner'); continue
  try:r=ET.parse(xp).getroot()
  except Exception as e: errors.append(f'{src}: XML parse failed: {e}'); continue
  for e in r.iter():
   tag=local(e.tag)
   if tag=='PackageReference':
    pkg=e.get('Include') or e.get('Update') or ''; to=po(pkg)
    if to and not allowed(so,to,src):errors.append(edge(src,pkg,so,to,'PackageReference'))
   elif tag=='ProjectReference':
    v=e.get('Include') or ''; q=resolve(xp.parent,v)
    if q:
     try: rel=norm(q.relative_to(root)); to=proj_owner.get(rel) or owner_for_path(q)
     except ValueError: rel=str(q); to='OUTSIDE'
     if to=='OUTSIDE' or (to and not allowed(so,to,src)):errors.append(edge(src,rel,so,to,'ProjectReference'))
   elif tag in ('Compile','Analyzer'):
    v=e.get('Include') or ''; q=resolve(xp.parent,v)
    if q and ('..' in v.replace('\\','/').split('/') or q.is_absolute()):
     try: rel=norm(q.relative_to(root)); to=owner_for_path(q)
     except ValueError: rel=str(q); to='OUTSIDE'
     if to=='OUTSIDE' or (to and not allowed(so,to,src)):errors.append(edge(src,rel,so,to,f'{tag} Include'))
   elif tag=='Import':
    v=e.get('Project') or ''; q=resolve(xp.parent,v)
    if q:
     try: rel=norm(q.relative_to(root)); to=owner_for_path(q)
     except ValueError: rel=str(q); to='OUTSIDE'
     if to=='OUTSIDE' or (to and not allowed(so,to,src)):errors.append(edge(src,rel,so,to,'Import'))
   elif tag=='Reference':
    hint=next((x.text for x in e if local(x.tag)=='HintPath' and x.text),None)
    if hint:
     q=resolve(xp.parent,hint)
     if q:
      try: rel=norm(q.relative_to(root)); to=owner_for_path(q)
      except ValueError: rel=str(q); to='OUTSIDE'
      if to=='OUTSIDE' or (to and not allowed(so,to,src)):errors.append(edge(src,rel,so,to,'Reference/HintPath'))
   elif tag=='MSBuild':
    v=e.get('Projects') or ''; q=resolve(xp.parent,v)
    if q:
     try: rel=norm(q.relative_to(root)); to=proj_owner.get(rel) or owner_for_path(q)
     except ValueError: rel=str(q); to='OUTSIDE'
     if to=='OUTSIDE' or (to and not allowed(so,to,src)):errors.append(edge(src,rel,so,to,'MSBuild target-to-project invocation'))
   elif tag=='InternalsVisibleTo':
    name=e.get('Include') or (e.text or '').strip(); tgt=asm.get(name.split(',')[0].strip()); to=proj_owner.get(tgt) if tgt else None
    if tgt and not allowed(so,to,src):errors.append(edge(src,tgt,so,to,'InternalsVisibleTo'))
 # source IVT + semantic source tokens
 ivt=re.compile(r'InternalsVisibleTo\s*\(\s*["\']([^"\']+)["\']')
 forbidden=man.get('forbiddenUniversalSourceTokens',['Wist','PlanFuzz'])
 for p in root.rglob('*.cs'):
  if any(x in p.parts for x in IGN):continue
  so=owner_for_path(p); rel=norm(p.relative_to(root)); txt=p.read_text(errors='replace')
  for name in ivt.findall(txt):
   tgt=asm.get(name.split(',')[0].strip()); to=proj_owner.get(tgt) if tgt else None
   if tgt and not allowed(so,to,rel):errors.append(edge(rel,tgt,so,to,'source-level InternalsVisibleTo'))
  if so=='UNIVERSAL':
   # tests/research/docs are not production framework; templates are public generic surface and are scanned
   if '/.Tests/' in rel or '/Generic.Tests/' in rel or '/Experiments/' in rel: continue
   for tok in forbidden:
    if tok in txt: errors.append(edge(rel,f"semantic token '{tok}'",so,'WIST/PLANFUZZ semantics','forbiddenUniversalSourceTokens'))
 # symlink escapes
 for p in root.rglob('*'):
  if not p.is_symlink():continue
  try:q=p.resolve(strict=False);q.relative_to(root)
  except Exception:errors.append(f'source symlink/path escape: {norm(p.relative_to(root))} -> {p.resolve(strict=False)}')
 # solutions
 specs={'UniversalToolchain/UniversalToolchain.sln':('UNIVERSAL',{'UNIVERSAL'}),'UniversalToolchain/Wist.sln':('WIST_PRODUCT',{'WIST_PRODUCT','UNIVERSAL'}),'UniversalToolchain/PlanFuzz.sln':('PLANFUZZ_RESEARCH',set(OWNERS))}
 slnrx=re.compile(r'^Project\("\{[^}]+\}"\) = "[^"]+", "([^"]+\.csproj)",',re.M)
 for rel,(ro,allowedset) in specs.items():
  sp=root/rel
  if not sp.exists():errors.append(f'missing static solution: {rel}');continue
  mem=set()
  for v in slnrx.findall(sp.read_text(errors='replace')):
   q=(sp.parent/v.replace('\\','/')).resolve()
   try:rr=norm(q.relative_to(root));mem.add(rr)
   except ValueError:errors.append(f'{rel} contains outside project {v}')
  owned={p for p,o in proj_owner.items() if o==ro}
  for x in sorted(owned-mem):errors.append(f'{rel} missing owned root {x}')
  for x in sorted(mem):
   o=proj_owner.get(x)
   if o and o not in allowedset:errors.append(f'{rel} contains forbidden root {x}')
 if errors:
  print('\n\n'.join(errors),file=sys.stderr);print(f'THREE_REPO_ARCHITECTURE=FAIL errors={len(errors)}',file=sys.stderr);return 1
 counts={o:sum(1 for v in proj_owner.values() if v==o) for o in OWNERS}
 if not a.quiet:print('THREE_REPO_ARCHITECTURE=PASS '+' '.join(f'{o}={counts[o]}' for o in OWNERS)+f' TOTAL={len(actual)} FILES={sum(len(file_lists[o]) for o in OWNERS)}')
 return 0
if __name__=='__main__':raise SystemExit(main())
