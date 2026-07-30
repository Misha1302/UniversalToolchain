# Review findings remediation ledger

Status: implementation and evidence regeneration in progress on `fix/review-ten-findings-20260730`.

This ledger binds the ten requested review outcomes to code, tests, evidence and publication artifacts. It is intentionally not a release claim until the pull request and post-merge master aggregate complete.

| # | Observable result | Validation |
|---:|---|---|
| 1 | Contract-annotated Bytecode without exactly one producer/source identity fails closed | metadata reader and strict observer regression tests |
| 2 | Runtime session construction preserves the primary exception when cleanup also fails | lifecycle regression with primary-first aggregate |
| 3 | Repeated module/pass identities are not silently collapsed | pipeline verifier rejects ambiguous duplicate occurrences |
| 4 | External language packages can contribute compiler-fact verifier routes | provider aggregation and custom-route regression |
| 5 | Flowed child execution contexts do not retain completed operation leases | child-task disposal regression |
| 6 | Technical article states the exact Bytecode ownership boundary | article diff and rendered PDF inspection |
| 7 | Article contains a compact evidence-identity table | article diff and rendered PDF inspection |
| 8 | A narrow conference draft isolates contract-guided reverification | separate paper draft and PDF |
| 9 | Package compatibility is run only with reviewed previous source/package identities | canonical full build evidence or explicit blocker |
| 10 | Post-freeze review holdouts are evaluated separately from the original author-designed corpus | holdout protocol, raw results, and bounded claims |

The package surfaces use monotonic remediation versions: the seven SDK/template packages are `0.3.0-alpha.3`, `UniversalToolchain.Wist.LanguagePack` is `0.3.0-alpha.4`, and `UniversalToolchain.Wist` is `0.1.0-alpha.5`. These versions remain candidates until the baseline-bearing package workflow succeeds.

The original 32-operator primary set and 10-operator challenge set remain immutable. Review-derived holdouts must never be merged into those denominators retroactively.
