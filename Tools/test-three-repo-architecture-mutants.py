#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, os, re, shutil, subprocess, sys, tempfile
from pathlib import Path

VALIDATOR='Tools/check-three-repo-architecture.py'

def add_item_before_project_end(path: Path, xml: str):
    t=path.read_text(encoding='utf-8')
    i=t.rfind('</Project>')
    path.write_text(t[:i]+xml+'\n'+t[i:],encoding='utf-8')

def run_case(source: Path, name: str, expected: str, mutate):
    # Never mutate the canonical source tree.  Each case runs in a same-filesystem
    # hardlink clone; only the bounded files that a mutant may rewrite are detached
    # to private inodes before mutation.  This remains safe even if the mutant
    # process is killed by an external timeout before Python can run cleanup code.
    tracked=[
      'UniversalToolchain/BasicCore/BasicCore.csproj',
      'UniversalToolchain/ArithmeticModule/ArithmeticModule.csproj',
      'UniversalToolchain/UniversalToolchain.PlanFuzz.Core/UniversalToolchain.PlanFuzz.Core.csproj',
      'UniversalToolchain/UniversalToolchain.PlanFuzz.Adapter.Acme/UniversalToolchain.PlanFuzz.Adapter.Acme.csproj',
      'eng/project-ownership.json','UniversalToolchain/UniversalToolchain.sln','UniversalToolchain/Wist.sln',
      'UniversalToolchain/Directory.Build.props',
    ]
    base=Path(tempfile.mkdtemp(prefix='ut-arch-mutant-clone-', dir=source.parent))
    work=base/'repo'
    try:
        clone=subprocess.run(['cp','-al',str(source),str(work)],text=True,capture_output=True,timeout=60)
        if clone.returncode != 0:
            return False,f'{name}: hardlink clone failed\n{(clone.stdout or "")+(clone.stderr or "")}'
        for rel in tracked:
            q=work/rel
            if not q.exists():
                continue
            mode=q.stat().st_mode
            data=q.read_bytes()
            q.unlink()
            q.write_bytes(data)
            os.chmod(q, mode)
        mutate(work)
        cp=subprocess.run([sys.executable,str(work/VALIDATOR),'--root',str(work)],text=True,capture_output=True,timeout=60)
        output=(cp.stdout or '')+(cp.stderr or '')
        if cp.returncode==0:
            return False,f'{name}: validator unexpectedly passed'
        if expected not in output:
            return False,f'{name}: expected reason {expected!r} not found\n{output[:3000]}'
        return True,f'{name}: PASS'
    finally:
        shutil.rmtree(base,ignore_errors=True)

