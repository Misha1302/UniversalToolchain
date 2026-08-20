#!/usr/bin/env python3
from __future__ import annotations
import argparse,pathlib,shutil,os
ROOT=pathlib.Path(__file__).resolve().parents[1]
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--candidate',required=True);ap.add_argument('--owner',required=True);ap.add_argument('--solution',required=True);a=ap.parse_args()
 cand=pathlib.Path(a.candidate).resolve(); owner=a.owner; sln=a.solution
 nested=cand/'UniversalToolchain/NuGet.config'
 if nested.exists(): nested.unlink()
 tools=cand/'Tools';tools.mkdir(exist_ok=True)
 for src,dst in [('split-repository-validator.py','check-repository-architecture.py'),('split-check-docs.py','check-docs.py'),('split-check-dependency-packages.py','check-dependency-packages.py'),('split-package-consumer-smoke.py','package-consumer-smoke.py')]:
  shutil.copy2(ROOT/'Tools'/src,tools/dst);os.chmod(tools/dst,0o755)
 wf=cand/'.github/workflows';wf.mkdir(parents=True,exist_ok=True)
 legacy=cand/('research/legacy-workflows' if owner=='PLANFUZZ_RESEARCH' else 'docs/legacy-workflows');legacy.mkdir(parents=True,exist_ok=True)
 for p in list(wf.glob('*.yml'))+list(wf.glob('*.yaml')):
  if p.name!='ci.yml':shutil.move(str(p),legacy/p.name)
 title='UniversalToolchain' if owner=='UNIVERSAL' else 'Wist' if owner=='WIST_PRODUCT' else 'PlanFuzz'
 dep=('This repository is language-neutral and has no Wist or PlanFuzz production source dependency.' if owner=='UNIVERSAL' else 'UniversalToolchain is consumed only through reviewed NuGet artifacts in `packages/`; no UniversalToolchain source checkout is required.' if owner=='WIST_PRODUCT' else 'UniversalToolchain and Wist are consumed only through reviewed NuGet artifacts in `packages/`; Wist code dependency is restricted to Adapter.Wist/integration layers.')
 (cand/'README.md').write_text(f'# {title}\n\nIndependent split candidate generated from `Misha1302/UniversalToolchain@8399b7de25ee850203e9db84f3bf1db7a4c85c79`.\n\n{dep}\n\n## Build\n\n```bash\n./build.sh\n```\n\nUse `./build.sh --pack` where the component is packable. Architecture checks run before restore/build. See `docs/architecture.md`, `docs/CONTRIBUTING.md`, and `docs/package-boundary.md`.\n')
 d=cand/'docs';d.mkdir(parents=True,exist_ok=True)
 (d/'architecture.md').write_text(f'# {title} architecture\n\nThis repository is the `{owner}` component. Its canonical solution is `{sln}`. Solution membership is a build/IDE surface; source ownership is recorded in `eng/component.json`.\n\nCross-repository source paths, ProjectReference edges, imports, source includes, hardcoded DLL paths and source probing are forbidden. `Tools/check-repository-architecture.py` scans all dependency-bearing `*.csproj`, `*.props` and `*.targets` files before build.\n')
 extra=(' Wist migration guardrails treat `LanguageCompiler` and `LanguagePlan` as canonical framework concepts; do not reintroduce retired Wist-only ownership for them.' if owner=='WIST_PRODUCT' else '')
 (d/'CONTRIBUTING.md').write_text(f'# Contributing to {title}\n\nRun `python3 Tools/check-repository-architecture.py` first, then `./build.sh`. For packable components use `./build.sh --pack`. Never add a path to a sibling repository; cross-repository dependencies must be NuGet artifacts.{extra}\n')
 bundle='`UniversalToolchain.RepositoryBundle` is a private review/dev artifact only. It is not a public package dependency contract.'
 (d/'package-boundary.md').write_text(f'# Package boundary\n\n{bundle}\n\nThe local `packages/` directory is the reviewed dependency feed for candidate isolation. Public Wist packages must not depend on the private repository bundle.\n')
 jobs={'architecture':['python3 Tools/check-repository-architecture.py'],'build':['./build.sh'],'tests':['./build.sh']}
 if owner=='UNIVERSAL':jobs.update({'package':['./build.sh --pack'],'consumer-smoke':['./build.sh --pack','python3 Tools/package-consumer-smoke.py'],'docs':['python3 Tools/check-docs.py']})
 elif owner=='WIST_PRODUCT':jobs.update({'package':['python3 Tools/check-dependency-packages.py --require UniversalToolchain.RepositoryBundle','./build.sh --pack'],'UT-package-consumer':['python3 Tools/check-dependency-packages.py --require UniversalToolchain.RepositoryBundle','./build.sh'],'consumer-smoke':['./build.sh --pack','python3 Tools/package-consumer-smoke.py'],'docs':['python3 Tools/check-docs.py']})
 else:jobs.update({'UT-package-consumer':['python3 Tools/check-dependency-packages.py --require UniversalToolchain.RepositoryBundle','./build.sh'],'Wist-adapter-consumer':['python3 Tools/check-dependency-packages.py --require UniversalToolchain.Wist','./build.sh'],'research-replay-smoke':['./build.sh','dotnet test UniversalToolchain/UniversalToolchain.PlanFuzz.IntegrationTests/UniversalToolchain.PlanFuzz.IntegrationTests.csproj -c Release --no-build --no-restore --filter StrictReplayTests'],'docs':['python3 Tools/check-docs.py']})
 lines=['name: split candidate CI','on: [push, pull_request]','jobs:']
 for j,cmds in jobs.items():
  lines += [f'  {j}:','    runs-on: ubuntu-latest','    steps:','      - uses: actions/checkout@v4','      - uses: actions/setup-dotnet@v4','        with:','          dotnet-version: 10.0.x']
  for cmd in cmds:lines.append(f'      - run: {cmd}')
 (wf/'ci.yml').write_text('\n'.join(lines)+'\n')
 return 0
if __name__=='__main__':raise SystemExit(main())
