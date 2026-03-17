# Final Branch Review

## What was fixed
- Removed duplicated order-constraint mapping by consolidating both syntax-rule and compiled-directive conversion in `DialectOrderConstraintMapper`.
- Ensured `DialectCompiledDialectBuildPlanBuilder` no longer carries a redundant private mapper implementation.

## What was refactored
- Kept frontend wiring explicit in `DialectDslFrontendModule` and documented why `ProcessAst` is intentionally a no-op.
- Added collaborator-focused test coverage for order-directive mapping in build-plan collaborators.

## Architectural risks reduced
- Lowered drift risk between syntax parsing and compiled-plan projection by reusing a single mapping implementation.
- Reduced accidental divergence in future edits by removing duplicate conversion paths.

## Remaining future work
- Consider adding explicit guardrails around unknown order-directive values once the DSL schema evolves.
- Expand end-to-end dialect fixture coverage to include malformed directive diagnostics.

## Merge-readiness verdict
**Ready with minor follow-ups.**

### Follow-up checklist (non-blocking)
- Add one integration test that validates unknown order directives surface a clear diagnostic.
- Revisit whether no-op `ProcessAst` should emit trace-level logging in debug builds.
