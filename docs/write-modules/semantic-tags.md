---
title: Semantic Tags
description: Explain why bytecode tags exist and how they support later stages.
---

# Semantic Tags

Semantic tags are stable names that let later pipeline stages understand what kind of syntax or operation a node or instruction represents.

They are useful only when they are owned, documented and tested. A tag should not become a magic string copied across unrelated files.

## When to read this page

Read this page when a feature needs a new AST node category, bytecode instruction kind, optimizer marker or backend-recognized operation.

## Goal

Understand how tags support modular lowering without turning the system into a set of stringly typed shortcuts.

## What tags are for

A semantic tag can help answer questions such as:

- is this AST node an arithmetic operation?
- is this node a loop construct?
- should this instruction be considered by an optimizer?
- does this backend support this lowered operation?

Tags make feature ownership visible to the pipeline.

## What tags are not for

Tags should not be used to hide missing architecture.

Do not use tags as:

- undocumented magic strings;
- hidden activation switches;
- replacements for dialect selection;
- replacements for parser/AST ownership;
- backend-specific shortcuts exposed as general frontend semantics.

## Ownership rule

Every tag should have an owner.

The owner should define:

- where the tag is introduced;
- what shape or instruction it describes;
- which visitors or processors consume it;
- which dialect/module enables it;
- which tests prove it behaves correctly.

If no module clearly owns a tag, the design is probably too implicit.

## Stable names

Tags and token names often become contracts between stages. Once parser code, visitors, optimizers or backends depend on a name, changing it can break behavior far away from the module that introduced it.

Prefer:

```text
module-owned constants or centralized registration lists
```

over:

```text
repeated string literals in unrelated layers
```

This does not mean every internal name is public API. It means cross-stage names should be deliberate.

## Producer and consumer tests

A tag needs at least one producer and one consumer.

For example:

```text
parser node creator produces an AST node tag
  -> AST visitor consumes that tag
  -> bytecode instruction is emitted
  -> backend executes it
```

Tests should cover the chain. A tag that is produced but never consumed is dead metadata. A consumer that relies on a tag no test produces is a hidden coupling risk.

## Tags and optimizers

Optimizers may use tags to identify operations that can be folded, normalized or lowered into native forms.

That creates an extra obligation:

- the optimizer must preserve semantics;
- the backend must support the optimized shape;
- unsupported backend paths must not receive backend-specific operations;
- the original unoptimized path must still be correct.

## Tags and dialects

A tag does not enable a feature by itself. Dialect selection does.

Correct model:

```text
dialect selects module
  -> module registers parser/visitor behavior
  -> parser/visitor produces and consumes tags
```

Incorrect model:

```text
tag exists somewhere
  -> feature becomes available implicitly
```

The second model makes restricted dialects unreliable.

## What to document

For a new tag, document:

- the owning module;
- the syntax or semantic concept it represents;
- where it is produced;
- where it is consumed;
- backend support expectations;
- optimizer interaction, if any.

This can live in the module page, reference page or tests, depending on how public the feature is.

## Common mistakes

- Copying tag names by string literal across many files.
- Adding a tag without a consumer test.
- Letting an optimizer introduce tags a backend does not support.
- Using tags to bypass dialect composition.
- Reusing a tag for two unrelated meanings.
- Treating tag presence as proof that semantics are implemented.

## Next

Continue with [Ordering and Priority](/write-modules/ordering-and-priority).
