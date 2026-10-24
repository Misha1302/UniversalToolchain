#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
sha256sum -c SHA256SUMS.txt
python3 - <<'PYVERIFY'
from pathlib import Path
from zipfile import ZipFile
from hashlib import sha256
archive=Path('evidence/P1_P2_Q_research_snapshot_2026-10-16.zip')
with ZipFile(archive) as z:
    assert z.testzip() is None
    lines=z.read('PRESERVED_SHA256SUMS.txt').decode().splitlines()
    historical=dict(line.split('  ',1)[::-1] for line in lines)
    assert len(historical)==30, len(historical)
    assert len([n for n in historical if n.endswith('.csproj')])==5
    assert set(z.namelist())==set(historical)|{'PRESERVED_SHA256SUMS.txt'}
    for name,digest in historical.items():
        assert sha256(z.read(name)).hexdigest()==digest,name
    assert 'prototype/Contracts/SemanticCore.cs' in historical
for name in ['00_DECISION.md','01_HYPOTHESIS_MATRIX.md','02_IMPLEMENTATION_ARCHITECTURE.md','03_UT_INTEGRATION_ROADMAP.md','04_VERIFICATION_AND_ADVERSARIAL_TESTS.md','05_EVIDENCE_AND_PROVENANCE.md','06_READY_TO_IMPLEMENT_PROMPT.md','07_LIBRARY_PACKAGING_AND_API.md','08_MIGRATION_AND_ACCEPTANCE.md']:
    assert (Path('docs')/name).is_file(),name
assert not list(Path('.').rglob('*.csproj')), 'Research folder must not expose production project files'
print('REVISION_STRUCTURE=PASS')
print('HISTORICAL_SOURCE_LOG_BYTES=PASS')
PYVERIFY
echo "PACKAGE_INTEGRITY=PASS"
