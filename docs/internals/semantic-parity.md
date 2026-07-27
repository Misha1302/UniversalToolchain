---
title: Semantic Parity
description: Explain why interpreter and compiled execution must agree.
---

# Semantic Parity

Semantic parity means that two supported execution paths produce the same observable behavior for the same dialect, source program and runtime inputs.

For Wist, the most important parity boundary is interpreter vs. CIL compiler.

## What parity is

When a dialect exposes both `interpreter` and `cil`, the same program should produce equivalent results through both modes:

```text
same dialect
same source
same external bindings
  -> interpreter result
  -> compiler result
  -> same observable behavior
```

This is a correctness requirement, not a performance benchmark.

## What parity is not

Parity does not mean every backend supports every dialect.

A dialect may intentionally expose only one backend. In that case, unsupported modes should fail explicitly. That is backend availability, not backend parity.

Keep these concepts separate:

| Concept | Meaning |
|---|---|
| backend availability | whether a dialect exposes a backend mode |
| semantic parity | whether exposed backends agree |
| optimizer safety | whether optimized and unoptimized behavior match |
| failure parity | whether invalid programs fail consistently enough across paths |

## Why parity matters

The interpreter and CIL backend consume the same AIR but implement execution differently.

The interpreter executes instructions directly with an instruction pointer, value stack and label positions.

The CIL backend simulates stack types, emits IL into a `DynamicMethod`, and executes compiled code.

Because the implementations are different, tests are required to prove they preserve the same language semantics.

## Value parity

Value parity compares successful results.

Examples:

```wist
2 + 3 * 4
```

```wist
let x = 5
x = x + 1
x
```

```wist
if 2 == 2 (1) else (2)
```

```wist
let sum = 0
let i = 1

while i <= 5 (
    sum = sum + i
    i = i + 1
)

sum
```

If both backends are exposed for the dialect, these examples should return equivalent values.

## Failure parity

Failure parity compares invalid or unsupported programs.

For high-level module tests, it is often enough to assert that both backends fail and that the failure is comparable. Exact diagnostic text may be too brittle if diagnostics are not yet a stable public contract.

Failure parity is useful for:

- malformed syntax;
- omitted module features;
- unsupported backend modes;
- unsupported intrinsic forms;
- invalid runtime binding shapes.

## Optimizer parity

Optimizer parity compares optimized and unoptimized behavior.

An optimizer may change AIR shape, but it must not change observable semantics.

Useful structure:

```text
run source without optimizer
run source with optimizer
compare results
then compare interpreter/compiler if both are exposed
```

This catches optimizer bugs that would not appear in one backend-only test.

## Runtime binding parity

External bindings must have the same meaning across backends.

A compiled artifact may use declared parameter types, while runtime execution passes values. The same source should not resolve a local variable differently depending on backend mode.

This matters especially for variables and formulas with external inputs.

## Current test infrastructure pattern

The module coverage tests use helper methods with this shape:

```text
CreateHost(modules, optimizers, backends)
ExecuteBoth(code, modules, optimizers)
AssertParity(compiler, interpreter)
AssertCompilerAndInterpreterFailSameWay(code, modules)
```

This pattern is valuable because it tests the composed dialect surface rather than only one hardcoded runtime path.

## What to test for a feature

A feature that affects execution should have parity cases for:

- smallest valid source;
- realistic source with neighboring modules;
- nested source when nesting is supported;
- failure when the owning module is omitted;
- backend availability when only one backend is exposed;
- optimizer behavior when optimizers touch the feature.

## What to test for a backend

A backend-affecting change should have parity cases for:

- `Push` / result behavior;
- arithmetic intrinsics;
- local variables and external bindings;
- labels and jumps;
- conditions;
- loops;
- unsupported intrinsic handling;
- empty stack / void-like result behavior.

## Common mistakes

- Testing CIL mode only.
- Treating interpreter success as proof that CIL works.
- Treating CIL success as proof that semantics are correct.
- Comparing performance before proving correctness.
- Letting unsupported backend modes silently fall back.
- Testing only successful examples and ignoring failure parity.

## Documentation rule

Do not document a dialect as supporting both modes unless there are tests or examples that exercise both modes.

Do not document an optimizer as semantics-preserving unless optimized and unoptimized behavior are compared somewhere.

## Next

Continue with [Dependency Injection](/internals/dependency-injection).
