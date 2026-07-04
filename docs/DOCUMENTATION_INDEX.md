# Documentation priority index

This index defines the authority order for Markdown files before code changes.

Ordinary developers do not need a formal process for every small change. Agents
and architecture-affecting changes must use this file to avoid treating stale
plans as current truth.

## Authority levels

| Level | Location | Meaning |
|---|---|---|
| Current truth | `docs/CURRENT_ARCHITECTURE_STATUS.md`, `docs/ARCHITECTURE_RULES.md`, `docs/PROJECT_RULES.md`, `docs/DOCUMENTATION_RULES.md` | May govern implementation and review. |
| Current implementation detail | `docs/architecture/ir-routing-foundation.md`, `docs/architecture/callable-first-ssa.md`, `docs/architecture/debug-trace-v2.md`, `docs/releases/ssa-route-correctness-2026-07-04.md` | Current for the named subsystem only. |
| Public docs | `docs/start/`, `docs/wist/`, `docs/build-dsls/`, `docs/write-modules/`, `docs/reference/`, `docs/internals/` | Must match current behavior before release. |
| Proposals | `docs/proposals/` | Design input only until implemented and tested. |
| Archive | `docs/archive/` | Historical context only. |

## Always read for architecture-affecting changes

1. `AGENTS.md`
2. `docs/PROJECT_RULES.md`
3. `docs/DOCUMENTATION_RULES.md`
4. `docs/CURRENT_ARCHITECTURE_STATUS.md`
5. `docs/ARCHITECTURE_RULES.md`

Architecture-affecting changes include parser, runtime, module, capability,
function, CLI, CI, public API, optimizer, backend, and documentation smoke-check
changes.

## Architecture boundaries

Read these when the task touches architecture, syntax, ownership, IR routing, or
cleanup:

1. `docs/SYNTAX_OWNERSHIP_RULES.md`
2. `docs/architecture/ir-routing-foundation.md`
3. `docs/architecture/callable-first-ssa.md`

## Current user-facing overview

Read these when changing examples, CLI, public behavior, or onboarding docs:

1. `readme.md`
2. `docs/index.md`
3. `docs/CURRENT_ARCHITECTURE_STATUS.md`

## Proposals and archive

Planning documents are useful context, but they are not automatically current
runtime truth.

Proposal documents now live under `docs/proposals/`. Historical documents now
live under `docs/archive/`.

If a future or historical plan contains executable `bash` blocks for removed or
not-yet-implemented functionality, convert those blocks to non-executable `text`
sketches instead of weakening Markdown checks.

## Conflict rule

If a Markdown rule and existing code disagree, report the conflict. Do not
silently implement around the documentation.
