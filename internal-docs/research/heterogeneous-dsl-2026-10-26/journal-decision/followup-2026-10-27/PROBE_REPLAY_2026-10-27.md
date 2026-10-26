# Exact probe replay — 2026-10-27

Research-only verification; not a new independent scientific sample.

- Device: connected Fedora; .NET SDK version 10.0.111.
- Source HEAD in clean independent worktree at execution: `ba14e481b14cbf323bfa96f52b469950ab751495`, PR #382.
- Worktree: `/home/micodiy/razakov/sharp/UniversalToolchain-journal-audit-20261027`, separate from user's existing untracked working files.
- Fresh output: `/tmp/ut-journal-probe-20261027.json` (file remains on connected Fedora).
- Command: `dotnet run --project internal-docs/research/heterogeneous-dsl-2026-10-26/journal-decision/probe/DiagnosticProbe.csproj -c Release -p:NuGetAudit=false -- /tmp/ut-journal-probe-20261027.json`
- Verifier: `python3 internal-docs/research/heterogeneous-dsl-2026-10-26/journal-decision/probe/verify_probe.py --replay /tmp/ut-journal-probe-20261027.json`
- Execution exit 0; verifier output: `Verified: source identities, four diagnostic cases, 8+12+2 observations, bounded manual parity, exact replay`.
- Fresh results SHA-256: `cb0cca960de68749d4b7415e0d1010a3c3ad99325ab99bd89bbd39c0f96aef28`; equal to historical exact JSON content hash.
- Observed: selective policy accepts undeclared corruption while always-check rejects; both selective and always-check accept a violation beyond verifier coverage. Matched manual typed policy agrees with selective on this bounded probe.
- Limitations: only four author-designed diagnostic cases, no independent natural bugs, human subjects, timing measures, new external datasets or journal-scale statistics. `verify_probe.py` checks integrity and exact replay, not empirical hypothesis superiority.
- No production files, frozen CGO27 evidence, or old source results edited. Separate branch commit is documentary only.
