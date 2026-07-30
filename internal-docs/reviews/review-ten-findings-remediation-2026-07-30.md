# Review findings remediation ledger

Status: all ten requested findings are complete at PR level on `fix/review-ten-findings-20260730`; post-merge master aggregate remains the final repository authority.

This ledger binds the requested outcomes to code, tests, evidence and publication artifacts. Candidate package versions are validated but are not claimed as published to NuGet.org.

| # | Observable result | Validation | Status |
|---:|---|---|---|
| 1 | Contract-annotated Bytecode without exactly one producer/source identity fails closed | metadata reader, strict observer tests and review holdout | complete |
| 2 | Runtime session construction preserves the primary exception when cleanup also fails | primary-first aggregate lifecycle regression | complete |
| 3 | Repeated module/pass identities are not silently collapsed | duplicate occurrence diagnostic and regression | complete |
| 4 | External language packages can contribute compiler-fact verifier routes | provider aggregation, conflict handling and custom-route regression | complete |
| 5 | Flowed child execution contexts do not retain completed operation leases | child-task disposal regression | complete |
| 6 | Technical article states the exact Bytecode ownership boundary | remediated Markdown/PDF and rendered-page inspection | complete |
| 7 | Article contains a compact evidence-identity table | front-matter table with archive, commits and workflow artifacts | complete |
| 8 | A narrow conference draft isolates contract-guided reverification | focused eight-page Markdown/PDF draft with related mechanisms and threats | complete |
| 9 | Package compatibility uses reviewed previous source/package identities | workflow `30580457427`, artifact `8774539955`, 9/9 packages, API delta 0/0, consumers/templates/integrity pass | complete |
| 10 | Post-freeze review holdouts are separate from the original corpus | workflow `30580457769`, artifact `8774419595`, B0/B1/B2 0/4, 4/4, 4/4 and 0/20 controls | complete |

The exact PR-level regression authority is `.NET CI` run `30580457345`: `TEST-CONTRACT COMPLETE passed=1551 entries=14`. Validation, Docs Check, published-package smoke, rollout smoke, benchmark smoke, Contract Experiment and Package Compatibility Review also completed successfully for the reviewed code head.

The package surfaces use monotonic remediation versions: the seven SDK/template packages are `0.3.0-alpha.3`, `UniversalToolchain.Wist.LanguagePack` is `0.3.0-alpha.4`, and `UniversalToolchain.Wist` is `0.1.0-alpha.5`.

The original 32-operator primary set and 10-operator challenge set remain immutable. Review-derived holdouts are not merged into those denominators and are not described as independently authored or statistically representative.
