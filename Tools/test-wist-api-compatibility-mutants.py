#!/usr/bin/env python3
from __future__ import annotations
import argparse
import importlib.util
import shutil
import tempfile
from pathlib import Path


def load_checker(root: Path):
    path = root / 'Tools' / 'check-wist-api-compatibility.py'
    spec = importlib.util.spec_from_file_location('api_checker', path)
    assert spec and spec.loader
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', default=str(Path(__file__).resolve().parents[1]))
    args = parser.parse_args()
    root = Path(args.root).resolve()
    checker = load_checker(root)
    baseline = root/'eng/wist-public-api-baseline.txt'
    provenance = root/'eng/wist-api-baseline.json'
    checker.validate_baseline_provenance(baseline, provenance)
    checker.validate_decision_ledger(root/'eng/wist-api-compatibility.csv')
    checker.validate_exact_diff(baseline, root/'UniversalToolchain/UniversalToolchain.Wist/PublicAPI.Shipped.txt', root/'eng/wist-api-deltas.csv')
    with tempfile.TemporaryDirectory(prefix='wist-api-mutant-') as tmp:
        current = Path(tmp)/'PublicAPI.Shipped.txt'
        shutil.copy2(root/'UniversalToolchain/UniversalToolchain.Wist/PublicAPI.Shipped.txt', current)
        current.write_text(current.read_text() + '\nmethod System.Void UniversalToolchain.Wist.SeededMutant.Forbidden()\n', encoding='utf-8')
        try:
            checker.validate_exact_diff(baseline, current, root/'eng/wist-api-deltas.csv')
        except ValueError:
            print('SURVIVOR=0 mutant=unclassified-public-api-addition')
        else:
            raise RuntimeError('unclassified public API addition survived')

    with tempfile.TemporaryDirectory(prefix='wist-api-paired-mutant-') as tmp:
        mutated_baseline = Path(tmp)/'wist-public-api-baseline.txt'
        mutated_current = Path(tmp)/'PublicAPI.Shipped.txt'
        shutil.copy2(baseline, mutated_baseline)
        shutil.copy2(root/'UniversalToolchain/UniversalToolchain.Wist/PublicAPI.Shipped.txt', mutated_current)
        addition = '\nmethod System.Void UniversalToolchain.Wist.SeededMutant.PairedForbidden()\n'
        mutated_baseline.write_text(mutated_baseline.read_text() + addition, encoding='utf-8')
        mutated_current.write_text(mutated_current.read_text() + addition, encoding='utf-8')
        try:
            checker.validate_baseline_provenance(mutated_baseline, provenance)
            checker.validate_exact_diff(mutated_baseline, mutated_current, root/'eng/wist-api-deltas.csv')
        except ValueError:
            print('SURVIVOR=0 mutant=paired-current-and-baseline-rewrite')
        else:
            raise RuntimeError('paired current+baseline public API rewrite survived')
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
