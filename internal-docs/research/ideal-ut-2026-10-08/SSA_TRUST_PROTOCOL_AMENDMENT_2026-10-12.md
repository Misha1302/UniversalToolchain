# Protocol amendment A1: exact-count CI contract

**When:** after the first real GitHub Actions Linux test execution, *before* adjusting the test contract. **Original preregistration remains byte-identical:** [SSA_TRUST_PREREG_2026-10-12.md](SSA_TRUST_PREREG_2026-10-12.md).

## New observed constraint

At test commit `969d2bfd2105d6953a7d357d355f870507157613`, Linux CI [workflow run 38245989179](https://github.com/Misha1302/UniversalToolchain/actions/runs/38245989179) successfully compiled and executed **504/504 Core tests** (504 passed, 0 failed). The canonical test contract then **rejected** the build because `eng/test-counts.json` expected 500. This is the intended fail-closed test-contract guard, not an experiment test failure. The run's canonical build log and TRX are the evidence; a green CI result is not yet reported.

## Limited mutation authorization, without weakening a guard

Extend only the mutation allowlist to **`eng/test-counts.json`**, changing `main.Core.expectedPassed` **500→504** and `totalPassed` **1324→1328**, preserving every other suite count, timeout, filter and schema field. Sum before change: 500 + 292 + 234 + 62 + 185 + 41 + 1+1+1+1+1+1+1+1+2 = 1324. The change represents four observed passing NUnit cases, not weakening/turning off validation.

Reject any broader count change absent independent TRX evidence. If next CI reveals other test-count drift or code failures, inspect and explain before acting. Do not rewrite the original preregistration or test assertions to make results look better.

## Evidence status

- **Observed:** Core project compiled on Linux and all 504 tests passed; CI failed solely at Core exact-count contract.
- **Not yet observed:** complete canonical multi-suite run and Windows run after count update; final aggregate CI success.
- **Unchanged research interpretation:** a characterization PASS does not prove semantic safety; the deliberately effect-losing candidate is still `FAIL_SAFE` within its narrow observation model.