def main():
 ap=argparse.ArgumentParser(); ap.add_argument('--root',default='.'); ap.add_argument('--case'); a=ap.parse_args(); source=Path(a.root).resolve()
 cases=[]
 def project_ref(src,target):
  return lambda r:add_item_before_project_end(r/src,f'  <ItemGroup><ProjectReference Include="{target}" /></ItemGroup>')
 cases.append(('universal-projectref-wist','rule=ProjectReference',project_ref(Path('UniversalToolchain/BasicCore/BasicCore.csproj'),'../ArithmeticModule/ArithmeticModule.csproj')))
 cases.append(('universal-packageref-wist','rule=PackageReference',lambda r:add_item_before_project_end(r/'UniversalToolchain/BasicCore/BasicCore.csproj','  <ItemGroup><PackageReference Include="UniversalToolchain.Wist" Version="0.1.0-alpha.7" /></ItemGroup>')))
 cases.append(('universal-ivt-wist','source-level InternalsVisibleTo',lambda r:(r/'UniversalToolchain/BasicCore/MutantIvt.cs').write_text('using System.Runtime.CompilerServices;\n[assembly: InternalsVisibleTo("ArithmeticModule")]\n')))
 cases.append(('universal-projectref-planfuzz','rule=ProjectReference',project_ref(Path('UniversalToolchain/BasicCore/BasicCore.csproj'),'../UniversalToolchain.PlanFuzz.Core/UniversalToolchain.PlanFuzz.Core.csproj')))
 cases.append(('wist-projectref-planfuzz','rule=ProjectReference',project_ref(Path('UniversalToolchain/ArithmeticModule/ArithmeticModule.csproj'),'../UniversalToolchain.PlanFuzz.Core/UniversalToolchain.PlanFuzz.Core.csproj')))
 cases.append(('planfuzz-core-wist','rule=ProjectReference',project_ref(Path('UniversalToolchain/UniversalToolchain.PlanFuzz.Core/UniversalToolchain.PlanFuzz.Core.csproj'),'../UniversalToolchain.Wist/UniversalToolchain.Wist.csproj')))
 cases.append(('planfuzz-acme-wist','rule=ProjectReference',project_ref(Path('UniversalToolchain/UniversalToolchain.PlanFuzz.Adapter.Acme/UniversalToolchain.PlanFuzz.Adapter.Acme.csproj'),'../UniversalToolchain.Wist/UniversalToolchain.Wist.csproj')))
 def hidden_import(r):
  (r/'UniversalToolchain/ArithmeticModule/foreign.targets').write_text('<Project />\n')
  add_item_before_project_end(r/'UniversalToolchain/BasicCore/BasicCore.csproj','  <Import Project="../ArithmeticModule/foreign.targets" />')
 cases.append(('hidden-import','rule=Import',hidden_import))
 def compile_inc(r):
  (r/'UniversalToolchain/ArithmeticModule/Foreign.cs').write_text('internal class Foreign {}\n')
  add_item_before_project_end(r/'UniversalToolchain/BasicCore/BasicCore.csproj','  <ItemGroup><Compile Include="../ArithmeticModule/Foreign.cs" /></ItemGroup>')
 cases.append(('compile-include','rule=Compile Include',compile_inc))
 def unclassified(r):
  p=r/'UniversalToolchain/Mutant';p.mkdir();(p/'Mutant.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>\n')
 cases.append(('unclassified-project','project ownership count must be 1',unclassified))
 def overlap(r):
  p=r/'eng/project-ownership.json';d=json.load(open(p));d['owners']['WIST_PRODUCT'].append('UniversalToolchain/BasicCore/BasicCore.csproj');p.write_text(json.dumps(d,indent=2)+'\n')
 cases.append(('overlap-owner','project ownership count must be 1',overlap))
 def missing_root(r):
  p=r/'UniversalToolchain/UniversalToolchain.sln';t=p.read_text();
  # remove BasicCore project block only
  t=re.sub(r'Project\("\{[^}]+\}"\) = "BasicCore", "BasicCore\\BasicCore\.csproj", "\{[^}]+\}"\nEndProject\n','',t, count=1)
  p.write_text(t)
 cases.append(('missing-solution-root','missing owned root UniversalToolchain/BasicCore/BasicCore.csproj',missing_root))
 def forbidden_root(r):
  p=r/'UniversalToolchain/Wist.sln';t=p.read_text();g='{11111111-1111-1111-1111-111111111111}';block=f'Project("{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}") = "UniversalToolchain.PlanFuzz.Core", "UniversalToolchain.PlanFuzz.Core\\UniversalToolchain.PlanFuzz.Core.csproj", "{g}"\nEndProject\n';p.write_text(t.replace('Global\n',block+'Global\n',1))
 cases.append(('forbidden-solution-root','contains forbidden root UniversalToolchain/UniversalToolchain.PlanFuzz.Core/UniversalToolchain.PlanFuzz.Core.csproj',forbidden_root))
 def hintpath(r):
  add_item_before_project_end(r/'UniversalToolchain/BasicCore/BasicCore.csproj','  <ItemGroup><Reference Include="ArithmeticModule"><HintPath>../ArithmeticModule/bin/Release/net10.0/ArithmeticModule.dll</HintPath></Reference></ItemGroup>')
 cases.append(('hardcoded-assembly-path','rule=Reference/HintPath',hintpath))
 cases.append(('cross-repo-ivt','source-level InternalsVisibleTo',lambda r:(r/'UniversalToolchain/BasicCore/CrossRepoIvt.cs').write_text('using System.Runtime.CompilerServices;\n[assembly: InternalsVisibleTo("UniversalToolchain.Wist")]\n')))
 def symlink_escape(r):
  out=r.parent/'outside.cs'; out.write_text('class Outside {}\n'); os.symlink(out,r/'UniversalToolchain/BasicCore/OutsideLink.cs')
 cases.append(('symlink-escape','source symlink/path escape',symlink_escape))
 def props_packageref(r):
  p=r/'UniversalToolchain/Directory.Build.props'
  text=p.read_text(); i=text.rfind('</Project>'); p.write_text(text[:i]+'  <ItemGroup><PackageReference Include="UniversalToolchain.Wist" Version="0.1.0-alpha.7" /></ItemGroup>\n'+text[i:])
 cases.append(('directory-build-props-packageref','rule=PackageReference',props_packageref))
 def props_compile(r):
  foreign=r/'UniversalToolchain/ArithmeticModule/PropsLeak.cs'; foreign.write_text('internal class PropsLeak {}\n')
  p=r/'UniversalToolchain/Directory.Build.props'; text=p.read_text(); i=text.rfind('</Project>'); p.write_text(text[:i]+'  <ItemGroup><Compile Include="ArithmeticModule/PropsLeak.cs" /></ItemGroup>\n'+text[i:])
 cases.append(('directory-build-props-compile','rule=Compile Include',props_compile))
 def universal_wist_token(r):
  (r/'UniversalToolchain/BasicCore/WistProductionLeak.cs').write_text('internal sealed class WistProductionLeak {}\n')
 cases.append(('universal-wist-production-token','rule=forbiddenUniversalSourceTokens',universal_wist_token))
 if a.case: cases=[c for c in cases if c[0]==a.case]
 if a.case and not cases: raise SystemExit('unknown case '+a.case)
 failures=[]
 for name,expect,mut in cases:
  ok,msg=run_case(source,name,expect,mut); print(msg,flush=True)
  if not ok:
   failures.append(msg)
   continue
  pristine=subprocess.run([sys.executable,str(source/VALIDATOR),'--root',str(source),'--quiet'],text=True,capture_output=True,timeout=60)
  if pristine.returncode != 0:
   detail=(pristine.stdout or '')+(pristine.stderr or '')
   leak=f'{name}: pristine validator failed after mutant restoration\n{detail[:3000]}'
   print(leak,flush=True)
   failures.append(leak)
 if failures:
  print(f'THREE_REPO_ARCH_MUTANTS=FAIL passed={len(cases)-len(failures)} total={len(cases)}',file=sys.stderr); return 1
 print(f'THREE_REPO_ARCH_MUTANTS=PASS passed={len(cases)} total={len(cases)}')
 return 0
if __name__=='__main__': raise SystemExit(main())
