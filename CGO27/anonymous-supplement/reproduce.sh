#!/usr/bin/env bash
set -euo pipefail
self_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
output="${1:-$self_dir/artifacts/full}"
bash "$self_dir/quick-check.sh" "$output"

if command -v pdflatex >/dev/null 2>&1; then
  build="$output/paper-build"
  rm -rf "$build"
  mkdir -p "$build"
  cp -a "$self_dir/paper/source/." "$build/"
  (
    cd "$build"
    pdflatex -interaction=nonstopmode -halt-on-error main.tex >/dev/null
    pdflatex -interaction=nonstopmode -halt-on-error main.tex >/dev/null
    pdflatex -interaction=nonstopmode -halt-on-error main.tex >/dev/null
    ! grep -Eq 'Overfull \\hbox|Citation .* undefined|Reference .* undefined|There were undefined references' main.log
  )
else
  printf '%s\n' 'PAPER_BUILD=SKIPPED_NO_PDFLATEX' > "$output/PAPER_BUILD_STATUS"
fi

printf '%s\n' 'CGO27_ANONYMOUS_SUPPLEMENT_REPRODUCE=PASS' \
  | tee "$output/REPRODUCE_RECEIPT"
