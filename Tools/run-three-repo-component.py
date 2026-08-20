#!/usr/bin/env python3
import argparse, json, os, pathlib, subprocess, sys
ROOT=pathlib.Path(__file__).resolve().parents[1]
COMP={
 'universal':('UniversalToolchain/UniversalToolchain.sln','eng/tests/universal.json','UNIVERSAL'),
 'wist':('UniversalToolchain/Wist.sln','eng/tests/wist.json','WIST_PRODUCT'),
 'planfuzz':('UniversalToolchain/PlanFuzz.sln','eng/tests/planfuzz.json','PLANFUZZ_RESEARCH'),
}
def run(cmd, env=None):
 print('+',' '.join(map(str,cmd)),flush=True)
 r=subprocess.run(cmd,cwd=ROOT,env=env)
 if r.returncode: raise SystemExit(r.returncode)
def main():
 ap=argparse.ArgumentParser()
 g=ap.add_mutually_exclusive_group(required=True); g.add_argument('--component',choices=COMP); g.add_argument('--all',action='store_true')
 ap.add_argument('--configuration',default='Release'); ap.add_argument('--serial',action='store_true'); ap.add_argument('--no-build-servers',action='store_true'); ap.add_argument('--skip-tests',action='store_true')
 a=ap.parse_args()
 run([sys.executable,'Tools/check-three-repo-architecture.py']); run([sys.executable,'Tools/check-three-repo-test-inventory.py'])
 dotnet=os.environ.get('DOTNET','dotnet'); env=os.environ.copy(); env.pop('PLATFORM',None)
 comps=['universal','wist','planfuzz'] if a.all else [a.component]
 cfg=os.environ.get('NUGET_CONFIG')
 for c in comps:
  sln,manifest,_=COMP[c]
  restore=[dotnet,'restore',sln,'-p:Platform=Any CPU','-p:NuGetAudit=false']
  if cfg: restore += ['--configfile',cfg]
  if a.serial: restore += ['--disable-parallel']
  run(restore,env)
  build=[dotnet,'build',sln,'-c',a.configuration,'--no-restore','-p:Platform=Any CPU','-p:NuGetAudit=false']
  if a.serial: build += ['-m:1','-p:BuildInParallel=false','-p:UseSharedCompilation=false']
  if a.no_build_servers: build += ['--disable-build-servers']
  run(build,env)
  if not a.skip_tests:
   data=json.load(open(ROOT/manifest))
   for p in data['projects']:
    test=[dotnet,'test',p,'-c',a.configuration,'--no-build','--no-restore','-p:NuGetAudit=false']
    if a.no_build_servers: test += ['--disable-build-servers']
    run(test,env)
 return 0
if __name__=='__main__': raise SystemExit(main())
