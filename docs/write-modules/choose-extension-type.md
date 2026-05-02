---
title: Choose an Extension Type
description: Decide whether you need a dialect, frontend module, optimizer, backend feature, or intrinsic.
---

# Choose an Extension Type

Before writing a module, decide whether you really need one. UniversalToolchain has several extension points. Choosing the wrong one usually creates hardcoded behavior, duplicate syntax ownership, or backend drift.

## Start with the smallest extension point

Use this decision table before creating new code:

| You want to... | Prefer... | Why |
|---|---|---|
| Restrict or combine existing language features | a `.wistdialect` file | no new syntax or runtime behavior is needed |
| Add new syntax that lowers to existing semantics | a frontend module | the module owns lexer/parser/AST integration |
| Add new syntax with new runtime behavior | a frontend module plus bytecode/AIR/backend support | syntax and execution must evolve together |
| Add a performance rewrite for existing AIR | an optimizer module | semantics already exist; only lowering changes |
| Add a backend-specific fast path | an intrinsic plus backend capability checks | the backend must explicitly declare support |
| Add a new execution target | a backend declaration and backend service provider | execution is a runtime concern, not a frontend concern |

## When a dialect file is enough

Do not create a module when the feature is only a product profile.

For example, this is a dialect composition problem:

```text
dialect PricingFormula
use Whitespaces,Numbers,Scopes,Arithmetic
backend interpreter
```

The dialect says which existing syntax exists. It does not add syntax.

## When a frontend module is right

Create a frontend module when the feature introduces new source-level syntax.

Examples:

- a new operator;
- a new keyword;
- a new expression form;
- a new statement form;
- a new module-owned AST translation rule.

A frontend module usually contributes:

1. module metadata;
2. lexer registrations;
3. parser node creators;
4. AST visitors;
5. tests proving dialect visibility and backend parity.

The next page walks through a small frontend module that adds `plus` as a textual addition operator.

## When backend work is required

A frontend module can reuse existing semantics, but not every feature can do that.

You need backend work when:

- the emitted bytecode operation is new;
- the generated AIR shape is new;
- an optimizer emits backend-specific intrinsics;
- interpreter and compiler do not already understand the behavior;
- the feature has backend-specific availability.

Do not hide missing backend behavior behind a parser shortcut or a fallback path. If both compiler and interpreter are supported, add parity tests.

## When an optimizer is right

Use an optimizer when the source language already works without it.

An optimizer may improve generated AIR or backend execution, but it must not be the only implementation of the feature semantics. Base execution should remain correct with optimizers disabled.

## Checklist

Before adding a module, answer:

- What source syntax does this feature own?
- Which existing modules does it depend on?
- Does it lower to existing semantics or introduce new runtime behavior?
- Which backend modes must support it?
- What should fail when the module is not selected?
- Which tests prove positive execution, negative dialect surface, and backend parity?

If the answers are unclear, start with a dialect or design note rather than a new runtime component.

## Next

Continue with [Create Your First Module](/write-modules/create-your-first-module).
