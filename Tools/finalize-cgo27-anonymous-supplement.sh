#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="${1:-$root/artifacts/cgo27-submission}"
mkdir -p "$output"
output="$(cd "$output" && pwd)"
stage="$output/anonymous-review-supplement"
archive="$output/anonymous-review-supplement.tar.gz"

test -d "$stage"
test -s "$stage/paper/source/main.pdf"
cp "$stage/paper/source/main.pdf" "$stage/paper/paper.pdf"
rm -f "$stage/paper/source/"{main.aux,main.log,main.out,main.pdf,main.fdb_latexmk,main.fls,main.synctex.gz,main.bbl,main.blg}

# Evidence is provider-generated from the anonymized snapshot, then normalized again.
if [[ -d "$output/provider-evidence" ]]; then
  cp -a "$output/provider-evidence/." "$stage/evidence/"
fi
python3 "$root/CGO27/submission/sanitize_submission_tree.py" \
  "$stage/evidence" "$output/.evidence-sanitized"
rm -rf "$stage/evidence"
mv "$output/.evidence-sanitized" "$stage/evidence"

(
  cd "$stage"
  find . -type f ! -name MANIFEST.sha256 -print0 \
    | sort -z | xargs -0 sha256sum > MANIFEST.sha256
)
"$stage/verify.sh" "$stage"

tar --sort=name --mtime='UTC 1980-01-01' --owner=0 --group=0 --numeric-owner \
  -C "$output" -cf - "$(basename "$stage")" | gzip -n -9 > "$archive"
(cd "$output" && sha256sum "$(basename "$archive")" > SHA256SUMS && sha256sum -c SHA256SUMS)
printf '%s\n' "$archive"
