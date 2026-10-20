#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
sha256sum -c SHA256SUMS.txt
python3 - <<'PYVERIFY'
from pathlib import Path
import re
root=Path('prototype')
assert (root/'Contracts/SemanticCore.cs').exists(), 'historical prototype unavailable'
for p in root.rglob('*.csproj'):
    assert p.is_file()
for name in ['00_DECISION.md','01_HYPOTHESIS_MATRIX.md','02_IMPLEMENTATION_ARCHITECTURE.md','03_UT_INTEGRATION_ROADMAP.md','04_VERIFICATION_AND_ADVERSARIAL_TESTS.md','05_EVIDENCE_AND_PROVENANCE.md','06_READY_TO_IMPLEMENT_PROMPT.md','07_LIBRARY_PACKAGING_AND_API.md','08_MIGRATION_AND_ACCEPTANCE.md']:
    assert (Path('docs')/name).exists(), name
print('REVISION_STRUCTURE=PASS')
PYVERIFY
echo "PACKAGE_INTEGRITY=PASS"
