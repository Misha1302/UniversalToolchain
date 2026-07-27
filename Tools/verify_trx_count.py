#!/usr/bin/env python3
from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def local_name(tag: str) -> str:
    return tag.rsplit('}', 1)[-1]


def verify_trx(path: Path, expected: int, label: str) -> None:
    if expected < 1:
        raise ValueError(f'{label}: expected count must be positive')
    if not path.is_file():
        raise ValueError(f'{label}: TRX result file is missing: {path}')
    try:
        root = ET.parse(path).getroot()
    except (ET.ParseError, OSError) as exc:
        raise ValueError(f'{label}: cannot parse TRX file {path}: {exc}') from exc

    results = [element for element in root.iter() if local_name(element.tag) == 'UnitTestResult']
    outcomes = [element.attrib.get('outcome', '') for element in results]
    non_passed = [outcome for outcome in outcomes if outcome != 'Passed']
    counters = next((element for element in root.iter() if local_name(element.tag) == 'Counters'), None)
    violations: list[str] = []
    if len(results) != expected:
        violations.append(f'UnitTestResult count is {len(results)}, expected {expected}')
    if non_passed:
        violations.append(f'non-passed outcomes: {non_passed}')
    if counters is None:
        violations.append('TRX counters are missing')
    else:
        values = {name: int(counters.attrib.get(name, '-1')) for name in ('total','executed','passed','failed','notExecuted')}
        for name in ('total', 'executed', 'passed'):
            if values[name] != expected:
                violations.append(f'counter {name} is {values[name]}, expected {expected}')
        for name in ('failed', 'notExecuted'):
            if values[name] != 0:
                violations.append(f'counter {name} is {values[name]}, expected 0')
    if violations:
        raise ValueError(f"{label}: TRX verification failed:\n- " + '\n- '.join(violations))
    print(f'{label}: verified {expected}/{expected} passed tests from {path.name}.')


def main() -> int:
    if len(sys.argv) != 4:
        print('usage: verify_trx_count.py <results.trx> <expected-count> <label>', file=sys.stderr)
        return 2
    try:
        verify_trx(Path(sys.argv[1]), int(sys.argv[2]), sys.argv[3])
    except (ValueError, OSError) as exc:
        print(exc, file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
