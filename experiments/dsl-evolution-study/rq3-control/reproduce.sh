#!/usr/bin/env bash
set -euo pipefail
ROOT=$(git rev-parse --show-toplevel)
STUDY="$ROOT/experiments/dsl-evolution-study/rq3-control"
BEFORE=214204c5668007353033e6c00f2fecca73e39923
AFTER=5bca2e701a8e213416f0bf1965a5b53b63e9c953
TMP=$(mktemp -d)
cleanup() {
  git -C "$ROOT" worktree remove --force "$TMP/before" >/dev/null 2>&1 || true
  git -C "$ROOT" worktree remove --force "$TMP/after" >/dev/null 2>&1 || true
  rm -rf "$TMP"
}
trap cleanup EXIT

git -C "$ROOT" worktree add --detach "$TMP/before" "$BEFORE" >/dev/null
dotnet build "$TMP/before/experiments/dsl-evolution-study/rq3-control/Rq3Control.csproj" -c Release >/dev/null
python3 "$TMP/before/experiments/dsl-evolution-study/rq3-control/run_oracle.py" --expect retained > "$STUDY/results/oracle-before.jsonl"

git -C "$ROOT" worktree add --detach "$TMP/after" "$AFTER" >/dev/null
dotnet build "$TMP/after/experiments/dsl-evolution-study/rq3-control/Rq3Control.csproj" -c Release >/dev/null
python3 "$TMP/after/experiments/dsl-evolution-study/rq3-control/run_oracle.py" --expect canonicalized > "$STUDY/results/oracle-after.jsonl"

python3 "$STUDY/measure.py"
