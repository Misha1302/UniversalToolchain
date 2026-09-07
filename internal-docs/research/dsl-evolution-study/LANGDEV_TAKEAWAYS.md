# LangDev takeaways

## One-sentence result

In this controlled two-variant workload, UT made the second overlapping variant smaller to add (19 vs 35 feature SLOC) but retained a larger total footprint and no E3 propagation advantage; a separate post-freeze RQ3 control found that downstream reuse was one shared site in both an ordinary shared pipeline and UT, so shared compiler reuse is not evidence unique to source-language extensibility.

## One table

| Strategy | First case / E1 | Variant 2 | Total treatment | Shared fix E3 | Downstream RQ3 |
|---|---:|---:|---:|---:|---:|
| Control | 16 SLOC | — | 26 SLOC | 1 site | — |
| Clone-and-own | 16 feature SLOC | 35 feature SLOC | 61 SLOC | 1 site | — |
| Ordinary shared pipeline (RQ3 only) | — | — | — | — | 1 site / 3 LOC |
| UT composition | 19 feature SLOC | 19 feature SLOC | 105 SLOC | 1 site | 1 site / 5 LOC |

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
6. A separate shared-pipeline control shows that downstream reuse is not unique to UT: both ordinary shared-pipeline and UT treatments needed one downstream site and zero variant-semantic edits.

## Claims we explicitly do NOT make

- UT makes DSL development N-times cheaper.
- Two variants establish a universal break-even point.
- UT beats a good monolith/shared-IR design on downstream compiler reuse.
- This micro-language result generalizes automatically to bioinformatics or multi-team production systems.
- `LanguagePlan` proves semantic compatibility between independently authored features.
