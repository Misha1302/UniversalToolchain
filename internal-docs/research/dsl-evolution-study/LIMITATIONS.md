# Limitations

1. The current generic SDK does not provide high-level generic grammar/binder/type-system authoring. This experiment composes typed semantic passes after a minimal shared parser; it does not evaluate arbitrary grammar composition.
2. The subject is a micro-language with two percentage operations. It is evidence about this controlled evolution path, not about multi-team production DSLs or bioinformatics.
3. Existing UniversalToolchain infrastructure is treated as available platform infrastructure for marginal-cost measurements. The historical cost of designing/building that platform is not measured.
4. The clone baseline is deliberately allowed a treatment-local shared percent utility. This makes it stronger and removes an E3 propagation advantage that a naive fork baseline would have shown.
5. The duplication detector is a deterministic normalized 3-line-window approximation. It is reproducible but not a semantic clone detector.
6. E1/E2 were implemented as treatment additions rather than one independently runnable commit per evolution step. M1/M2 therefore are reported mechanically for the isolated E3 commit; feature slices are used for E1/E2 instead of inventing historical churn.
7. E4 downstream/shared-IR control was optional and was not added after measurement scripts were frozen. RQ3 remains a control claim to test in a future cycle.
8. The initial scaffold temporarily contained the final percent rule before measurement. Commit `7994fcac` explicitly reset all treatments to pre-E3 semantics, and `11ea672e` then applies the frozen E3 change. The frozen hypotheses/spec commit `3b78658f` precedes both; history was not rewritten.
