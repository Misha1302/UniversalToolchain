#!/usr/bin/env python3
import json,os,pathlib,subprocess,sys,tempfile,zipfile,xml.etree.ElementTree as ET
root=pathlib.Path(__file__).resolve().parents[1];m=json.load(open(root/'eng/component.json'));d=os.environ.get('DOTNET','dotnet');feed=root/'artifacts/packages';env={k:v for k,v in os.environ.items() if k.upper()!='PLATFORM'}
def pkg(pid):
    matches=[]
    for candidate in sorted(feed.glob('*.nupkg')):
        with zipfile.ZipFile(candidate) as z:
            ns=next(x for x in z.namelist() if x.endswith('.nuspec')); r=ET.fromstring(z.read(ns))
            package_id=next((e.text for e in r.iter() if e.tag.endswith('id') and e.text),None)
            version=next((e.text for e in r.iter() if e.tag.endswith('version') and e.text),None)
            if package_id==pid and version: matches.append((candidate,version))
    if not matches:raise SystemExit('PACKAGE_CONSUMER_SMOKE=FAIL missing exact package '+pid)
    return matches[-1][1]
comp=m['component']
if comp=='UNIVERSAL':pid='UniversalToolchain.Language.Abstractions';code='using System; using System.Reflection; Console.WriteLine(Assembly.Load("UniversalToolchain.Language.Abstractions").GetName().Name);'
elif comp=='WIST_PRODUCT':pid='UniversalToolchain.Wist';code='using System; using UniversalToolchain.Wist; using var e=WistEngine.CreateRestrictedArithmetic(); Console.WriteLine(e.Evaluate<double>("1+2"));'
else:print('PACKAGE_CONSUMER_SMOKE=SKIP component='+comp);sys.exit(0)
ver=pkg(pid)
with tempfile.TemporaryDirectory(prefix='ut-consumer-') as td:
    td=pathlib.Path(td); (td/'consumer.csproj').write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="{pid}" Version="{ver}" /></ItemGroup></Project>');(td/'Program.cs').write_text(code)
    cfg=td/'NuGet.config'
    sources=f'<add key="candidate" value="{feed}"/><add key="deps" value="{root/"packages"}"/>'
    external=os.environ.get('EXTERNAL_NUGET_FEED')
    if external:sources+=f'<add key="external" value="{external}"/>'
    else:sources+='<add key="nuget.org" value="https://api.nuget.org/v3/index.json"/>'
    cfg.write_text(f'<?xml version="1.0"?><configuration><packageSources><clear/>{sources}</packageSources></configuration>')
    for cmd in ([d,'restore','consumer.csproj','--configfile',str(cfg),'-p:NuGetAudit=false'],[d,'run','--project','consumer.csproj','-c','Release','--no-restore']):
        r=subprocess.run(cmd,cwd=td,env=env)
        if r.returncode:sys.exit(r.returncode)
print('PACKAGE_CONSUMER_SMOKE=PASS component='+comp)
