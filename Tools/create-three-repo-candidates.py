#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, os, pathlib, re, shutil, uuid, subprocess, sys, xml.etree.ElementTree as ET
ROOT=pathlib.Path(__file__).resolve().parents[1]
BUNDLE_ID='UniversalToolchain.RepositoryBundle'
BUNDLE_VERSION=None
IGNORE=shutil.ignore_patterns('bin','obj','artifacts','packages','.git','.idea','.vs','TestResults','*.user')

def norm(s): return s.replace('\\','/')
def readj(p): return json.load(open(p,encoding='utf-8'))
def copytree(src,dst):
 if src.exists(): shutil.copytree(src,dst,dirs_exist_ok=True,ignore=IGNORE)
def assembly_name(csproj:pathlib.Path):
 try:
  root=ET.parse(csproj).getroot()
  x=root.find('.//AssemblyName')
  return (x.text.strip() if x is not None and x.text else csproj.stem)
 except Exception: return csproj.stem

def resolve_proj(src_project, inc):
 p=(src_project.parent/pathlib.Path(norm(inc))).resolve()
 try: return p.relative_to(ROOT.resolve()).as_posix()
 except ValueError: return None

def solution(path, projects, base):
 cs_type='{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}'
 lines=['Microsoft Visual Studio Solution File, Format Version 12.00','# Visual Studio Version 17','VisualStudioVersion = 17.0.31903.59','MinimumVisualStudioVersion = 10.0.40219.1']
 gids=[]
 for rel in projects:
  rp=pathlib.Path(rel).relative_to(base).as_posix() if str(rel).startswith(str(base)+'/') else pathlib.Path(rel).as_posix()
  name=pathlib.Path(rel).stem
  gid='{'+str(uuid.uuid5(uuid.NAMESPACE_URL,'compilationlab:'+rel)).upper()+'}'
  gids.append(gid)
  lines += [f'Project("{cs_type}") = "{name}", "{rp.replace("/", "\\")}", "{gid}"','EndProject']
 lines += ['Global','\tGlobalSection(SolutionConfigurationPlatforms) = preSolution','\t\tDebug|Any CPU = Debug|Any CPU','\t\tRelease|Any CPU = Release|Any CPU','\tEndGlobalSection','\tGlobalSection(ProjectConfigurationPlatforms) = postSolution']
 for gid in gids:
  for c in ['Debug','Release']:
   lines += [f'\t\t{gid}.{c}|Any CPU.ActiveCfg = {c}|Any CPU',f'\t\t{gid}.{c}|Any CPU.Build.0 = {c}|Any CPU']
 lines += ['\tEndGlobalSection','EndGlobal','']
 path.write_text('\n'.join(lines),encoding='utf-8')

def patch_cross_refs(candidate, owner, owners):
 for rel in owners[owner]:
  p=candidate/rel
  if not p.exists(): continue
  tree=ET.parse(p); root=tree.getroot(); changed=False
  for ig in root.findall('.//ItemGroup'):
   for pr in list(ig.findall('ProjectReference')):
    inc=pr.get('Include','')
    target=resolve_proj(ROOT/rel, inc)
    other={x for o,ps in owners.items() if o!=owner for x in ps}
    if (target and target in other) or (owner=='WIST_PRODUCT' and 'FeatureManifestEmitter' in inc):
     ig.remove(pr); changed=True
  if changed:
   ET.indent(tree,space='  '); tree.write(p,encoding='utf-8',xml_declaration=False)

