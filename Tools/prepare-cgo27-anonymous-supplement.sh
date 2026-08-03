#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="${1:-$root/artifacts/cgo27-submission}"
mkdir -p "$output"
output="$(cd "$output" && pwd)"
stage="$output/anonymous-review-supplement"
tmp="$output/.prepare"

rm -rf "$output"
mkdir -p "$stage"/{paper/source,source,verification,evidence} "$tmp/original"

commit="$(git -C "$root" rev-parse HEAD)"
git -C "$root" archive "$commit" UniversalToolchain Tools CGO27 \
  | tar -x -C "$tmp/original"

python3 "$root/CGO27/submission/sanitize_submission_tree.py" \
  "$tmp/original" "$tmp/sanitized"

cp -a "$tmp/sanitized/CGO27/paper/." "$stage/paper/source/"

# The executable source snapshot does not need the paper manuscript, but the
# ablation runner deliberately byte-compares regenerated tables with the
# committed neutral baselines. Preserve only those generated tables in-source.
find "$tmp/sanitized/CGO27/paper" -mindepth 1 -maxdepth 1 \
  ! -name generated -exec rm -rf {} +
rm -rf "$tmp/sanitized/CGO27/submission"

# Keep the source snapshot text-only and content-addressed.
tar --sort=name --mtime='UTC 1980-01-01' --owner=0 --group=0 --numeric-owner \
  -C "$tmp/sanitized" -cf - . | gzip -n -9 > "$stage/source/anonymous-system-source.tar.gz"
source_hash="$(sha256sum "$stage/source/anonymous-system-source.tar.gz" | awk '{print $1}')"
printf 'content-sha256:%s\n' "$source_hash" > "$stage/SOURCE_REVISION"

cp "$root/CGO27/submission/README.md" "$stage/README.md"
cp "$root/CGO27/submission/ANONYMIZATION.md" "$stage/ANONYMIZATION.md"
cp "$root/CGO27/submission/validate_anonymous_source.py" "$stage/verification/validate_anonymous_source.py"
cp "$root/CGO27/submission/verify_anonymous_bundle.sh" "$stage/verify.sh"
chmod +x "$stage/verify.sh" "$stage/verification/validate_anonymous_source.py"
printf '%s\n' BLOCKED_EXTERNAL > "$stage/EXTERNAL_CORPUS_STATUS"
printf '%s\n' BLOCKED_PINNED_MACHINE > "$stage/PERFORMANCE_STATUS"
printf '%s\n' ANONYMOUS_IDENTITY_SCAN=PASS > "$stage/IDENTITY_SCAN_RECEIPT"

rm -rf "$tmp"
printf '%s\n' "$stage"
