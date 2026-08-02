#!/usr/bin/env bash
set -euo pipefail

output="${1:?usage: capture-cgo27-performance-environment.sh OUTPUT_DIRECTORY}"
: "${CGO27_PINNED_MACHINE_ID:?CGO27_PINNED_MACHINE_ID is required}"
: "${CGO27_EXPECTED_CPU:?CGO27_EXPECTED_CPU is required}"
: "${CGO27_CPUSET:?CGO27_CPUSET is required}"

if [[ "$(uname -s)" != Linux ]]; then
  echo "decision-grade performance collection requires Linux" >&2
  exit 1
fi
model="$(lscpu | awk -F: '/Model name/ {sub(/^[[:space:]]+/, "", $2); print $2; exit}')"
if [[ "$model" != *"$CGO27_EXPECTED_CPU"* ]]; then
  echo "CPU mismatch: expected substring '$CGO27_EXPECTED_CPU', actual '$model'" >&2
  exit 1
fi
if ! command -v taskset >/dev/null; then
  echo "taskset is required" >&2
  exit 1
fi
selected="$(taskset -c "$CGO27_CPUSET" sh -c 'taskset -pc $$' | sed 's/.*: //')"
if [[ -z "$selected" ]]; then
  echo "CPU affinity could not be established" >&2
  exit 1
fi
IFS=',' read -r -a groups <<< "$CGO27_CPUSET"
for group in "${groups[@]}"; do
  if [[ "$group" == *-* ]]; then
    start="${group%-*}"; end="${group#*-}"
  else
    start="$group"; end="$group"
  fi
  for ((cpu=start; cpu<=end; cpu++)); do
    governor="/sys/devices/system/cpu/cpu${cpu}/cpufreq/scaling_governor"
    if [[ ! -r "$governor" || "$(cat "$governor")" != performance ]]; then
      echo "CPU $cpu is not pinned to performance governor" >&2
      exit 1
    fi
  done
done
load1="$(awk '{print $1}' /proc/loadavg)"
python3 - "$load1" <<'PY'
import sys
load=float(sys.argv[1])
if load > 1.0:
    raise SystemExit(f"background load too high: {load}")
PY

mkdir -p "$output"
printf '%s\n' "$CGO27_PINNED_MACHINE_ID" > "$output/MACHINE_ID"
printf '%s\n' "$CGO27_CPUSET" > "$output/CPUSET"
printf '%s\n' "$load1" > "$output/LOAD1"
uname -a > "$output/uname.txt"
lscpu > "$output/lscpu.txt"
cat /proc/meminfo > "$output/meminfo.txt"
dotnet --info > "$output/dotnet-info.txt"
if [[ -r /proc/cpuinfo ]]; then grep -m1 '^microcode' /proc/cpuinfo > "$output/microcode.txt" || true; fi
if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  git rev-parse HEAD > "$output/COMMIT"
  git status --porcelain=v1 > "$output/git-status.txt"
fi
python3 - "$output" <<'PY'
from pathlib import Path
import hashlib,json,os,platform
root=Path(os.sys.argv[1])
receipt={
 "schemaVersion":1,
 "status":"PINNED_ENVIRONMENT_VALIDATED",
 "machineId":os.environ["CGO27_PINNED_MACHINE_ID"],
 "expectedCpu":os.environ["CGO27_EXPECTED_CPU"],
 "cpuSet":os.environ["CGO27_CPUSET"],
 "platform":platform.platform(),
 "files":{}
}
for path in sorted(root.iterdir()):
 if path.is_file() and path.name not in {"environment.json","MANIFEST.sha256"}:
  receipt["files"][path.name]=hashlib.sha256(path.read_bytes()).hexdigest()
(root/"environment.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
PY
(
 cd "$output"
 find . -type f ! -name MANIFEST.sha256 -print0 | sort -z | xargs -0 sha256sum > MANIFEST.sha256
 sha256sum -c MANIFEST.sha256
)