def add_bundle_reference(candidate, owner):
 props=candidate/'UniversalToolchain/Directory.Build.props'
 tree=ET.parse(props); root=tree.getroot()
 ig=ET.Element('ItemGroup')
 pr=ET.SubElement(ig,'PackageReference',{'Include':BUNDLE_ID,'Version':BUNDLE_VERSION,'GeneratePathProperty':'true','PrivateAssets':'all'})
 root.append(ig); ET.indent(tree,space='  '); tree.write(props,encoding='utf-8',xml_declaration=False)
 if owner=='WIST_PRODUCT':
  lp=candidate/'UniversalToolchain/UniversalToolchain.Wist.LanguagePack/UniversalToolchain.Wist.LanguagePack.csproj'
  tree=ET.parse(lp); r=tree.getroot()
  # Remove monorepo-only project path and resolver target.
  for pg in r.findall('PropertyGroup'):
   for x in list(pg):
    if x.tag=='FeatureManifestEmitterProjectPath': pg.remove(x)
  for t in list(r.findall('Target')):
   if t.get('Name')=='ResolveLanguagePackBuildProviders': r.remove(t)
  # Force package tool path before emitter target.
  emit=next(t for t in r.findall('Target') if t.get('Name')=='EmitToolchainFeatureManifest')
  pg=emit.find('PropertyGroup')
  x=ET.SubElement(pg,'FeatureManifestEmitterResolvedDll')
  x.text='$(PkgUniversalToolchain_RepositoryBundle)/tools/net10.0/UniversalToolchain.FeatureManifestEmitter.dll'
  prep=ET.Element('Target',{'Name':'PrepareUniversalToolchainBundleRuntime','BeforeTargets':'EmitToolchainFeatureManifest','Condition':"'$(DesignTimeBuild)' != 'true'"})
  pig=ET.SubElement(prep,'ItemGroup'); ET.SubElement(pig,'_UniversalToolchainBundleRuntime',{'Include':'$(PkgUniversalToolchain_RepositoryBundle)/lib/net10.0/*.dll'})
  ET.SubElement(prep,'Copy',{'SourceFiles':'@(_UniversalToolchainBundleRuntime)','DestinationFolder':'$(TargetDir)','SkipUnchangedFiles':'true'})
  emit_index=list(r).index(emit); r.insert(emit_index,prep)
  # Filter runtime closure to the pre-split public Wist package assembly set.
  baseline=[x.removesuffix('.dll') for x in (ROOT/'eng/wist-package-lib-baseline.txt').read_text().splitlines() if x.strip()]
  tgt=next(t for t in r.findall('Target') if t.get('Name')=='GetWistLanguagePackRuntimeClosure')
  item=tgt.find('ItemGroup')
  for e in list(item):
   if e.get('Include')=='@(ReferenceCopyLocalPaths)': item.remove(e)
  cond=' Or '.join(f"'%(ReferenceCopyLocalPaths.Filename)' == '{n}'" for n in baseline)
  ET.SubElement(item,'_WistLanguagePackRuntimeClosure',{'Include':'@(ReferenceCopyLocalPaths)','Condition':cond})
  # Package references do not reliably surface every multi-assembly bundle file through
  # ReferenceCopyLocalPaths. Add the reviewed pre-split closure explicitly when present.
  for n in baseline:
   bundle_path=f'$(PkgUniversalToolchain_RepositoryBundle)/lib/net10.0/{n}.dll'
   ET.SubElement(item,'_WistLanguagePackRuntimeClosure',{'Include':bundle_path,'Condition':f"Exists('{bundle_path}')"})
  ET.indent(tree,space='  '); tree.write(lp,encoding='utf-8',xml_declaration=False)
 if owner=='PLANFUZZ_RESEARCH':
  aw=candidate/'UniversalToolchain/UniversalToolchain.PlanFuzz.Adapter.Wist/UniversalToolchain.PlanFuzz.Adapter.Wist.csproj'
  tree=ET.parse(aw); r=tree.getroot(); ig=ET.SubElement(r,'ItemGroup')
  ET.SubElement(ig,'PackageReference',{'Include':'UniversalToolchain.Wist','Version':'0.1.0-alpha.7'})
  ET.indent(tree,space='  '); tree.write(aw,encoding='utf-8',xml_declaration=False)

