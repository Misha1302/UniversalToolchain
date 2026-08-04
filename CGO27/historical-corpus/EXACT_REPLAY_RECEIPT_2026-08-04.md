# Exact pre-fix replay receipt

Date: 2026-08-04

## Execution identity

- exact source revision: `eb851d4bf80f363969e04abdb4bcddf3e56830f3`;
- exact-source archive SHA-256: `550abe24a08c9967f2add03fed5b1fdaa514e0b52f35509c3f60ba2493dc8b7a`;
- replay manifest SHA-256: `dc87966af2e73ae9f787f9976ab86b6929449a963dff5b34dfb551eefbab3623`;
- replay summary SHA-256: `45eb7f0581cd6e8667f3c5d02690728d557334c8365925759694ce8eb4c025e6`;
- .NET SDK/runtime: `10.0.301` / `10.0.9`;
- restore: offline local package feed;
- exact historical graph build: 0 warnings, 0 errors.

Command:

```text
campaign --adapter wist-restricted-int32 --seed 1 --cases 3 --repeat 3 --timeout-seconds 60 --include-regressions
```

## Outcome

- requested/completed cases: 3/3;
- confirmed findings: 3;
- fresh-process attempts: 9/9 reproduced their case fingerprint;
- distinct finding classes: 2;
- flaky cases: 0;
- inconclusive cases: 0;
- infrastructure failures: 0.

| Frozen origin | Source | Attempts | Confirmed | Class fingerprint |
|---|---|---:|---:|---|
| `regression-corpus:issue-302` | `(0 * x)` | 3 | yes | `bb233469745ba18fd24fe496005dc033e3e4917cdde2a37b01df9e462c9253b4` |
| `regression-corpus:issue-303` | `((0 * 1) - 1)` | 3 | yes | `bb233469745ba18fd24fe496005dc033e3e4917cdde2a37b01df9e462c9253b4` |
| `regression-corpus:issue-307` | `(x + (-2))` | 3 | yes | `9076590930c54e482e22f69ab2bd799e1cc171ff0f4a755db1123410bdcdbee0` |

Every attempt directory has its own manifest; the campaign-level manifest covers all case files and the summary.

## Claim boundary

This receipt establishes exact pre-fix reproducibility of the three frozen issue-defined regressions. It does **not** establish a historical P2 detection rate: the selected revision predates P2 and no policy backport was executed. The corpus remains author-selected and does not establish independent authorship, natural-defect prevalence, or complete repository-history coverage.
