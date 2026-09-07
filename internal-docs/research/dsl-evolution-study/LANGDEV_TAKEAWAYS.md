# LangDev takeaways

## One-sentence result

In this controlled two-variant semantic-composition workload, UT made the second variant smaller to add (19 vs 35 feature SLOC) and lowered the normalized duplication ratio, but still had a larger total footprint and showed **no** E3 propagation advantage over a fair clone baseline with one shared utility.

## One table

| Strategy | First case / E1 | Variant 2 | Total treatment | Shared fix E3 |
|---|---:|---:|---:|---:|
| Control | 16 SLOC | — | 26 SLOC | 1 site |
| Clone-and-own | 16 feature SLOC | 35 feature SLOC | 61 SLOC | 1 site |
| UT composition | 19 feature SLOC | 19 feature SLOC | 105 SLOC | 1 site |

Additional measured context: normalized repeated-line ratio was 0.4865 for clone-and-own and 0.2222 for UT, while both had 18 duplicated lines under the deterministic detector. UT's operational ceremony/platform-facing slice was 76 SLOC.

## Figures

- `experiments/dsl-evolution-study/results/figures/implementation-footprint.svg`
- `experiments/dsl-evolution-study/results/figures/duplication-ratio.svg`

## Speaker-safe claims

1. The first fixed case was cheaper without the UT composition machinery in this experiment.
2. By the second overlapping variant, UT had lower **marginal feature LOC**, although cumulative UT code was still larger.
3. A strengthened clone baseline erased the expected shared-fix propagation advantage: both treatments changed one logical site at E3.
4. The experiment therefore supports a conditional story: repeated variability can improve the marginal economics of explicit composition before it necessarily repays the platform ceremony in total footprint.
5. The measured boundary is semantic pass reuse, not arbitrary grammar composition.

## Claims we explicitly do NOT make

- UT makes DSL development N-times cheaper.
- Two variants establish a universal break-even point.
- UT beats a good monolith/shared-IR design on downstream compiler reuse.
- This micro-language result generalizes automatically to bioinformatics or multi-team production systems.
- `LanguagePlan` proves semantic compatibility between independently authored features.
