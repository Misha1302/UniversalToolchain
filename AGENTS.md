# AGENTS

## Project identity

- **UniversalToolchain** is the primary product: a reusable, modular toolchain/framework for building and composing
  language runtimes.
- **Wist** is the reference language and proving ground in this repository, not the only architectural truth.
- Treat Wist-specific code and docs as examples of framework usage unless a file explicitly defines a Wist-only
  contract.

## Non-negotiable priorities

1. Universality first.
2. Preserve existing architectural principles instead of locally optimizing around them.
3. Avoid hardcode.
4. Avoid coupling to concrete implementation details.
5. Avoid coupling to concrete dialects.
6. Avoid coupling to concrete modules when reasonably possible.
7. Prefer designs that make universality erosion hard, visible, and testable.
8. Apply DRY, KISS, SOLID, and OOP.
9. Do not introduce technical debt.
10. Do not introduce legacy behavior/patterns.

## Expected change strategy

Prefer:

- extension points over special cases,
- abstractions over ad hoc wiring,
- composition over branching,
- reusable contracts over one-off shortcuts,
- deterministic behavior over hidden magic,
- minimal targeted changes over broad rewrites,
- data-driven descriptors over smart central authorities,
- optional convenience layers over mandatory framework dependencies,
- designs where narrowing a reusable abstraction requires an explicit structural change rather than a small ad hoc patch.

## Forbidden patterns

Do not introduce:

- hardcoded dialect assumptions,
- hardcoded module assumptions,
- “just for this case” hacks,
- implementation-detail leakage into public contracts,
- copy-paste extensions,
- architecture bypasses,
- preservation of bad legacy behavior without explicit justification,
- silent behavior changes without test updates,
- convenience registries/catalogs/loaders that become hidden decision-makers for framework-level composition,
- framework entities that are easy to expand by adding concrete-profile, concrete-module, or concrete-backend branching.

## Rules for touching code

Before editing:

- read the relevant architecture and module boundaries,
- reuse existing extension points when possible,
- avoid parallel abstractions when one already exists,
- verify whether the change preserves the project's existing universality and layering principles.

While editing:

- avoid new global mutable state,
- keep public API naming/shape consistent,
- keep framework-level abstractions independent from Wist-specific details where reasonable,
- keep convenience layers thin and optional,
- prefer designs where built-in or product-specific entities are data-only when reasonably possible,
- follow `PROJECT_RULES.md` for coding standards.

After editing:

- add or update tests for behavior changes,
- add or update architecture guardrails when a new convenience layer, catalog, registry, or facade is introduced,
- update docs when behavior, contracts, or architecture meaningfully change.

## Documentation policy

- `readme.md` is the canonical repository overview.
- `PROJECT_RULES.md` is the canonical coding standard.
- `CONTRIBUTING.md` is the canonical contribution workflow.
- This file (`AGENTS.md`) is the canonical AI-agent behavior guide.
