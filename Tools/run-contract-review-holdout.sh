#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

out_dir="${1:-artifacts/contract-review-holdout}"
project="UniversalToolchain/Experiments/UniversalToolchain.ContractReviewHoldouts/UniversalToolchain.ContractReviewHoldouts.csproj"
source_dir="UniversalToolchain/Experiments/UniversalToolchain.ContractReviewHoldouts"

rm -rf "$out_dir"
mkdir -p "$out_dir/source" "$out_dir/environment"
mkdir -p UniversalToolchain/packages
unset PLATFORM || true

commit="$(git -C "$root" rev-parse HEAD 2>/dev/null || true)"
if [[ -z "$commit" ]]; then
  commit="${CGO27_SOURCE_SHA:-${CONTRACT_REVIEW_HOLDOUT_COMMIT:-${GITHUB_SHA:-}}}"
fi
if [[ ! "$commit" =~ ^[0-9a-f]{40}$ ]]; then
  echo "exact 40-hex source commit could not be resolved" >&2
  exit 2
fi
unset GITHUB_SHA || true
export CONTRACT_REVIEW_HOLDOUT_COMMIT="$commit"
printf '%s\n' "$commit" > "$out_dir/environment/commit.txt"
git status --porcelain=v1 > "$out_dir/environment/git-status.txt" 2>/dev/null || true
dotnet --info > "$out_dir/environment/dotnet-info.txt"
uname -a > "$out_dir/environment/uname.txt"

cp "$source_dir/Program.cs" "$out_dir/source/Program.cs"
cp "$source_dir/UniversalToolchain.ContractReviewHoldouts.csproj" "$out_dir/source/UniversalToolchain.ContractReviewHoldouts.csproj"
cp "$source_dir/README.md" "$out_dir/source/README.md"
cp "$source_dir/REVIEW_HOLDOUT_PROTOCOL.md" "$out_dir/source/REVIEW_HOLDOUT_PROTOCOL.md"
cp "Tools/run-contract-review-holdout.sh" "$out_dir/source/run-contract-review-holdout.sh"

# Audit stays off here: this harness reproduces a recorded experiment and is executed from
# documentation blocks with a hard time limit. Fetching the advisory database on a cold
# cache costs minutes (measured 213s vs 5s for one experiment project), which makes the
# harness time out for a reason unrelated to the experiment. Vulnerability policy is
# enforced on the canonical build, not on reproduction harnesses.
dotnet restore "$project" -p:NuGetAudit=false
dotnet build "$project" -c Release --no-restore
dotnet run -c Release --no-build --no-restore --project "$project" -- "$out_dir/results"

(
  cd "$out_dir"
  find . -type f ! -name MANIFEST.sha256 -print0 \
    | sort -z \
    | xargs -0 sha256sum \
    > MANIFEST.sha256
  sha256sum -c MANIFEST.sha256
)