def write_validator(cand, component, projects):
 t=cand/'Tools'; t.mkdir(parents=True,exist_ok=True)
 shutil.copy2(ROOT/'Tools/split-repository-validator.py',t/'check-repository-architecture.py'); os.chmod(t/'check-repository-architecture.py',0o755)

def write_tools(cand, component, solution_name):
 tools=cand/'Tools'; tools.mkdir(exist_ok=True)
 run_tests=r'''#!/usr/bin/env python3
import json, os, pathlib, subprocess, sys
root=pathlib.Path(__file__).resolve().parents[1]; d=os.environ.get('DOTNET','dotnet'); m=json.load(open(root/'eng/component.json'))
for p in m['tests']:
 r=subprocess.run([d,'test',p,'-c','Release','--no-build','--no-restore','--disable-build-servers','-p:NuGetAudit=false'],cwd=root,env={k:v for k,v in os.environ.items() if k.upper()!='PLATFORM'})
 if r.returncode: sys.exit(r.returncode)
'''
 (tools/'run-tests.py').write_text(run_tests); os.chmod(tools/'run-tests.py',0o755)
 pack=r'''#!/usr/bin/env python3
import json, os, pathlib, subprocess, sys, tempfile
root=pathlib.Path(__file__).resolve().parents[1]; d=os.environ.get('DOTNET','dotnet'); m=json.load(open(root/'eng/component.json')); out=root/'artifacts/packages'; out.mkdir(parents=True,exist_ok=True)
env={k:v for k,v in os.environ.items() if k.upper()!='PLATFORM'}
for p in m.get('packProjects',[]):
 cmd=[d,'pack',p,'-c','Release','--no-restore','--disable-build-servers','-p:NuGetAudit=false','-o',str(out)]
 r=subprocess.run(cmd,cwd=root,env=env)
 if r.returncode: sys.exit(r.returncode)
if m['component']=='UNIVERSAL':
 # Build an internal reviewed artifact contract. It is intentionally not a public package.
 files=[]
 for rel in m['bundleProjects']:
  p=root/rel; name=pathlib.Path(rel).stem
  candidates=list(p.parent.glob('bin/Release/net10.0/*.dll'))+list(p.parent.glob('bin/*/*/Release/net10.0/*.dll'))
  target=next((x for x in candidates if x.name==name+'.dll'),None)
  if target: files.append(target)
 emitter=root/'UniversalToolchain/UniversalToolchain.FeatureManifestEmitter'
 # The private bundle has a build-time tool contract; never silently create a runtime-only bundle.
 emitter_project=emitter/'UniversalToolchain.FeatureManifestEmitter.csproj'
 er=subprocess.run([d,'build',str(emitter_project),'-c','Release','--no-restore','--disable-build-servers','-m:1','-p:BuildInParallel=false','-p:UseSharedCompilation=false','-p:NuGetAudit=false'],cwd=root,env=env)
 if er.returncode: sys.exit(er.returncode)
 toolfiles=[x for pat in ('bin/Release/net10.0/*','bin/*/*/Release/net10.0/*') for x in emitter.glob(pat) if x.is_file() and x.suffix.lower() in {'.dll','.json'}]
 if not any(x.name=='UniversalToolchain.FeatureManifestEmitter.dll' for x in toolfiles): raise SystemExit('RepositoryBundle tool contract missing UniversalToolchain.FeatureManifestEmitter.dll')
 deps={}
 import xml.etree.ElementTree as ET
 for rel in m['bundleProjects']:
  try: rr=ET.parse(root/rel).getroot()
  except Exception: continue
  for pr in rr.findall('.//PackageReference'):
   inc=pr.get('Include'); ver=pr.get('Version') or pr.findtext('Version')
   if inc and ver: deps[inc]=ver
 with tempfile.TemporaryDirectory() as td:
  td=pathlib.Path(td); cs=td/'bundle.csproj'; lines=['<Project Sdk="Microsoft.NET.Sdk">','<PropertyGroup><TargetFramework>net10.0</TargetFramework><PackageId>UniversalToolchain.RepositoryBundle</PackageId><Version>__BUNDLE_VERSION__</Version><IsPackable>true</IsPackable><IncludeBuildOutput>false</IncludeBuildOutput><PackageLicenseExpression>Apache-2.0</PackageLicenseExpression></PropertyGroup>','<ItemGroup>']
  for inc,ver in sorted(deps.items()): lines.append(f'<PackageReference Include="{inc}" Version="{ver}" />')
  for f in sorted(set(files)): lines.append(f'<None Include="{f}" Pack="true" PackagePath="lib/net10.0" />')
  for f in sorted(set(toolfiles)): lines.append(f'<None Include="{f}" Pack="true" PackagePath="tools/net10.0" />')
  lines += ['</ItemGroup>','</Project>']; cs.write_text('\n'.join(lines))
  r=subprocess.run([d,'pack',str(cs),'-c','Release','--disable-build-servers','-p:NuGetAudit=false','-o',str(out)],cwd=root,env=env)
  if r.returncode: sys.exit(r.returncode)
'''
 pack=pack.replace('__BUNDLE_VERSION__',BUNDLE_VERSION)
 (tools/'pack-component.py').write_text(pack); os.chmod(tools/'pack-component.py',0o755)
 build=f'''#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${{BASH_SOURCE[0]}}")" && pwd)"; cd "$root"; unset PLATFORM || true
DOTNET="${{DOTNET:-dotnet}}"; configuration=Release; do_pack=false
while (($#)); do case "$1" in --pack) do_pack=true; shift;; --configuration) configuration="$2"; shift 2;; *) echo "unknown argument: $1" >&2; exit 2;; esac; done
python3 Tools/check-repository-architecture.py
"$DOTNET" restore {solution_name} --disable-parallel --disable-build-servers -m:1 -p:Platform="Any CPU" -p:RestoreBuildInParallel=false -p:RestoreUseStaticGraphEvaluation=false -p:NuGetAudit=false
"$DOTNET" build {solution_name} -c "$configuration" --no-restore --disable-build-servers -m:1 -p:Platform="Any CPU" -p:BuildInParallel=false -p:UseSharedCompilation=false -p:NuGetAudit=false
python3 Tools/run-tests.py
if [[ "$do_pack" == true ]]; then python3 Tools/pack-component.py; fi
'''
 (cand/'build.sh').write_text(build); os.chmod(cand/'build.sh',0o755)
 ps=f'''param([switch]$Pack,[string]$Configuration="Release")\n$ErrorActionPreference="Stop"\n$root=Split-Path -Parent $MyInvocation.MyCommand.Path; Set-Location $root; $env:PLATFORM=$null\npython3 Tools/check-repository-architecture.py; if ($LASTEXITCODE) {{ exit $LASTEXITCODE }}\n$dotnet=if($env:DOTNET){{$env:DOTNET}}else{{"dotnet"}}\n& $dotnet restore {solution_name} --disable-parallel --disable-build-servers -m:1 '-p:Platform=Any CPU' -p:RestoreBuildInParallel=false -p:RestoreUseStaticGraphEvaluation=false -p:NuGetAudit=false; if($LASTEXITCODE){{exit $LASTEXITCODE}}\n& $dotnet build {solution_name} -c $Configuration --no-restore --disable-build-servers -m:1 '-p:Platform=Any CPU' -p:BuildInParallel=false -p:UseSharedCompilation=false -p:NuGetAudit=false; if($LASTEXITCODE){{exit $LASTEXITCODE}}\npython3 Tools/run-tests.py; if($LASTEXITCODE){{exit $LASTEXITCODE}}\nif($Pack){{python3 Tools/pack-component.py; exit $LASTEXITCODE}}\n'''
 (cand/'build.ps1').write_text(ps)
 for src,dst in [('split-check-docs.py','check-docs.py'),('split-check-dependency-packages.py','check-dependency-packages.py'),('split-package-consumer-smoke.py','package-consumer-smoke.py')]:
  shutil.copy2(ROOT/'Tools'/src,tools/dst); os.chmod(tools/dst,0o755)

