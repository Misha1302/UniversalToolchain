#!/usr/bin/env python3
"""Reproduce the three-repository package boundary from a clean generated split.

The harness deliberately never supplies a sibling source checkout as an MSBuild input.
UniversalToolchain artifacts are copied to Wist/packages; Wist artifacts are copied to
PlanFuzz/packages. External third-party packages come from --external-feed (or NuGet.org
when omitted).
"""
from __future__ import annotations
import argparse, os, pathlib, shutil, subprocess, sys, tempfile
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
    generated={name:out/name for name in ('UniversalToolchain','Wist','PlanFuzz')}
    artifact_feed=out/'package-feed'; artifact_feed.mkdir()
    def isolated_copy(name):
        temporary=tempfile.TemporaryDirectory(prefix=f'ut-{name.lower()}-isolation-')
        repo=pathlib.Path(temporary.name)/'repo'
        shutil.copytree(generated[name],repo)
        return temporary,repo
    # UniversalToolchain is built with no sibling component source tree in its build root.
    ut_tmp,ut=isolated_copy('UniversalToolchain')
    try:
        ut_env=env | {'NUGET_PACKAGES':str(pathlib.Path(ut_tmp.name)/'nuget-packages')}
        write_nuget(ut,a.external_feed)
        run(['./build.sh','--pack'],ut,ut_env)
        run([sys.executable,'Tools/package-consumer-smoke.py'],ut,ut_env)
        copy_packages(ut,artifact_feed)
    finally:
        ut_tmp.cleanup()
    # Wist sees only package artifacts, never a sibling UniversalToolchain checkout.
    wist_tmp,wist=isolated_copy('Wist')
    try:
        wist_env=env | {'NUGET_PACKAGES':str(pathlib.Path(wist_tmp.name)/'nuget-packages')}
        write_nuget(wist,a.external_feed)
        (wist/'packages').mkdir(exist_ok=True)
        for package in artifact_feed.glob('*.nupkg'): shutil.copy2(package,wist/'packages'/package.name)
        run([sys.executable,'Tools/check-dependency-packages.py','--require','UniversalToolchain.RepositoryBundle'],wist,wist_env)
        run(['./build.sh','--pack'],wist,wist_env)
        run([sys.executable,'Tools/package-consumer-smoke.py'],wist,wist_env)
        copy_packages(wist,artifact_feed)
    finally:
        wist_tmp.cleanup()
    # PlanFuzz likewise sees only the reviewed package feed.
    pf_tmp,pf=isolated_copy('PlanFuzz')
    try:
        pf_env=env | {'NUGET_PACKAGES':str(pathlib.Path(pf_tmp.name)/'nuget-packages')}
        write_nuget(pf,a.external_feed)
        (pf/'packages').mkdir(exist_ok=True)
        for package in artifact_feed.glob('*.nupkg'): shutil.copy2(package,pf/'packages'/package.name)
        run([sys.executable,'Tools/check-dependency-packages.py','--require','UniversalToolchain.RepositoryBundle'],pf,pf_env)
        run([sys.executable,'Tools/check-dependency-packages.py','--require','UniversalToolchain.Wist'],pf,pf_env)
        run(['./build.sh'],pf,pf_env)
        run([pf_env['DOTNET'],'test','UniversalToolchain/UniversalToolchain.PlanFuzz.IntegrationTests/UniversalToolchain.PlanFuzz.IntegrationTests.csproj','-c','Release','--no-build','--no-restore','--filter','StrictReplayTests','-p:NuGetAudit=false'],pf,pf_env)
    finally:
        pf_tmp.cleanup()
    print('THREE_REPO_PACKAGE_ISOLATION=PASS')
if __name__=='__main__': main()
