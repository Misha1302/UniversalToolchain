---
title: Lifecycle, Concurrency and Privacy
description: Wist and generic runtime ownership, disposal, source retention and diagnostic boundaries.
---

# Lifecycle, concurrency and privacy

## `WistEngine`

- `WistEngine` implements `IDisposable`;
- options, resource limits, optimization settings, source-retention policy, diagnostic exposure and allowed assemblies are snapshotted during `Create`;
- public operations reject use after disposal;
- disposing the engine waits for an admitted active operation and then disposes its owned runtime;
- one engine instance is **not a concurrent operation unit**: overlapping `Evaluate`, `Validate`, `Compile` or `TryCompile` calls fail fast with `InvalidOperationException`;
- use a separate `WistEngine` per concurrent operation stream.

`Compile<TDelegate>` returns `WistProgram<TDelegate>`, which is not disposable. The alpha does not publish a general guarantee that a compiled delegate remains valid after its originating engine is disposed. Keep the engine alive for the intended program lifetime unless a focused compatibility test proves otherwise for the exact preset and backend.

`PerSession` is an ownership/lifetime statement only. It does not imply reentrancy or thread safety.

## Source retention

`WistEngineOptions.SourceRetention` controls durable `WistProgramMetadata` source retention:

- `Full` — compatibility default; stores `SourceText`, source SHA-256 and `SourceLength`;
- `HashAndIdentity` — drops `SourceText`, retains SHA-256 and length;
- `None` — drops both source text and source hash, retains source length.

The SHA-256 value is an identity aid, not secret scrubbing. Low-level artifacts outside the Wist facade may retain source or source-derived data according to their own contracts. A redacted CLI trace or `WistProgramMetadata` policy therefore does not imply that every in-memory compiler artifact is source-free.

## Consumer versus developer diagnostics

`WistEngineOptions.DiagnosticExposure` separates expected-failure diagnostics:

- `Developer` is the alpha compatibility default and may expose the expected exception object;
- `Safe` omits the exception object from `WistValidationResult`/`WistCompileResult` and bounds diagnostic messages.

`Validate` and `TryCompile` return structured failures only for expected `UserInput`, `Policy` and `Unsupported` categories. Infrastructure and internal framework faults are fail-fast and are never converted into an ordinary invalid-formula result.

Safe diagnostics are not a general-purpose secret scrubber. They reduce accidental detail exposure; hosts still own storage, access control and any domain-specific redaction policy.

## Artifact/session boundary

A low-level `ICompiledArtifact` stores immutable structure and binding metadata. `CreateSession()` creates mutable execution state. Use independent sessions for independent mutable arguments.

Binding values follow normal CLR reference semantics and are not deep-cloned.

## Generic runtime capabilities

`LanguageRuntime` is the execution-only runtime and implements `IDisposable`/`IAsyncDisposable`.

Artifact build operations are represented by the distinct `LanguageBuildRuntime` type returned by the exact component-source factory. Consumers no longer need to know which `LanguageRuntime.Create` overload happened to be used before deciding whether `Build`, `ExecuteBuilt` or `GetBuiltArtifactValue` is valid.

Both runtime types coordinate disposal with active operations and reject new operations after disposal begins. Construction rollback preserves a primary failure; if cleanup also fails, the primary exception is the first `AggregateException.InnerExceptions` entry and cleanup failures follow it.

`PerSession` components are created per runtime session and disposed in reverse construction order. `SingletonStateless` requires `IStatelessLanguageRuntimeComponent`, thread safety and no disposable resources.

## Generic concurrency

The generic runtime coordinates lifetime transitions; it does not infer thread safety from lifetime. Arbitrary stateful runtime components and mutable argument objects are not made thread-safe by `LanguageRuntime`.

The shipped Wist facade therefore uses the stricter non-concurrent same-instance contract above rather than claiming reentrancy that its component traits do not establish.

## CLI trace privacy

The default trace omits direct serialization of raw source, arguments and result values. It records source length/hash and coarse execution metadata.

Error text may still originate from lower layers. The sanitizer bounds output but does not prove secret removal from every exception message. Do not treat trace output as a hardened secret-scrubbing boundary.
