---
title: Performance Model
description: Separate Wist compilation, convenience execution and prepared hot invocation; avoid generic claims.
audience: all-technical-users
status: current-reference
lastVerifiedAgainst: wist-release-state-2026-08-06
---

# Performance model

## Wist paths

Cold path:

```text
source -> parse/bind -> dialect/runtime selection -> compile or execute
```

Prepared hot path:

```text
compiled typed delegate -> invocation
```

Use `Evaluate<T>` for one-off evaluation, tests and administration paths. Use `Compile<TDelegate>` when the same approved formula is invoked repeatedly.

```csharp
using var engine = WistEngine.CreateRestrictedArithmetic();
var program = engine.Compile<Func<double, double, double>>(
    "price * 0.9 + fee",
    "price",
    "fee");

double result = program.CompiledDelegate(100.0, 5.0);
```

Do not compare `Evaluate` against a prepared delegate and call the result “execution overhead”; those paths include different work.

## Generic language runtime

Generic route execution cost depends on:

- number of route transformations/passes;
- artifact allocation strategy;
- executor implementation;
- component lifetime and host objects;
- whether compilation happens inside a transformer;
- result shape and effects.

The framework does not claim a universal performance profile for external languages. Each language package must publish workload-specific cold planning, runtime creation and steady execution measurements.

## Benchmark evidence rules

Use [Benchmark Methodology](/reference/benchmark-methodology). Tie every claim to source identity, environment, correctness/parity precheck and raw BenchmarkDotNet artifacts.

Restricted composition and fast compiled execution are not sandboxing.
