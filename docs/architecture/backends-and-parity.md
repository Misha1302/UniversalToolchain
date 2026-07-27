# Backends and Semantic Parity

This document defines the backend architecture expectations for UniversalToolchain and Wist.

## Backend purpose

A backend is an execution strategy selected by a dialect runtime plan.

A backend must not decide which language features exist. Feature availability is selected earlier by dialect composition and runtime manifests.

## Current backend roles

The current public story includes two important execution roles:

- interpreter path;
- CIL/compiler path.

The interpreter should be treated as the semantic reference path. The CIL path should be treated as the performance-oriented compiled path.

## Interpreter as semantic oracle

The interpreter is not just a slower fallback.

Under the current architecture rules, the interpreter is the reference path used to answer:

> What does this selected dialect mean?

This gives the project a clean correctness model:

- interpreter behavior defines expected semantics;
- compiled backend behavior must match it;
- optimizer behavior must preserve it;
- backend-specific intrinsics must not leak into the interpreter unless the architecture policy changes.

## CIL backend role

The CIL backend is the performance-oriented backend.

It may use native lowering, optimized invokers, and backend-specific intrinsics only when those features are selected and supported.

The CIL backend must not become a second semantic definition of the language.

## Semantic parity contract

When a feature is supported by both interpreter and CIL, the same program should produce the same observable result in both modes.

Parity tests should cover:

- arithmetic;
- variables;
- scopes;
- conditions;
- loops;
- labels and jumps when supported;
- function calls when supported;
- restricted dialect rejection behavior;
- optimizer-enabled and optimizer-disabled configurations when relevant.

If interpreter and CIL disagree, treat the compiled path or optimizer as suspect until the semantic rule is clarified.

## Intrinsic support contract

Backend-specific intrinsic use must be capability-gated.

Rules:

- A backend declares supported intrinsics.
- Optimizers may emit backend-specific intrinsics only when the selected backend supports them.
- Generic semantic passes must not assume CIL-specific intrinsics exist.
- The interpreter must not grow feature-specific intrinsic branches merely to make optimized tests pass.
- Unsupported intrinsic use should fail early with a clear diagnostic.

## Compiled artifact boundary

The repository already implements two neutral boundaries:

- Wist dialect integration uses `IArtifactCompiler`, `ICompiledArtifact` and `ICompiledArtifactSession`;
- external languages use typed `LanguageArtifact<T>` routes and an exact backend executor selected by `LanguagePlan`.

The remaining design debt is adoption and validation, not absence of the abstraction:

- some low-level Wist fast-invocation helpers still intentionally expose `DynamicMethod`;
- the Wist runtime provider must reject typed routes it cannot faithfully execute;
- a third production-scale backend is still needed to pressure-test every convenience and packaging boundary.

```text
selected backend and typed terminal contract
-> backend cil/executor
-> neutral artifact/session or generic route result
-> facade/host execution
```

## Backend author checklist

A backend contribution should define:

- backend alias and declaration metadata;
- selected-runtime activation path;
- supported intrinsic set;
- accepted input representation;
- produced artifact type or abstraction;
- fallback behavior or diagnostics for unsupported features;
- semantic parity tests against the interpreter/reference path;
- negative tests for unsupported intrinsics and disabled dialect capabilities.

## Optimizer interaction

Optimizers must be backend-aware without becoming backend-hardcoded in generic layers.

Good optimizer behavior:

- reads selected backend capabilities;
- emits only supported intrinsics;
- has a non-optimized semantic fallback;
- proves semantic preservation through tests.

Bad optimizer behavior:

- assumes CIL is always available;
- emits native intrinsics before backend selection;
- changes interpreter behavior indirectly;
- uses concrete Wist profile names to decide optimization behavior.

## Documentation expectations

Every backend should have documentation explaining:

- what semantic level it consumes;
- what optimizations it expects;
- which intrinsics it supports;
- how it is selected by dialect/runtime manifests;
- how parity with the reference path is tested.

## Execution-bound native pointer wrapper parity note

The CIL fast native pointer path may expose execution-bound wrappers over generated `DynamicMethod` artifacts. Generated CIL dynamic methods use `IExecutionEnvironment` as the hidden first parameter, followed by external binding arguments in declared binding order. Public fast wrappers hide this environment parameter from callers, validate full raw `DynamicMethod` signatures before function-pointer invocation, and provide typed `Invoke` methods for the supported native pointer arities. When generated code lowers external loads through the universal runtime-call path, the wrapper must ensure those loads observe the same invocation arguments as the native pointer call.
