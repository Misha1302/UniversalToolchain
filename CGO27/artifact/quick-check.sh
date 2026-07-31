#!/usr/bin/env bash
set -euo pipefail

self_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
output="${1:-$self_dir/artifacts/quick-check}"

if [[ -d "$self_dir/UniversalToolchain" && -f "$self_dir/Tools/run-cgo27-ablations.sh" ]]; then
  repo="$self_dir"
elif [[ -d "$self_dir/../../UniversalToolchain" ]]; then
  repo="$(cd "$self_dir/../.." && pwd)"
else
  archive="$(find "$self_dir/source" -maxdepth 1 -type f -name 'Wist2-source-*.tar.gz' -print | sort | head -n 1)"
  if [[ -z "$archive" ]]; then
    echo "embedded source archive was not found" >&2
    exit 1
  fi
  work="${TMPDIR:-/tmp}/cgo27-artifact-source-$$"
  rm -rf "$work"
  mkdir -p "$work"
  tar -xzf "$archive" -C "$work"
  trap 'rm -rf "$work"' EXIT
  repo="$work"
fi

output="$(mkdir -p "$output" && cd "$output" && pwd)"
mkdir -p "$repo/UniversalToolchain/packages"
timeout 900 bash "$repo/Tools/run-cgo27-ablations.sh" "$output/evidence"

python3 - "$output/evidence/analysis/ablations.json" <<'PY'
import json,sys
path=sys.argv[1]
data=json.load(open(path,encoding='utf-8'))
assert data['status']=='VALIDATED'
a=data['ablations']
assert a['A1_NO_TYPED_CONTRACTS']['boundaryPrimaryDetected']==12
assert a['A2_NO_REVERIFICATION_DISCHARGE']['boundaryPrimaryDetected']==28
assert a['A3_SELECTIVE_VS_ALWAYS']['boundaryParityCases']==42
assert a['A3_SELECTIVE_VS_ALWAYS']['wistParityCases']==30
assert a['A3_SELECTIVE_VS_ALWAYS']['tensorParityCases']==12
assert not a['A3_SELECTIVE_VS_ALWAYS']['efficiencyHeadlineThresholdMet']
PY

printf '%s\n' "CGO27_ARTIFACT_QUICK_CHECK=PASS" | tee "$output/QUICK_CHECK_RECEIPT"