def write_ci(cand, comp):
 wf=cand/'.github/workflows'; wf.mkdir(parents=True,exist_ok=True)
 jobs={'architecture':['python3 Tools/check-repository-architecture.py'],'build':['./build.sh'],'tests':['./build.sh']}
 if comp=='UNIVERSAL':
  jobs.update({'package':['./build.sh --pack'],'consumer-smoke':['./build.sh --pack','python3 Tools/package-consumer-smoke.py'],'docs':['python3 Tools/check-docs.py']})
 elif comp=='WIST_PRODUCT':
  jobs.update({'package':['python3 Tools/check-dependency-packages.py --require UniversalToolchain.RepositoryBundle','./build.sh --pack'],'UT-package-consumer':['python3 Tools/check-dependency-packages.py --require UniversalToolchain.RepositoryBundle','./build.sh'],'consumer-smoke':['./build.sh --pack','python3 Tools/package-consumer-smoke.py'],'docs':['python3 Tools/check-docs.py']})
 else:
  jobs.update({'UT-package-consumer':['python3 Tools/check-dependency-packages.py --require UniversalToolchain.RepositoryBundle','./build.sh'],'Wist-adapter-consumer':['python3 Tools/check-dependency-packages.py --require UniversalToolchain.Wist','./build.sh'],'research-replay-smoke':['./build.sh','dotnet test UniversalToolchain/UniversalToolchain.PlanFuzz.IntegrationTests/UniversalToolchain.PlanFuzz.IntegrationTests.csproj -c Release --no-build --no-restore --filter StrictReplayTests'],'docs':['python3 Tools/check-docs.py']})
 lines=['name: split candidate CI','on: [push, pull_request]','jobs:']
 for j,cmds in jobs.items():
  lines += [f'  {j}:','    runs-on: ubuntu-latest','    steps:','      - uses: actions/checkout@v4','      - uses: actions/setup-dotnet@v4','        with:','          dotnet-version: 10.0.x']
  for cmd in cmds: lines += [f'      - run: {cmd}']
 (wf/'ci.yml').write_text('\n'.join(lines)+'\n')

