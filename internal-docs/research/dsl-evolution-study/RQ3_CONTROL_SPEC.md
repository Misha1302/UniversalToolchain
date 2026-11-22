# RQ3 downstream/shared-pipeline control (post-freeze addendum)

Status: frozen before RQ3 treatment implementation.
Parent experiment baseline: `ebeae4a3aaadcaa89d2b36e9c5d5559e0773af58`.

This is an isolated post-freeze control. It MUST NOT alter or be merged into the already-frozen RQ1/RQ2 measurements, oracle, or claims.

## Question

Can one feature-independent downstream compiler improvement be implemented once for all source variants both in a strong ordinary shared-pipeline baseline and in UniversalToolchain composition?

## Strong alternative

The baseline is deliberately **not** clone-and-own. It is a small ordinary C# pipeline with one shared `PricingProgram` representation and variant-specific semantic handlers. This gives the non-UT alternative the same legitimate shared-representation advantage relevant to RQ3.

## Frozen workload

The source variants are base, discount, surcharge, and discount+surcharge. Before the downstream change, both treatments produce the expected decimal result while retaining consumed operations in the terminal `PricingProgram` artifact.

The downstream change is one semantics-preserving canonicalization: after all selected source-language operations have been evaluated, clear the consumed operation list before the backend reads `Value`.

Acceptance after the change:

- all expected decimal values are unchanged;
- terminal `PricingProgram.Operations.Count == 0` for every case;
- no variant-specific semantic implementation is edited by the downstream-change commit.

## Treatments

- `shared-pipeline`: ordinary C# shared parser/artifact/pipeline plus selected semantic handlers; no UT planner/runtime.
- `universal-toolchain`: existing public Language Authoring SDK with feature passes, one immutable plan/runtime, and the downstream canonicalization as one planned pass.

## Metrics

For the isolated downstream-change commit, record per treatment:

1. changed existing LOC;
2. touched existing files;
3. downstream propagation sites;
4. new downstream implementation LOC;
5. variant-specific semantic files/regions touched.

These are experiment-specific operational metrics.

## Interpretation lock

If both treatments require one shared downstream site and zero variant-specific edits, RQ3 supports only this claim:

> Shared downstream representation/pipeline reuse is real, but it is not evidence that source-language extensibility is necessary or uniquely responsible for that reuse.

Any extra UT descriptor/planning code remains platform ceremony, not a shared-IR advantage.
