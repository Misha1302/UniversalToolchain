#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <manifest-path>"
  exit 1
fi

manifest_path="$1"

if [[ ! -f "$manifest_path" ]]; then
  echo "Smoke manifest not found: $manifest_path"
  exit 1
fi

while IFS=$'\t' read -r smoke_name smoke_command expected_substrings; do
  if [[ -z "${smoke_name// }" ]] || [[ "${smoke_name}" == \#* ]]; then
    continue
  fi

  if [[ -z "${smoke_command// }" ]]; then
    echo "Smoke command is empty for entry: $smoke_name"
    exit 1
  fi

  echo "::group::Smoke: $smoke_name"
  echo "Running: $smoke_command"

  output="$(bash -lc "$smoke_command")"
  printf '%s\n' "$output"

  if [[ -n "${expected_substrings// }" ]]; then
    remaining_checks="$expected_substrings"
    while [[ -n "$remaining_checks" ]]; do
      current_check="$remaining_checks"
      if [[ "$remaining_checks" == *';;'* ]]; then
        current_check="${remaining_checks%%;;*}"
        remaining_checks="${remaining_checks#*;;}"
      else
        remaining_checks=""
      fi

      if [[ -n "${current_check// }" ]]; then
        grep -F -- "$current_check" <<< "$output" > /dev/null
      fi
    done
  fi

  echo "::endgroup::"
done < "$manifest_path"
