#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="${1:-$root/artifacts/cgo27-artifact}"
commit="$(git -C "$root" rev-parse HEAD)"
short="${commit:0:12}"
stage="$output/cgo27-artifact-$short"
archive="$output/cgo27-artifact-$short.tar.gz"

rm -rf "$output"
mkdir -p "$stage"/{source,paper/source,protocols}

git -C "$root" archive --format=tar.gz --output="$stage/source/Wist2-source-$commit.tar.gz" "$commit"
cp "$root/CGO27/artifact/README.md" "$stage/README.md"
cp "$root/CGO27/artifact/STATUS.md" "$stage/STATUS.md"
cp "$root/CGO27/artifact/LICENSES.md" "$stage/LICENSES.md"
cp "$root/CGO27/artifact/EXPECTED_RESULTS.md" "$stage/EXPECTED_RESULTS.md"
cp "$root/CGO27/artifact/Dockerfile" "$stage/Dockerfile"
cp "$root/CGO27/artifact/quick-check.sh" "$stage/quick-check.sh"
cp "$root/CGO27/artifact/reproduce.sh" "$stage/reproduce.sh"
chmod +x "$stage/quick-check.sh" "$stage/reproduce.sh"

cp -a "$root/CGO27/paper/." "$stage/paper/source/"
rm -f "$stage/paper/source/"{main.aux,main.log,main.out,main.pdf,main.fdb_latexmk,main.fls,main.synctex.gz,main.bbl,main.blg}

for file in \
  00_WORKSTATE.md RESULTS_SUMMARY.md CLAIM_EVIDENCE_LEDGER.md DEVIATION_LEDGER.md \
  EXPERIMENT_PROTOCOL.md PERFORMANCE_ENVIRONMENT.md SECOND_LANGUAGE_REPORT.md ABLATION_REPORT.md \
  SUBMISSION_READINESS.md; do
  cp "$root/CGO27/$file" "$stage/protocols/$file"
done
cp -a "$root/CGO27/external-fault-kit" "$stage/protocols/"
cp -a "$root/CGO27/historical-corpus" "$stage/protocols/"
cp -a "$root/CGO27/reviews" "$stage/protocols/"

printf '%s\n' "$commit" > "$stage/COMMIT"
git -C "$root" status --porcelain=v1 > "$stage/BUILD_GIT_STATUS"
if [[ -s "$stage/BUILD_GIT_STATUS" ]]; then
  echo "artifact build requires a clean checkout" >&2
  cat "$stage/BUILD_GIT_STATUS" >&2
  exit 1
fi
printf '%s\n' "BLOCKED_PINNED_MACHINE" > "$stage/PERFORMANCE_STATUS"
printf '%s\n' "BLOCKED_EXTERNAL" > "$stage/EXTERNAL_CORPUS_STATUS"

if find "$stage" -type d \( -name .git -o -name bin -o -name obj -o -name .idea \) -print -quit | grep -q .; then
  echo "forbidden build/cache directory entered artifact" >&2
  exit 1
fi
if grep -RIlE 'gh[pousr]_[A-Za-z0-9]{20,}|BEGIN (RSA|OPENSSH|EC) PRIVATE KEY' "$stage" --exclude='*.tar.gz' | grep -q .; then
  echo "secret-like material detected" >&2
  exit 1
fi
if grep -RIlE '/home/|/mnt/data/' "$stage" --exclude='*.tar.gz' | grep -q .; then
  echo "local absolute path leaked into artifact" >&2
  exit 1
fi

(
  cd "$stage"
  find . -type f ! -name MANIFEST.sha256 -print0 | sort -z | xargs -0 sha256sum > MANIFEST.sha256
  sha256sum -c MANIFEST.sha256
)

tar --sort=name --mtime='UTC 1980-01-01' --owner=0 --group=0 --numeric-owner -C "$output" -cf - "$(basename "$stage")" | gzip -n -9 > "$archive"
(cd "$output" && sha256sum "$(basename "$archive")" > SHA256SUMS && sha256sum -c SHA256SUMS)
printf '%s\n' "$archive"
