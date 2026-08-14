---
title: Performance Model
description: Separate Wist setup, formula compilation, convenience execution and prepared hot invocation; avoid generic claims.
audience: all-technical-users
status: current-reference
lastVerifiedAgainst: wist-architecture-production-hardening-2026-08-13
---

# Performance model

## Wist paths

Wist has three materially different cost boundaries. Do not collapse them into one "execution" path.

### 1. Engine setup and semantic planning

`WistEngine.Create(...)` resolves the selected preset or dialect once and materializes one exact runtime from the resulting immutable plan:

```text
DialectSource / preset
  -> LanguageDefinition
  -> LanguageCompiler
  -> immutable LanguagePlan
  -> LanguageBuildRuntime
```

`LanguageCompiler` is the semantic planner. `Evaluate`, `Validate`, `Compile<TDelegate>` and `TryCompile<TDelegate>` reuse the plan/runtime created here; they do not perform dialect selection or create a second backend plan for each formula.

Measure this boundary when engine construction itself matters to startup or request latency.

### 2. Formula processing

Formula work begins after the engine already owns its plan/runtime. The exact stages depend on the selected route, but the Wist pipeline is conceptually:

```text
source
  -> lexer / parser
  -> AST
  -> Bytecode
  -> AIR
  -> optional optimizer / SSA route
  -> selected backend artifact or execution
```

`Evaluate<T>` is the convenience path for one-off execution. It includes source processing and the selected runtime route on every call.

`Compile<TDelegate>` processes the formula and materializes a typed durable program/delegate for repeated invocation. Compilation is therefore a cold operation relative to the prepared delegate hot path.

### 3. Prepared hot invocation

```text
compiled typed delegate
  -> invocation
  -> result
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

Do not compare `Evaluate` against a prepared delegate and call the difference "execution overhead": those paths include different work. Likewise, do not attribute `WistEngine.Create` planning/runtime-creation cost to per-formula execution; it is a distinct setup boundary.

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