def main():
 global BUNDLE_VERSION
 ap=argparse.ArgumentParser(); ap.add_argument('--output',required=True); a=ap.parse_args(); out=pathlib.Path(a.output).resolve()
 status=subprocess.check_output(['git','status','--porcelain','--untracked-files=all'],cwd=ROOT,text=True)
 if status.strip():
  raise SystemExit('split candidate generation requires a clean working tree')
 revision=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
 BUNDLE_VERSION=f'0.3.0-split.2.g{revision[:12]}'
 shutil.rmtree(out,ignore_errors=True); out.mkdir(parents=True)
 m=readj(ROOT/'eng/project-ownership.json'); owners=m['owners']; partitions=readj(ROOT/'eng/repository-partitions.json'); pack=[x.strip() for x in (ROOT/'eng/package-projects.txt').read_text().splitlines() if x.strip() and not x.startswith('#')]
 specs=[('UniversalToolchain','UNIVERSAL','UniversalToolchain.sln'),('Wist','WIST_PRODUCT','Wist.sln'),('PlanFuzz','PLANFUZZ_RESEARCH','PlanFuzz.sln')]
 for name,owner,sln in specs:
  cand=out/name; cand.mkdir()
  for rel in owners[owner]: copytree((ROOT/rel).parent,(cand/rel).parent)
  # File-level split is driven by the canonical explicit partition manifest.
  for rel in partitions.get('files',{}).get(owner,[]):
   src=ROOT/rel; dst=cand/rel
   if src.is_file(): dst.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(src,dst)
  for rel in partitions.get('shared_metadata',[]):
   src=ROOT/rel; dst=cand/rel
   if src.is_file(): dst.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(src,dst)
  if owner=='WIST_PRODUCT':
   for rel in ('WIST_PARITY_MATRIX.json','eng/retired-surface.json'):
    if (ROOT/rel).exists():
     (cand/rel).parent.mkdir(parents=True,exist_ok=True); shutil.copy2(ROOT/rel,cand/rel)
   copytree(ROOT/'eng/wist-new-architecture-migration',cand/'eng/wist-new-architecture-migration')
  for f in ['LICENSE','.gitignore']:
   if (ROOT/f).exists(): shutil.copy2(ROOT/f,cand/f)
  # ambient build configuration
  (cand/'UniversalToolchain').mkdir(exist_ok=True)
  shutil.copy2(ROOT/'UniversalToolchain/Directory.Build.props',cand/'UniversalToolchain/Directory.Build.props')
  if owner=='UNIVERSAL':
   bp=cand/'UniversalToolchain/Directory.Build.props'; text=bp.read_text(); text=text.replace('Solutions are still built sequentially because Wist.sln and PlanFuzz.sln share\n             output directories.', 'Component solutions may be built sequentially when outputs are shared.') ; bp.write_text(text)
  shutil.copy2(ROOT/'UniversalToolchain/global.json',cand/'global.json')
  (cand/'.editorconfig').write_text('root = true\n\n[*]\ncharset = utf-8\nend_of_line = lf\ninsert_final_newline = true\n')
  (cand/'packages').mkdir(); (cand/'packages/.gitkeep').write_text('')
  (cand/'NuGet.config').write_text('<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear/><add key="candidate-packages" value="packages"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>\n')
  patch_cross_refs(cand,owner,owners)
  if owner!='UNIVERSAL': add_bundle_reference(cand,owner)
  # candidate solution has owned source only
  solution(cand/sln,owners[owner],pathlib.Path('.'))
  tests=readj(ROOT/('eng/tests/'+('universal.json' if owner=='UNIVERSAL' else 'wist.json' if owner=='WIST_PRODUCT' else 'planfuzz.json')))['projects']
  packp=[p for p in pack if p in owners[owner]]
  # internal UT bundle excludes tests/templates/samples/experiments, but includes generic testing library.
  bundle=[]
  if owner=='UNIVERSAL':
   for p in owners[owner]:
    if any(x in p for x in ['.Tests/','/Experiments/','UniversalToolchain.Templates','samples/']): continue
    if p.endswith('UniversalToolchain.FeatureManifestEmitter.csproj'): continue
    bundle.append(p)
  comp={'schemaVersion':1,'component':owner,'projects':owners[owner],'tests':tests,'packProjects':packp,'bundleProjects':bundle,'sourceRevision':revision,'artifactContract':{'id':BUNDLE_ID,'version':BUNDLE_VERSION}}
  if owner=='UNIVERSAL': comp['migrationAllowlist']=m.get('migrationAllowlist',[])
  (cand/'eng').mkdir(exist_ok=True); (cand/'eng/component.json').write_text(json.dumps(comp,indent=2)+'\n')
  shutil.copy2(ROOT/'eng/wist-package-lib-baseline.txt',cand/'eng/wist-package-lib-baseline.txt') if owner=='WIST_PRODUCT' else None
  write_validator(cand,owner,owners[owner]); write_tools(cand,owner,sln); write_ci(cand,owner)
  subprocess.run([sys.executable,str(ROOT/'Tools/finalize-split-candidate.py'),'--candidate',str(cand),'--owner',owner,'--solution',sln,'--source-revision',revision],check=True)
 print(out)
if __name__=='__main__': main()
