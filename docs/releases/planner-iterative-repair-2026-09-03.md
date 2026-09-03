# Planner iterative repair — 2026-09-03

This repair hardens executable planning around one rule: semantic constraints define feasible plans; declared preferences rank feasible plans; technical IDs do not silently decide semantics.

## Effective ordering

Executable same-contract passes now use one effective ordering model built from:

- contribution descriptor `Before` / `After`;
- definition `Before` / `After` / `Requires`;
- numeric `Order` only among currently ready contributions.

Definition `Requires(source, target)` retains the existing resolution meaning that `target` precedes `source` in an executable ordered domain. Contradictory ordering is a planning error, not a `LanguagePlanVerifier` exception.

If multiple unrelated ready passes have the same minimum `Order`, planning fails closed. `ContributionId` remains a stable identity and canonical presentation key, but is not an execution-priority policy.

## Route feasibility and ambiguity

Selected same-contract passes are mandatory executable contributions within their backend scope. Selected non-pass transformations are conversion candidates.

Route selection now searches for the minimum-cost conversion route that can place all selected backend-applicable passes. This prevents a locally cheapest conversion skeleton from invalidating an otherwise feasible plan.

If multiple semantically distinct fully feasible routes have the same minimum declared cost and no explicit policy distinguishes them, planning fails closed instead of selecting by lexical `ContributionId`.

## Cost domain and API migration

Per-transformation `ArtifactTransformationDescriptor.Cost` remains `int` and continues to accept the existing public range.

Aggregate route arithmetic is widened to `long`. `LanguageArtifactRoute.TotalCost` therefore changes from `int` to `long`; this source/binary API change is necessary because a valid route can contain multiple legal `int.MaxValue` edges and its mathematical total cannot be represented by `int`.

Migration for callers is mechanical: consume `TotalCost` as `long` and avoid narrowing casts unless the caller has separately proved the value is in `Int32` range.

Lock serialization remains JSON numeric: `totalCost` is emitted from the widened aggregate value. Individual step costs remain unchanged. Plan identity continues to be derived from canonical semantic content; this repair does not introduce floating-point or platform-dependent cost arithmetic.

## Diagnostics introduced by the repair

- `UTL2205`: executable route violates definition-level ordering;
- `UTL2206`: executable route violates descriptor-level ordering that should have been rejected during planning;
- `UTL2207`: unresolved equal-cost fully feasible conversion-route ambiguity;
- `UTL2208`: unresolved equal-`Order` ready-pass ambiguity.

Existing `UTL2202` remains the ordering-cycle diagnostic and `UTL2204` remains the unplaceable selected-pass diagnostic.

## Performance status

No performance claim is made. Tracking mandatory-pass coverage enlarges route-search state. Correctness is the authority for this repair; planner-search performance remains `NEEDS_MEASUREMENT` before any optimization or caching claim.
