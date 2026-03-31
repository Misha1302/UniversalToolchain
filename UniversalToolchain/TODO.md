# TODO

## Active architectural debt

- Replace reflection-heavy service/module discovery with a more deterministic composition model.
- Reduce abstraction leakage in `BasicCoreImpl` and clarify stage boundaries/extension contracts.
- Introduce stronger intrinsic governance (central registry/contracts) to prevent invalid intrinsic generation.
- Improve compiler/interpreter behavior parity and explicitly document supported divergences.
- Reduce global mutable state risks that can affect repeated runs and long-lived hosts.

## In-progress / partially addressed areas

- Dialect subsystem exists (parsing/core/integration/frontend/wist projects), but composition ergonomics and policy
  depth continue to evolve.
- `ParametersSetter` module contracts (parser references + runtime binding semantics) are still pending; module remains
  intentionally non-exported and excluded from active coverage.
- Constrained runtime profiles exist through dialect examples, but security hardening is still incomplete.
- Module grouping/dependency-order concepts are only partially represented and need first-class contracts.
- Test coverage exists for both core and dialect paths, but additional structure and grouping improvements are still
  useful.

## Documentation and repository hygiene

- Keep docs synchronized with real CLI verbs/options and dialect paths.
- Keep examples runnable from repository root and avoid stale snapshot wording.
- Continue removing obsolete generated artifacts from source control and document what should remain ignored.
- Expand architecture docs where needed to keep deterministic behavior and trust model explicit.

## Future research / long-term ideas

- CIL optimizer roadmap (SSA-oriented passes, inlining strategy, and backend tuning).
- Broader frontend/parser strategy experiments (alternative parsing algorithms and extensibility models).
- Configuration format modernization where it improves determinism and tooling.
- Optional code generation for repetitive boilerplate where it preserves readability and correctness.
