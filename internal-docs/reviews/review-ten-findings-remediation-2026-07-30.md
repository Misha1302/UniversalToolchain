# Review findings remediation ledger

Status: all ten requested findings are complete on master commit `2b0a4d1f0e255432daf0d5ddd485269b6490b67e`; aggregate run `30585251873` completed successfully.

This ledger binds the requested outcomes to code, tests, evidence and publication artifacts. Candidate package versions are validated but are not claimed as published to NuGet.org.

| # | Observable result | Validation | Status |
|---:|---|---|---|
| 1 | Contract-annotated Bytecode without exactly one producer/source identity fails closed | metadata reader, strict observer tests and review holdout | complete |
| 2 | Runtime session construction preserves the primary exception when cleanup also fails | primary-first aggregate lifecycle regression | complete |
| 3 | Repeated module/pass identities are not silently collapsed | duplicate occurrence diagnostic and regression | complete |
| 4 | External language packages can contribute compiler-fact verifier routes | provider aggregation, conflict handling and custom-route regression | complete |
| 5 | Flowed child execution contexts do not retain completed operation leases | child-task disposal regression | complete |
| 6 | Technical article states the exact Bytecode ownership boundary | remediated Markdown/PDF and rendered-page inspection | complete |
| 7 | Article contains a compact evidence-identity table | archive, baselines, master commit, runs and artifact digests are separated | complete |
| 8 | A narrow conference draft isolates contract-guided reverification | focused Markdown/PDF draft with related mechanisms and threats | complete |
| 9 | Package compatibility uses reviewed previous source/package identities | run `30580457427`, artifact `8774539955`, 9/9 packages, API delta 0/0, consumers/templates/integrity pass; later changes were documentation-only | complete |
| 10 | Post-freeze review holdouts are separate from the original corpus | master run `30585251945`, artifact `8776245456`, B0/B1/B2 0/4, 4/4, 4/4 and 0/20 controls | complete |

Master `.NET CI` run `30585251901` recorded `TEST-CONTRACT COMPLETE passed=1551 entries=14`. Canonical-build artifact ID `8776313506` has digest `sha256:1a7875efad0bc1fc230f61bd9b4d578e7d626c0230905ecbb193846c742f9e30`.

Master Contract Experiment artifact ID `8776245456` has digest `sha256:ca1708b8054e63eb9fff0526f9113013a9569121890ff3b0ea19572e5c199961`. Both the main-study and review-holdout checksum trees verify after extraction, and both captured git-status files are empty.

The package surfaces use monotonic remediation versions: the seven SDK/template packages are `0.3.0-alpha.3`, `UniversalToolchain.Wist.LanguagePack` is `0.3.0-alpha.4`, and `UniversalToolchain.Wist` is `0.1.0-alpha.5`.

The original 32-operator primary set and 10-operator challenge set remain immutable. Review-derived holdouts are not merged into those denominators and are not described as independently authored or statistically representative.
