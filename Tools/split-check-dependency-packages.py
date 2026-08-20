#!/usr/bin/env python3
import argparse,pathlib,sys
root=pathlib.Path(__file__).resolve().parents[1]; ap=argparse.ArgumentParser();ap.add_argument('--require',action='append',default=[]);a=ap.parse_args();names=[p.name.lower() for p in (root/'packages').glob('*.nupkg')]
miss=[x for x in a.require if not any(n.startswith(x.lower()+'.') for n in names)]
if miss:print('DEPENDENCY_PACKAGES=FAIL missing='+','.join(miss));sys.exit(1)
print('DEPENDENCY_PACKAGES=PASS '+','.join(a.require))
