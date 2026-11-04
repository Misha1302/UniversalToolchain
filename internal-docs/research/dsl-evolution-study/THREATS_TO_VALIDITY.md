# Threats to validity and adversarial review

## Baseline quality

The strongest cheap attack on the result was that clone-and-own was artificially denied obvious sharing. The baseline was therefore allowed one shared percent-validation helper. Result: E3 propagation became identical to UT (one site / one file / seven changed LOC). This materially weakens H2 and is retained as the result.

A still stronger baseline could introduce reusable operation objects or a registry. That would likely reduce duplication further, but at some point it becomes a small feature-composition platform. This study does not claim a sharp architectural boundary between those designs.

## Free infrastructure

UT uses already-existing package, compiler, plan, and runtime infrastructure. Marginal feature cost is meaningful for a current UT user, but platform-inclusive historical engineering cost is unknown. The 76-SLOC ceremony/wiring measurement exposes immediate usage cost but is not a substitute for platform-development cost.

## Workload selection

RQ/hypotheses/workload/oracle were committed as `3b78658f` before treatments and measurements. No post-result feature was added. E4 was omitted rather than introduced after seeing the primary results.

## Semantic parity

Both full treatments use the same `cases.json` oracle and pass the same base, discount, surcharge, combined-order, and invalid-percentage cases. Treatment-specific code is not used as the acceptance oracle.

## Measurement construction

Generated files, docs, tests, and framework sources are excluded from production SLOC. The clone approximation uses the same deterministic normalizer for both treatments. Its ratio is sensitive to treatment size, so absolute duplicated-line counts are reported alongside ratios.

## Repository validity

The experiment builds on .NET 10.0.111. The Acme generic authoring sample produced `35.0:35.0`; `UniversalToolchain.LanguageSdk.Tests` passed 185/185 and `UniversalToolchain.LanguageSdk.Generic.Tests` passed 62/62 in the experiment worktree.

## External confirmation-bias check

Krüger & Berger (ESEC/FSE 2020, DOI `10.1145/3368089.3409684`) explicitly find platform-oriented reuse has higher upfront cost and can even have more expensive change propagation. Bertolotti et al. (JSS 2023, DOI `10.1016/j.jss.2023.111704`) support explicit linguistic reuse against clone-and-own at studied reuse granularities, but do not imply that every platform wins every workload. The mixed result here is consistent with treating both as competing cost structures rather than a predetermined UT victory.

## Reproduction repair

The first detached-worktree reproduction matched all JSON artifacts but changed only `raw.csv` line endings. This was treated as a reproducibility failure, not ignored. The CSV writer was made explicitly LF-stable in `2211af5e`; the detached reproduction was rerun and all checked evidence hashes then matched with a clean worktree.
