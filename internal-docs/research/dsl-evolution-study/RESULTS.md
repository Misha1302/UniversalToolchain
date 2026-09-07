# Results

Baseline: `40117eb68c630f7129c120aaaadc69be8f4ecbfb`. Measurement commit: `7baade19a6f263d0bc941df5439fa7ff9415cb7c`.

All treatment-neutral oracle cases passed for clone-and-own and UniversalToolchain composition. The first-case control passed its applicable base/discount cases. The experiment build completed with 0 warnings and 0 errors.

## Main observations

| Observation | First case / E1 | E2 second variant | E3 shared percent fix |
|---|---:|---:|---:|
| Control | 16 SLOC fixed evaluator | n/a | 1 site, 1 file, 7 changed LOC |
| Clone-and-own | 16 E1 feature SLOC | 35 E2 feature SLOC | 1 site, 1 file, 7 changed LOC |
| UT composition | 19 E1 feature SLOC | 19 E2 feature SLOC | 1 site, 1 file, 7 changed LOC |

Whole-treatment handwritten production footprint was 26 SLOC for control, 61 for clone-and-own, and 105 for UT. In UT, the operational split classified 29 SLOC as semantic implementation and 76 SLOC as package/planning/runtime ceremony or platform-facing wiring.

The deterministic normalized 3-line clone approximation marked 18 duplicated eligible lines in clone-and-own and 18 in UT. Because UT has a larger denominator, the repeated-line ratio was 0.4865 for clone-and-own versus 0.2222 for UT. This is a ratio result, **not** evidence that UT had fewer duplicated lines in absolute terms on this workload.

## Hypotheses

- **H1: partial / mixed support.** The duplication ratio was lower for UT, and E2 marginal feature LOC was lower (19 vs 35), but absolute duplicated-line count was equal (18 vs 18) and total UT footprint remained larger.
- **H2: not supported.** A fair clone baseline centralized the percent validity rule; E3 therefore required one logical site in both clone-and-own and UT.
- **H3: supported for this micro-workload.** The fixed first-case evaluator was 16 SLOC and avoided the UT package/planner/runtime ceremony. UT's full treatment carried 76 SLOC classified as ceremony/platform-facing wiring.
- **H4 / RQ3: not experimentally exercised.** E4 was optional and was not added after measurement freeze. Existing repository architecture supports shared downstream routes, but this study does not turn that implementation fact into experimental evidence for source-language extensibility.
- **H0: not rejected globally.** The result is mixed and too small to establish a universal break-even point.

## Crossover interpretation

The first fixed language is clearly cheaper in the simple control. At the second overlapping variant, UT has a lower measured marginal feature slice (19 vs 35 SLOC), but its cumulative implementation footprint is still higher (105 vs 61 SLOC). No measured propagation advantage appeared at E3 after strengthening the baseline. Therefore this experiment observes a **marginal-cost crossover on E2, not a total-cost crossover** within two variants.
