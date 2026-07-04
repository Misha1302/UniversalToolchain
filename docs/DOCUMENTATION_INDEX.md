# Documentation priority index

This index helps agents find the right Markdown files before editing code.

It is intentionally lightweight. Ordinary developers do not need to follow a formal process for every small change, but AI agents should use this index to avoid treating code as the only source of truth.

## Always read for architecture-affecting changes

1. `AGENTS.md`
2. `docs/PROJECT_RULES.md`
3. `docs/DOCUMENTATION_RULES.md`
4. `docs/CURRENT_ARCHITECTURE_STATUS.md`

Architecture-affecting changes include parser, runtime, module, capability, function, CLI, CI, public API, and documentation smoke-check changes.

## Architecture boundaries

Read these when the task touches architecture, syntax, ownership, or cleanup:

1. `docs/ARCHITECTURE_RULES.md`
2. `docs/SYNTAX_OWNERSHIP_RULES.md`
3. `docs/architecture/ir-routing-foundation.md` — current generic IR routing contracts and AIR-only compatibility bridge.

## Current user-facing overview

Read these when changing examples, CLI, public behavior, or onboarding docs:

1. `readme.md`
2. `docs/CURRENT_ARCHITECTURE_STATUS.md`

## Historical and future plans

Planning documents are useful context, but they are not automatically current runtime truth.

If a future or historical plan contains executable `bash` blocks for removed or not-yet-implemented functionality, convert those blocks to non-executable `text` sketches instead of weakening Markdown checks.

## Proposed architecture designs

These documents describe possible future architecture and must not be treated as implemented runtime behavior:

1. `docs/architecture/flame-ssa-optimizing-backend-design/index.md` — motivation, constraints, architecture, rollout, validation, and licensing plan for an optional SSA optimizing CIL backend based on Flame.
2. `docs/architecture/typed-module-contracts-and-verifiers.md` — design proposal for typed module descriptors, AST ownership, bytecode/AIR verifiers, diagnostics, tests, and staged rollout.
3. `docs/architecture/ir-routing-foundation.md` — implemented generic IR contracts, AIR CFG/stack verification, minimal SSA model, structural verifier, current minimal AIR-to-SSA/SSA-to-AIR converter boundaries and first verifier-gated SSA optimization boundary.
4. `docs/architecture/callable-first-ssa.md` — implemented callable-first SSA foundation plus current route boundaries and future direction.
5. `docs/releases/ssa-route-correctness-2026-07-04.md` — release note for the no-optimization SSA route correctness pass.

## Conflict rule

If a Markdown rule and existing code disagree, report the conflict. Do not silently implement around the documentation.
