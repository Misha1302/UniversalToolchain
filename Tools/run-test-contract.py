#!/usr/bin/env python3
"""Execute the canonical test contract with per-entry timeouts and exact TRX counts."""
from __future__ import annotations

import argparse
import json
import os
import shutil
import signal
import subprocess
import sys
import tempfile
from pathlib import Path

from verify_trx_count import verify_trx


class ContractError(RuntimeError):
    pass


def safe_label(value: str) -> str:
    return ''.join(ch if ch.isalnum() or ch in '._-' else '_' for ch in value)


def run_entry(root: Path, dotnet: str, configuration: str, results: Path, entry: dict) -> int:
    label = str(entry['label'])
    runner = str(entry['runner'])
    path = root / str(entry['path'])
    timeout = int(entry['timeoutSeconds'])
    expected = int(entry['expectedPassed'])
    trx = results / f"{safe_label(label)}.trx"
    if trx.exists():
        trx.unlink()

    if runner == 'project':
        command = [
            dotnet, 'test', str(path), '-c', configuration,
            '--no-build', '--no-restore', '--disable-build-servers',
            '--logger', f'trx;LogFileName={trx.name}',
            '--results-directory', str(results),
            '-p:UseSharedCompilation=false', '-p:NuGetAudit=false',
        ]
    elif runner in {'assembly', 'filter'}:
        command = [
            dotnet, 'vstest', str(path),
            f'--Logger:trx;LogFileName={trx.name}',
            f'--ResultsDirectory:{results}',
        ]
        if runner == 'filter':
            test_filter = str(entry.get('filter', '')).strip()
            if not test_filter:
                raise ContractError(f'{label}: filter runner requires a non-empty filter')
            command.append(f'--TestCaseFilter:{test_filter}')
    else:
        raise ContractError(f'{label}: unsupported runner {runner!r}')

    print(f"TEST-CONTRACT START label={label} expected={expected} timeout={timeout}s", flush=True)
    kwargs: dict = {
        'cwd': root,
        'text': True,
        'stdout': subprocess.PIPE,
        'stderr': subprocess.STDOUT,
        'env': os.environ.copy(),
    }
    if os.name == 'posix':
        kwargs['start_new_session'] = True
    process = subprocess.Popen(command, **kwargs)
    try:
        output, _ = process.communicate(timeout=timeout)
    except subprocess.TimeoutExpired as exc:
        if os.name == 'posix':
            os.killpg(process.pid, signal.SIGKILL)
        else:
            subprocess.run(
                ['taskkill', '/PID', str(process.pid), '/T', '/F'],
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
                check=False,
            )
        output, _ = process.communicate()
        print(output or '', end='')
        raise ContractError(f'{label}: exceeded declared timeout of {timeout} seconds') from exc

    print(output or '', end='')
    if process.returncode != 0:
        raise ContractError(f'{label}: test process exited with code {process.returncode}')
    verify_trx(trx, expected, label)
    print(f"TEST-CONTRACT PASS label={label} passed={expected}", flush=True)
    return expected


def validate_manifest(document: dict) -> list[dict]:
    if document.get('schemaVersion') != 1:
        raise ContractError('unsupported test contract schemaVersion')
    entries = [*document.get('main', []), *document.get('isolated', [])]
    if not entries:
        raise ContractError('test contract has no entries')
    labels: set[str] = set()
    for entry in entries:
        required = {'label', 'documentationName', 'runner', 'path', 'timeoutSeconds', 'expectedPassed'}
        missing = required - set(entry)
        if missing:
            raise ContractError(f'test entry is missing fields: {sorted(missing)}')
        label = str(entry['label'])
        if label in labels:
            raise ContractError(f'duplicate test label: {label}')
        labels.add(label)
        if int(entry['timeoutSeconds']) < 1 or int(entry['expectedPassed']) < 1:
            raise ContractError(f'{label}: timeout and expected count must be positive')
    expected_total = sum(int(entry['expectedPassed']) for entry in entries)
    if int(document.get('totalPassed', -1)) != expected_total:
        raise ContractError(
            f"test contract totalPassed is {document.get('totalPassed')}, but entry sum is {expected_total}"
        )
    return entries


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', default=str(Path(__file__).resolve().parents[1]))
    parser.add_argument('--manifest', default='eng/test-counts.json')
    parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
    parser.add_argument('--configuration', default='Release')
    parser.add_argument('--results-directory', default='artifacts/test-contract')
    args = parser.parse_args(argv)

    root = Path(args.root).resolve()
    manifest_path = (root / args.manifest).resolve()
    document = json.loads(manifest_path.read_text(encoding='utf-8'))
    entries = validate_manifest(document)
    results = (root / args.results_directory).resolve()
    if results.exists():
        shutil.rmtree(results)
    results.mkdir(parents=True)

    total = 0
    for entry in entries:
        total += run_entry(root, args.dotnet, args.configuration, results, entry)
    if total != int(document['totalPassed']):
        raise ContractError(f'executed total {total} does not match contract total {document["totalPassed"]}')
    print(f'TEST-CONTRACT COMPLETE passed={total} entries={len(entries)}')
    return 0


if __name__ == '__main__':
    try:
        raise SystemExit(main())
    except (ContractError, OSError, ValueError, json.JSONDecodeError) as exc:
        print(f'ERROR: {exc}', file=sys.stderr)
        raise SystemExit(1)
