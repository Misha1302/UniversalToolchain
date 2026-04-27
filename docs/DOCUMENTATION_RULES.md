# Documentation rules

Markdown files in this repository are architectural source material, not optional notes.

These rules are written for AI agents first. They should not slow down ordinary developers: they define how to handle documentation while making code changes, but they do not add a new approval process.

## Core rule

Documentation must be preserved and synchronized with implementation.

If code and Markdown disagree, treat it as architecture drift. Do not silently choose the code just because it currently compiles.

## Required behavior for agents

Before non-trivial code changes, identify the Markdown files that govern the task and the constraints they impose.

For small typo fixes or mechanical formatting-only changes, this can be a short mental check. For architecture, parser, runtime, feature, module, CLI, CI, or public API changes, it must be explicit in the PR summary or final report.

## Forbidden fixes

Do not fix CI or tests by:

- deleting architectural documentation;
- replacing a large document with a placeholder;
- adding path-based Markdown exclusions to smoke checks;
- weakening documentation checks instead of fixing stale examples;
- restoring removed runtime functionality only because old documentation references it;
- implementing behavior that contradicts current architecture docs.

## Handling stale executable examples

When a Markdown `bash` block is stale, choose the narrowest honest fix:

1. If the command describes currently supported behavior, update the command.
2. If the command is a future or historical sketch, convert the fence from `bash` to `text` and say it is not executable in the current state.
3. If the surrounding document is obsolete, move or rewrite only the obsolete section. Do not destroy unrelated architectural context.

## Current vs future docs

Current-state documents describe what the repository supports now.

Future or historical documents may describe planned or removed behavior, but they must not contain executable `bash` blocks for commands that do not exist in the current branch.

## Principle

A failing documentation check means documentation and implementation drifted. It does not mean the check should be weakened.
