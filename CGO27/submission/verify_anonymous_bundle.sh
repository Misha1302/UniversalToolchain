#!/usr/bin/env bash
set -euo pipefail
bundle="$(cd "${1:?bundle directory is required}" && pwd)"

(cd "$bundle" && sha256sum -c MANIFEST.sha256)

test "$(cat "$bundle/EXTERNAL_CORPUS_STATUS")" = "BLOCKED_EXTERNAL"
test "$(cat "$bundle/PERFORMANCE_STATUS")" = "BLOCKED_PINNED_MACHINE"
test "$(cat "$bundle/IDENTITY_SCAN_RECEIPT")" = "ANONYMOUS_IDENTITY_SCAN=PASS"
grep -Eq '^content-sha256:[0-9a-f]{64}$' "$bundle/SOURCE_REVISION"

if find "$bundle" -type d \( -name .git -o -name .github -o -name bin -o -name obj -o -name .idea \) -print -quit | grep -q .; then
  echo "forbidden repository/build directory entered anonymous bundle" >&2
  exit 1
fi

if grep -RInaE '/(home|mnt)/' "$bundle" \
    --exclude='MANIFEST.sha256' \
    --exclude='anonymous-system-source.tar.gz' \
    --exclude='paper.pdf' | head -n 1 | grep -q .; then
  echo "local absolute path entered anonymous bundle" >&2
  exit 1
fi

work="${TMPDIR:-/tmp}/anonymous-review-supplement-verify-$$"
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/source"
tar -xzf "$bundle/source/anonymous-system-source.tar.gz" -C "$work/source"
python3 "$bundle/verification/validate_anonymous_source.py" "$work/source"

if [[ -f "$bundle/paper/paper.pdf" ]]; then
  pdfinfo "$bundle/paper/paper.pdf" | grep -q 'Page size:.*612 x 792 pts'
  pages="$(pdfinfo "$bundle/paper/paper.pdf" | awk '/^Pages:/ {print $2}')"
  [[ "$pages" =~ ^[0-9]+$ ]] && (( pages <= 11 ))
  pdffonts "$bundle/paper/paper.pdf" | awk 'NR > 2 && $5 != "yes" { bad=1 } END { exit bad }'
  author="$(pdfinfo "$bundle/paper/paper.pdf" | awk -F': *' '/^Author:/ {print $2}')"
  [[ -z "$author" || "$author" = "Anonymous" ]]
fi

echo "CGO27_ANONYMOUS_BUNDLE_VERIFY=PASS"
