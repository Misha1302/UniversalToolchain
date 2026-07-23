---
title: Lifecycle, Concurrency and Privacy
description: Wist and generic runtime ownership, disposal, source retention and trace boundaries.
---

# Lifecycle, concurrency and privacy

## `WistEngine`

- `WistEngine` implements `IDisposable`;
- options, resource limits, optimization settings and allowed assemblies are snapshotted during `Create`;
- public operations reject use after disposal;
- disposing the engine disposes its owned execution host.

`Compile<TDelegate>` returns `WistProgram<TDelegate>`, which is not disposable. The alpha does not publish a general guarantee that a compiled delegate remains valid after its originating engine is disposed. Keep the engine alive for the intended program lifetime unless a focused compatibility test proves otherwise for the exact preset and backend.

## Source retention

`WistProgramMetadata.SourceText` stores the complete formula. Low-level `ICompiledArtifact.SourceText` also stores source text.

A redacted CLI trace does not imply that in-memory compiled artifacts are source-free. Treat compiled programs as sensitive when formulas are confidential.

## Artifact/session boundary

A low-level `ICompiledArtifact` stores immutable structure and binding metadata. `CreateSession()` creates mutable execution state. Use independent sessions for independent mutable arguments.

Binding values follow normal CLR reference semantics and are not deep-cloned.

## Generic `LanguageRuntime`

`LanguageRuntime` implements `IDisposable` and `IAsyncDisposable`. It owns one runtime session, coordinates disposal with active operations and rejects new operations after disposal begins.

`PerSession` components are created per runtime session and disposed in reverse construction order. `SingletonStateless` requires `IStatelessLanguageRuntimeComponent`, thread safety and no disposable resources.

## Concurrency

The framework coordinates lifetime transitions; it does not make arbitrary user components or mutable arguments thread-safe. The public alpha does not promise universal concurrent use of one Wist engine, one low-level artifact session or one generic runtime containing stateful components.

Use separate sessions/runtimes where mutable state requires isolation and document reentrancy for every external transformer/executor.

## CLI trace privacy

The default trace omits direct serialization of raw source, arguments and result values. It records source length/hash and coarse execution metadata.

However, error text may include `exception.Message` from lower layers. The current sanitizer bounds length but does not prove secret removal from every exception message. Do not treat trace output as a hardened secret-scrubbing boundary; review storage and access controls accordingly.
