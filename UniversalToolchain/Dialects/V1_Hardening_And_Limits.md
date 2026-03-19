# Dialect DSL v1 limits and next steps

## Honest v1 status

Implemented now:

- deterministic parsing/compilation path for the DSL
- semantic normalization + build-plan diagnostics
- explicit runtime descriptor resolution
- explicit apply-mode description seam (non-activating)

Not implemented yet:

- full runtime activation/wiring policy
- automated descriptor catalog ownership and governance
- cross-file dialect package model
- signed/trusted dialect distribution policy

## Known supportability constraints

- descriptor registration is still explicit/manual for most flows
- compatibility checks are descriptor-based, not full backend inventory verification
- security profile semantics are modeled but not yet enforced during host activation

## Recommended near-term steps

1. Add host-level apply executor that consumes `DialectApplyDescription` with explicit lifecycle rules.
2. Add machine-readable inspect/apply reports for CI automation.
3. Expand policy tests around backend/optimizer/intrinsic compatibility constraints.
4. Define ownership model for descriptor catalogs and intrinsic allowlists.

## Recommended contributor practice

When adding features, keep the stage boundaries intact and document the change in the nearest subsystem note before
extending integration behavior.
