# Documentation priority index

This index helps agents find the right Markdown files before editing code.

It is intentionally lightweight. Ordinary developers do not need to follow a formal process for every small change, but AI agents should use this index to avoid treating code as the only source of truth.

## Always read for architecture-affecting changes

1. `AGENTS.md`
2. `docs/PROJECT_RULES.md`
3. `docs/DOCUMENTATION_RULES.md`
4. `docs/CURRENT_ARCHITECTURE_STATUS.md`

Architecture-affecting changes include parser, runtime, module, capability, function, rules, CLI, CI, public API, and documentation smoke-check changes.

## Architecture boundaries

Read these when the task touches architecture, syntax, ownership, or cleanup:

1. `docs/ARCHITECTURE_RULES.md`
2. `docs/SYNTAX_OWNERSHIP_RULES.md`
3. `docs/wist-rule-system-clean-design.md`

## Current user-facing overview

Read these when changing examples, CLI, public behavior, or onboarding docs:

1. `readme.md`
2. `docs/CURRENT_ARCHITECTURE_STATUS.md`

## Historical and future plans

Planning documents are useful context, but they are not automatically current runtime truth.

If a future or historical plan contains executable `bash` blocks for removed or not-yet-implemented functionality, convert those blocks to non-executable `text` sketches instead of weakening Markdown checks.

Examples:

- `docs/series-01-to-07-final-task.md`
- feature-system and strong-MVP planning documents, if present in the branch

## Conflict rule

If a Markdown rule and existing code disagree, report the conflict. Do not silently implement around the documentation.
