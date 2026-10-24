# Frozen experiment specification

## Research questions

- **RQ1:** Under one fixed evolution workload, does explicit UT feature composition reduce duplication/change propagation versus a fair clone-and-own baseline?
- **RQ2:** What is the marginal upfront cost of UT composition versus the smallest first fixed implementation?
- **RQ3:** Does a shared downstream transformation benefit all variants independently of source-language composition strategy?

## Subject and boundary

A small pricing-adjustment language family is used because the existing `Acme.PricingLanguage` sample proves the generic SDK route and the current architecture explicitly lacks high-level generic grammar/binder authoring. The study therefore evaluates reuse/composition of **typed linguistic semantic passes after a minimal parser**, not arbitrary independent grammar composition.

Observable syntax is intentionally tiny: `price <decimal>` followed by zero or more pipe-separated operations. E1 introduces `discount <percent>`, E2 adds the sibling variant `surcharge <percent>`, and E3 changes the shared percentage rule to clamp percentages to `[0,100]` with identical diagnostics/behavior in all variants.

## Frozen evolution workload

- **E0 — Base:** `price 100` evaluates to `100`.
- **E1 — Discount:** `price 100 | discount 10` evaluates to `90`.
- **E2 — Second variant:** `price 100 | surcharge 10` evaluates to `110`; a composed variant supports both in order.
- **E3 — Shared change:** percent operands outside `[0,100]` are rejected by the same semantic rule for all percent-based operations.
- **E4 — downstream control (only if cheap):** shared rounding/canonicalization after semantic operations; source variants must not change.

## Treatments

- **A Clone-and-own:** independent fixed variant implementations may use obvious shared non-linguistic utilities, but duplicated operation semantics remain owned by each cloned variant where that is the natural copy/adapt path.
- **B UT composition:** core parser plus independently selected UT features/passes using current `LanguagePackageBuilder`/`LanguageCompiler`/`LanguagePlan`/`LanguageRuntime`; no UT-core modification is allowed merely for the benchmark.
- **C First-case control:** smallest E0/E1 fixed implementation without package/planner/runtime ceremony.

## Oracle

Treatment-neutral fixture file under `experiments/dsl-evolution-study/spec/cases.json`. Both runnable treatments must produce the same expected result or expected error class for the same case IDs.

## Primary operational metrics

M1 changed existing implementation LOC; M2 touched existing files; M3 duplicated linguistic implementation via deterministic normalized-block matching; M4 propagation sites for E3; M5 new feature LOC; M6 UT-only ceremony/platform LOC. Derived CAR, FD, PF and duplication ratio are experiment-specific operational metrics, not general maintainability standards.

## Fairness / exclusions

Generated files, docs and tests are excluded from production LOC. Test LOC is reported separately. The same scanner/exclusions are used for both treatments. Existing UT framework code is not counted as marginal feature cost; the report must separately state that the study does not measure historical platform development cost. A reasonable baseline improvement is permitted if it does not effectively turn the baseline into the same composition platform.
