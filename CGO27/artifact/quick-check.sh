#!/usr/bin/env bash
set -euo pipefail

self_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
output="${1:-$self_dir/artifacts/quick-check}"
source_commit=""

if [[ -f "$self_dir/MANIFEST.sha256" ]]; then
  (cd "$self_dir" && sha256sum -c MANIFEST.sha256)
fi
if [[ -f "$self_dir/COMMIT" ]]; then
  source_commit="$(tr -d '\r\n' < "$self_dir/COMMIT")"
  if [[ ! "$source_commit" =~ ^[0-9a-f]{40}$ ]]; then
    echo "artifact COMMIT is not an exact 40-hex revision" >&2
    exit 2
  fi
fi

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
  if [[ -z "$source_commit" ]]; then
    echo "clean artifact is missing its exact COMMIT receipt" >&2
    exit 2
  fi
  work="${TMPDIR:-/tmp}/cgo27-artifact-source-$$"
  rm -rf "$work"
  mkdir -p "$work"
  tar -xzf "$archive" -C "$work"
  trap 'rm -rf "$work"' EXIT
  repo="$work"
fi

if git -C "$repo" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  resolved_commit="$(git -C "$repo" rev-parse HEAD)"
  if [[ -n "$source_commit" && "$resolved_commit" != "$source_commit" ]]; then
    echo "artifact COMMIT does not match checkout HEAD" >&2
    exit 2
  fi
  source_commit="$resolved_commit"
fi
if [[ ! "$source_commit" =~ ^[0-9a-f]{40}$ ]]; then
  echo "exact source revision could not be resolved" >&2
  exit 2
fi
export CGO27_SOURCE_SHA="$source_commit"

output="$(mkdir -p "$output" && cd "$output" && pwd)"
mkdir -p "$repo/UniversalToolchain/packages"
timeout 900 bash "$repo/Tools/run-cgo27-ablations.sh" "$output/evidence"
test "$(cat "$output/evidence/COMMIT")" = "$source_commit"

python3 - "$output/evidence/analysis/ablations.json" <<'PY'
import json,sys
path=sys.argv[1]
data=json.load(open(path,encoding='utf-8'))
assert data['status']=='VALIDATED'
assert data['schemaVersion']==2
a=data['ablations']
m=a['A0_MECHANISM_ISOLATION']
assert m['mechanisms']==8
assert m['fullProtocolDetections']==8
assert m['ablatedProtocolDetections']==0
assert m['controlFalsePositives']==0
assert a['A1_NO_TYPED_CONTRACTS']['boundaryPrimaryDetected']==12
assert a['A2_NO_REVERIFICATION_DISCHARGE']['boundaryPrimaryDetected']==28
assert a['A3_SELECTIVE_VS_ALWAYS']['boundaryParityCases']==42
assert a['A3_SELECTIVE_VS_ALWAYS']['wistParityCases']==30
assert a['A3_SELECTIVE_VS_ALWAYS']['tensorParityCases']==12
assert not a['A3_SELECTIVE_VS_ALWAYS']['efficiencyHeadlineThresholdMet']
PY

printf '%s\n' "CGO27_ARTIFACT_QUICK_CHECK=PASS" | tee "$output/QUICK_CHECK_RECEIPT"
