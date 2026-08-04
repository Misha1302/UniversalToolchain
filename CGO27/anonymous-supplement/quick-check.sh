#!/usr/bin/env bash
set -euo pipefail
self_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
output="${1:-$self_dir/artifacts/quick}"
rm -rf "$output"
mkdir -p "$output"

(cd "$self_dir" && sha256sum -c MANIFEST.sha256)
python3 "$self_dir/analysis/check_anonymity.py" "$self_dir"
PYTHONPYCACHEPREFIX="$output/pycache" python3 -m py_compile \
  "$self_dir/analysis/analyze_ablations.py" \
  "$self_dir/analysis/render_paper_tables.py" \
  "$self_dir/analysis/check_anonymity.py"

python3 "$self_dir/analysis/analyze_ablations.py" \
  "$self_dir/evidence/boundary-analysis.json" \
  "$self_dir/evidence/boundary-results.jsonl" \
  "$self_dir/evidence/system-w-summary.json" \
  "$self_dir/evidence/system-w-results.jsonl" \
  "$self_dir/evidence/system-t-results.json" \
  "$self_dir/evidence/mechanism-ablations.json" \
  "$output/analysis"

cmp "$output/analysis/policy-ablation-table.tex" \
  "$self_dir/paper/source/generated/policy-ablation-table.tex"
cmp "$output/analysis/mechanism-ablation-table.tex" \
  "$self_dir/paper/source/generated/mechanism-ablation-table.tex"

python3 - "$self_dir/historical/screening-summary.json" "$output/analysis/ablations.json" <<'PY'
import json, sys
historical = json.load(open(sys.argv[1], encoding='utf-8'))
analysis = json.load(open(sys.argv[2], encoding='utf-8'))
assert historical == {
    'blocked': 10,
    'excluded': 11,
    'included': 3,
    'invalid': 0,
    'schemaVersion': 1,
    'total': 24,
}
assert analysis['status'] == 'VALIDATED'
assert analysis['schemaVersion'] == 3
assert analysis['claimBoundary']['wholeCompilationPerformance'] == 'BLOCKED_PINNED_MACHINE'
assert analysis['claimBoundary']['externalValidity'] == 'BLOCKED_EXTERNAL'
assert not analysis['claimBoundary']['efficiencyHeadlineAllowed']
PY

printf '%s\n' 'CGO27_ANONYMOUS_SUPPLEMENT_QUICK_CHECK=PASS' \
  | tee "$output/QUICK_CHECK_RECEIPT"
