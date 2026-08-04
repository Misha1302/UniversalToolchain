#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
output="${1:?usage: build-cgo27-anonymous-supplement.sh OUTPUT EVIDENCE_DIR}"
evidence="${2:?usage: build-cgo27-anonymous-supplement.sh OUTPUT EVIDENCE_DIR}"
stage="$output/cgo27-anonymous-supplement"
archive="$output/cgo27-anonymous-supplement.tar.gz"

required=(
  "$evidence/inputs/boundary/analysis/analysis.json"
  "$evidence/inputs/boundary/main/results.jsonl"
  "$evidence/inputs/wist-end-to-end/summary.json"
  "$evidence/inputs/wist-end-to-end/raw-results.jsonl"
  "$evidence/inputs/tensorrules/results.json"
  "$evidence/inputs/mechanisms/mechanism-ablations.json"
)
for path in "${required[@]}"; do
  test -s "$path" || { echo "missing evidence input: $path" >&2; exit 2; }
done

rm -rf "$output"
mkdir -p "$stage"/{analysis,evidence,historical,paper/source,protocols}
cp "$root/CGO27/anonymous-supplement/README.md" "$stage/README.md"
cp "$root/CGO27/anonymous-supplement/ANONYMITY_POLICY.md" "$stage/ANONYMITY_POLICY.md"
cp "$root/CGO27/anonymous-supplement/quick-check.sh" "$stage/quick-check.sh"
cp "$root/CGO27/anonymous-supplement/reproduce.sh" "$stage/reproduce.sh"
chmod +x "$stage/quick-check.sh" "$stage/reproduce.sh"

cp "$root/CGO27/ablations/analyze_ablations.py" "$stage/analysis/analyze_ablations.py"
cp "$root/CGO27/ablations/render_paper_tables.py" "$stage/analysis/render_paper_tables.py"
chmod +x "$stage/analysis/"*.py

cp "$evidence/inputs/boundary/analysis/analysis.json" "$stage/evidence/boundary-analysis.json"
cp "$evidence/inputs/boundary/main/results.jsonl" "$stage/evidence/boundary-results.jsonl"
cp "$evidence/inputs/wist-end-to-end/summary.json" "$stage/evidence/system-w-summary.json"
cp "$evidence/inputs/wist-end-to-end/raw-results.jsonl" "$stage/evidence/system-w-results.jsonl"
cp "$evidence/inputs/tensorrules/results.json" "$stage/evidence/system-t-results.json"
cp "$evidence/inputs/mechanisms/mechanism-ablations.json" "$stage/evidence/mechanism-ablations.json"

cp -a "$root/CGO27/paper/." "$stage/paper/source/"
rm -f "$stage/paper/source/"{main.aux,main.log,main.out,main.pdf,main.fdb_latexmk,main.fls,main.synctex.gz,main.bbl,main.blg}

for file in \
  EXPERIMENT_PROTOCOL.md RESULTS_SUMMARY.md CLAIM_EVIDENCE_LEDGER.md \
  ABLATION_REPORT.md SECOND_LANGUAGE_REPORT.md PERFORMANCE_ENVIRONMENT.md \
  DEVIATION_LEDGER.md; do
  cp "$root/CGO27/$file" "$stage/protocols/$file"
done

python3 - "$root/CGO27/historical-corpus/candidates.json" \
  "$stage/historical/screening-summary.json" <<'PY'
import json, sys
source = json.load(open(sys.argv[1], encoding='utf-8'))
counts = {'included': 0, 'excluded': 0, 'blocked': 0, 'invalid': 0}
for row in source['candidates']:
    status = row['status']
    if status not in counts:
        raise SystemExit(f'unknown historical status: {status}')
    counts[status] += 1
result = {'schemaVersion': 1, 'total': len(source['candidates']), **counts}
json.dump(result, open(sys.argv[2], 'w', encoding='utf-8'), indent=2, sort_keys=True)
open(sys.argv[2], 'a', encoding='utf-8').write('\n')
PY

