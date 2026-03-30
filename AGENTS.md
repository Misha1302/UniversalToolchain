# AGENTS

## Project identity
- **UniversalToolchain** is the primary product: a reusable, modular toolchain/framework for building and composing language runtimes.
- **Wist** is the reference language and proving ground in this repository, not the only architectural truth.
- Treat Wist-specific code and docs as examples of framework usage unless a file explicitly defines a Wist-only contract.

## Non-negotiable priorities
1. Universality first.
2. Avoid hardcode.
3. Avoid coupling to concrete implementation details.
4. Avoid coupling to concrete dialects.
5. Avoid coupling to concrete modules when reasonably possible.
6. Apply DRY, KISS, SOLID, and OOP.
7. Do not introduce technical debt.
8. Do not introduce legacy behavior/patterns.

## Expected change strategy
Prefer:
- extension points over special cases,
- abstractions over ad hoc wiring,
- composition over branching,
- reusable contracts over one-off shortcuts,
- deterministic behavior over hidden magic,
- minimal targeted changes over broad rewrites.

## Forbidden patterns
Do not introduce:
- hardcoded dialect assumptions,
- hardcoded module assumptions,
- “just for this case” hacks,
- implementation-detail leakage into public contracts,
- copy-paste extensions,
- architecture bypasses,
- preservation of bad legacy behavior without explicit justification,
- silent behavior changes without test updates.

## Rules for touching code
Before editing:
- read the relevant architecture and module boundaries,
- reuse existing extension points when possible,
- avoid parallel abstractions when one already exists.

While editing:
- avoid new global mutable state,
- keep public API naming/shape consistent,
- keep framework-level abstractions independent from Wist-specific details where reasonable,
- follow `PROJECT_RULES.md` for coding standards.

After editing:
- add or update tests for behavior changes,
- update docs when behavior, contracts, or architecture meaningfully change.

## Documentation policy
- `readme.md` is the canonical repository overview.
- `PROJECT_RULES.md` is the canonical coding standard.
- `CONTRIBUTING.md` is the canonical contribution workflow.
- This file (`AGENTS.md`) is the canonical AI-agent behavior guide.
