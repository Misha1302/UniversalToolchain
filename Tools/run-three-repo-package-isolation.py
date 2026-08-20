#!/usr/bin/env python3
"""Reproduce the three-repository package boundary from a clean generated split.

The harness deliberately never supplies a sibling source checkout as an MSBuild input.
UniversalToolchain artifacts are copied to Wist/packages; Wist artifacts are copied to
PlanFuzz/packages. External third-party packages come from --external-feed (or NuGet.org
when omitted).
"""
from __future__ import annotations
import argparse, os, pathlib, shutil, subprocess, sys
ROOT=pathlib.Path(__file__).resolve().parents[1]

def run(cmd,cwd,env):
    print('+', ' '.join(map(str,cmd)), flush=True)
    r=subprocess.run(list(map(str,cmd)),cwd=cwd,env=env)
    if r.returncode: raise SystemExit(r.returncode)

def write_nuget(repo:pathlib.Path, external:str|None):
    sources=f'<add key="component-packages" value="{repo/"packages"}"/>'
    if external:
        sources+=f'<add key="external" value="{pathlib.Path(external).resolve()}"/>'
    else:
        sources+='<add key="nuget.org" value="https://api.nuget.org/v3/index.json"/>'
    (repo/'NuGet.config').write_text('<?xml version="1.0" encoding="utf-8"?>\n<configuration><packageSources><clear/>'+sources+'</packageSources></configuration>\n')

def copy_packages(src:pathlib.Path,dst:pathlib.Path):
    dst.mkdir(parents=True,exist_ok=True)
    for p in (src/'artifacts/packages').glob('*.nupkg'):
        shutil.copy2(p,dst/p.name)

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--output',required=True)
    ap.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'))
    ap.add_argument('--external-feed')
    a=ap.parse_args()
    out=pathlib.Path(a.output).resolve()
    shutil.rmtree(out,ignore_errors=True)
    env={k:v for k,v in os.environ.items() if k.upper()!='PLATFORM'}
    env['DOTNET']=str(pathlib.Path(a.dotnet).resolve()) if '/' in a.dotnet else a.dotnet
    if a.external_feed: env['EXTERNAL_NUGET_FEED']=str(pathlib.Path(a.external_feed).resolve())
    run([sys.executable,ROOT/'Tools/create-three-repo-candidates.py','--output',out],ROOT,env)
    ut,wist,pf=(out/'UniversalToolchain',out/'Wist',out/'PlanFuzz')
    for repo in (ut,wist,pf): write_nuget(repo,a.external_feed)
    # UniversalToolchain is self-contained source; its public and reviewed private artifacts seed Wist.
    run(['./build.sh','--pack'],ut,env)
    run([sys.executable,'Tools/package-consumer-smoke.py'],ut,env)
    copy_packages(ut,wist/'packages')
    # Wist may consume UT only from packages. No source path is provided to the command or config.
    run([sys.executable,'Tools/check-dependency-packages.py','--require','UniversalToolchain.RepositoryBundle'],wist,env)
    run(['./build.sh','--pack'],wist,env)
    run([sys.executable,'Tools/package-consumer-smoke.py'],wist,env)
    copy_packages(ut,pf/'packages'); copy_packages(wist,pf/'packages')
    # PlanFuzz consumes both upstream components as packages; Adapter.Wist is the allowed Wist edge.
    run([sys.executable,'Tools/check-dependency-packages.py','--require','UniversalToolchain.RepositoryBundle'],pf,env)
    run([sys.executable,'Tools/check-dependency-packages.py','--require','UniversalToolchain.Wist'],pf,env)
    run(['./build.sh'],pf,env)
    run([env['DOTNET'],'test','UniversalToolchain/UniversalToolchain.PlanFuzz.IntegrationTests/UniversalToolchain.PlanFuzz.IntegrationTests.csproj','-c','Release','--no-build','--no-restore','--filter','StrictReplayTests','-p:NuGetAudit=false'],pf,env)
    print('THREE_REPO_PACKAGE_ISOLATION=PASS')
if __name__=='__main__': main()
