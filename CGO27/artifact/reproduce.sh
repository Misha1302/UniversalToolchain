#!/usr/bin/env bash
set -euo pipefail
self_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
output="${1:-$self_dir/artifacts/full}"
bash "$self_dir/quick-check.sh" "$output"

paper_source="$self_dir/paper/source"
if [[ -d "$paper_source" ]] && command -v pdflatex >/dev/null 2>&1; then
  paper_build="$output/paper-build"
  rm -rf "$paper_build"
  mkdir -p "$paper_build"
  cp -a "$paper_source/." "$paper_build/"
  (cd "$paper_build" && pdflatex -interaction=nonstopmode -halt-on-error main.tex >/dev/null && pdflatex -interaction=nonstopmode -halt-on-error main.tex >/dev/null && pdflatex -interaction=nonstopmode -halt-on-error main.tex >/dev/null)
else
  printf '%s\n' "PAPER_BUILD=SKIPPED_NO_PDFLATEX" > "$output/PAPER_BUILD_STATUS"
fi
printf '%s\n' "CGO27_ARTIFACT_REPRODUCE=PASS" | tee "$output/REPRODUCE_RECEIPT"
