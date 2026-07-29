---
title: Documentation Rules
description: Summarize documentation synchronization and executable example rules.
---

# Documentation Rules

Markdown files in this repository are architectural source material, not optional notes.

This page is the canonical public summary. The repository-only agent/maintainer policy is `internal-docs/policies-and-reports/DOCUMENTATION_RULES.md`.

## Core rule

Documentation must be preserved and synchronized with implementation.

If code and Markdown disagree, treat it as architecture drift. Do not silently choose code just because it currently compiles, and do not silently choose stale documentation just because it sounds like the intended design.

## Required behavior

Before non-trivial changes, identify the Markdown files that govern the task and the constraints they impose.

This matters for changes touching:

- parser behavior;
- runtime composition;
- modules;
- capabilities;
- function calls;
- CLI behavior;
- CI behavior;
- public API;
- documentation smoke checks.

## Forbidden fixes

Do not fix CI or tests by:

- deleting architectural documentation;
- replacing substantial documents with placeholders;
- adding path-based Markdown exclusions to smoke checks;
- weakening documentation checks instead of fixing stale examples;
- restoring removed runtime functionality only because old documentation references it;
- implementing behavior that contradicts current architecture docs without explicitly resolving the conflict.

## Executable examples

When a Markdown `bash` block is stale, choose the narrowest honest fix:

1. If the command describes supported behavior, update the command.
2. If the command is future or historical, convert the fence to `text` and say it is not executable in the current state.
3. If the surrounding document is obsolete, rewrite only the obsolete section.

The Markdown bash smoke runner executes tracked Markdown `bash` fences unless the block is explicitly marked with supported CI attributes.

## Current versus future docs

Current-state documents describe what the repository supports now.

Future or historical documents may describe planned or removed behavior, but they must not contain executable `bash` blocks for commands that do not exist in the current branch.

Use `internal-docs/proposals/` for future designs. Preserve historical context in Git history or a detached history/evidence bundle rather than the product source tree. Neither proposals nor historical bundles are current runtime truth unless a current-state document and tests explicitly promote the behavior.

## Documentation authority and fitness

Substantial architecture, rules, proposal, release, review, or archive documents
must state whether they are current truth, a proposal, archive context, a dated
review, or a public guide.

Architecture rules should be paired with a practical fitness check before they
are treated as enforced release gates.

## Practical validation

For documentation changes that affect public examples, run:

```text
npm run docs:status
python3 .github/scripts/run-markdown-bash-blocks.py
npm run docs:build
```

For code or behavior changes, also run the relevant .NET build and test commands from [Installation](/start/installation).

## Related pages

- [Project Rules](/reference/project-rules)
- [Module Contracts](/reference/module-contracts)
- [Testing a DSL](/build-dsls/testing-dsl)