python3 - "$root/CGO27/historical-corpus/exact-replay-summary.json" \
  "$stage/historical/exact-replay-summary.json" <<'PY'
import json, sys
source = json.load(open(sys.argv[1], encoding='utf-8'))
campaign = source['campaignSummary']
cases = source['cases']
result = {
    'schemaVersion': 1,
    'campaign': {
        'requestedCases': campaign['requestedCases'],
        'completedCases': campaign['completedCases'],
        'confirmedFindings': campaign['confirmedFindings'],
        'distinctFindingClasses': campaign['distinctFindingClasses'],
        'flakyCases': campaign['flakyCases'],
        'inconclusiveCases': campaign['inconclusiveCases'],
        'infrastructureFailures': campaign['infrastructureFailures'],
        'freshProcessAttempts': sum(case['attempts'] for case in cases),
    },
    'cases': [
        {
            'anonymousCaseId': f'H{index:02d}',
            'attempts': case['attempts'],
            'confirmedViolation': case['confirmedViolation'],
            'flaky': case['flaky'],
            'inconclusive': case['inconclusive'],
            'infrastructureFailure': case['infrastructureFailure'],
        }
        for index, case in enumerate(cases, 1)
    ],
    'claimBoundary': {
        'exactPrefixReproduction': True,
        'historicalP2RateAvailable': False,
        'independentlyAuthored': False,
    },
}
json.dump(result, open(sys.argv[2], 'w', encoding='utf-8'), indent=2, sort_keys=True)
open(sys.argv[2], 'a', encoding='utf-8').write('\n')
PY

python3 - "$stage" <<'PY'
from pathlib import Path
import re, sys
root = Path(sys.argv[1])
text_suffixes = {'.cs', '.csproj', '.csv', '.json', '.jsonl', '.md', '.py', '.sh', '.tex', '.txt', '.yml', '.yaml'}
replacements = (
    (re.compile(r'https?://[^\s"<>]*github[^\s"<>]*', re.I), 'https://example.invalid/anonymized'),
    (re.compile(r'/(?:home|mnt|Users|runner)/[^\s"<>]*'), '/ANONYMIZED_PATH'),
    (re.compile(r'[A-Za-z]:(?:\\\\)+(?:Users|home|runner|mnt)(?:\\\\)+[^\s"<>]*', re.I), r'C:\\ANONYMIZED_PATH'),
    (re.compile(r'misha1302', re.I), 'anonymous-account'),
    (re.compile(r'razakov', re.I), 'anonymous-author'),
    (re.compile(r'Wist2'), 'SystemW'),
    (re.compile(r'Wist'), 'SystemW'),
    (re.compile(r'wist'), 'systemw'),
    (re.compile(r'TensorRules'), 'SystemT'),
    (re.compile(r'tensorrules'), 'systemt'),
    (re.compile(r'\b[0-9a-f]{40}\b'), 'ANONYMIZED_REVISION'),
)
for path in sorted(root.rglob('*')):
    if not path.is_file() or path.suffix not in text_suffixes:
        continue
    text = path.read_text(encoding='utf-8')
    for pattern, replacement in replacements:
        text = pattern.sub(replacement, text)
    path.write_text(text, encoding='utf-8')
PY

cp "$root/Tools/check-cgo27-anonymous-supplement.py" "$stage/analysis/check_anonymity.py"
chmod +x "$stage/analysis/check_anonymity.py"
python3 "$stage/analysis/check_anonymity.py" "$stage"
(
  cd "$stage"
  find . -type f ! -name MANIFEST.sha256 -print0 | sort -z | xargs -0 sha256sum > MANIFEST.sha256
  sha256sum -c MANIFEST.sha256
)

tar --sort=name --mtime='UTC 1980-01-01' --owner=0 --group=0 --numeric-owner \
  -C "$output" -cf - "$(basename "$stage")" | gzip -n -9 > "$archive"
(cd "$output" && sha256sum "$(basename "$archive")" > SHA256SUMS && sha256sum -c SHA256SUMS)
printf '%s\n' "$archive"
