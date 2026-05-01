---
title: Ordering and Priority
description: Explain deterministic module ordering and parser priority.
---

# Ordering and Priority

Ordering and priority decide which registered components see syntax first and how overlapping constructs are resolved.

For module authors, this is not a cosmetic detail. Non-deterministic ordering can make the same dialect behave differently across machines, test runs or reflection results.

## When to read this page

Read this page when you add parser node creators, AST visitors, optimizers, dialect modules or runtime exports whose order can affect behavior.

## Goal

Understand which ordering decisions must be explicit and tested.

## Places where ordering matters

Ordering can affect several stages:

| Stage | Why order matters |
|---|---|
| Lexeme registration | overlapping token patterns may compete |
| Parser node creators | priorities define grammar behavior |
| AST visitors | multiple visitors may inspect the same node |
| Bytecode processors | transformations may depend on previous instructions |
| Optimizers | one optimization may enable or invalidate another |
| Runtime composition | selected modules must be resolved deterministically |
| Backends | supported intrinsics and lowered shapes must match the selected backend |

Do not assume order is irrelevant unless tests prove it.

## Parser priority

Parser priority is the most visible ordering mechanism.

Arithmetic is a simple example: multiplication and division must bind tighter than addition and subtraction. Unary minus also needs its own position in the priority system.

Conceptual ordering:

```text
unary operators
  -> multiplication/division
  -> addition/subtraction
  -> broader expression constructs
```

The concrete numbers are implementation details, but the intent must be stable.

## Deterministic module composition

A dialect should produce the same module composition every time:

```text
same dialect file
same repository state
same selected modules
same ordered runtime plan
```

If a test sometimes sees modules in a different order, that is an architecture smell. Fix deterministic resolution instead of adjusting the assertion randomly.

## AST visitor ordering

Visitor order matters less when every visitor self-filters correctly, but it can still matter when visitors cooperate or share scoped state.

A safe visitor should:

- return immediately for nodes it does not own;
- avoid mutating unrelated nodes;
- avoid hidden state that depends on previous visits;
- document intentional cooperation with another visitor.

When visitor order is important, test that order directly or test the observable behavior that depends on it.

## Optimizer ordering

Optimizer order can change performance and sometimes correctness if transformations are not isolated.

Rules:

- optimizers must preserve observable semantics;
- backend-specific optimizers must run only where the backend supports their output;
- optimizer order should be deterministic;
- tests should compare optimized and unoptimized behavior when possible.

Do not introduce an optimizer only to make broken base lowering work.

## Priority changes are risky

Changing an existing parser priority can affect syntax that appears unrelated to the feature you are modifying.

Before changing a priority:

1. identify which constructs can overlap;
2. add a failing test that demonstrates the intended grammar change;
3. run examples involving nearby syntax;
4. verify interpreter/compiler parity;
5. document the reason if the change is non-obvious.

## What to test

For ordering and priority, test:

- precedence examples such as `2 + 3 * 4`;
- grouping examples such as `(2 + 3) * 4`;
- nested constructs;
- syntax that could be parsed by more than one node creator;
- deterministic module order in composition results;
- backend parity when ordering affects emitted semantics;
- optimizer behavior when one optimizer depends on another.

## Common mistakes

- Relying on reflection enumeration order.
- Picking priority values by trial and error.
- Adding a node creator with a broad match that steals syntax from another feature.
- Assuming visitor order is safe without self-filtering.
- Making optimizer ordering implicit.
- Fixing ordering problems by hardcoding concrete module names in generic layers.

## Practical checklist

Before merging an ordering-sensitive change, verify:

- the order is explicit;
- the order is deterministic;
- the order is covered by tests;
- the feature can still be omitted by dialect composition;
- restricted dialects do not gain the feature accidentally;
- compiler and interpreter results still match when both backends are enabled.

## Next

Continue with [Testing a Module](/write-modules/testing-module).
