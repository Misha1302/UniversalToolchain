---
title: Runtime Lifecycle and Policy
description: Session ownership, disposal, concurrency boundary and fail-closed runtime policy.
---

# Runtime lifecycle and policy

`LanguageRuntime` owns one runtime session created from the exact provider selected by the plan. It implements both `IDisposable` and `IAsyncDisposable`.

## Creation checks

`LanguageRuntime.Create` validates before session creation:

- the plan is executable and has a runtime provider;
- provider ID and version match the plan;
- Toolchain API versions match;
- runtime-provider contribution ID matches;
- every enabled backend is supported and has a route;
- a policy validator exists when determinism or no-host-interop policy requires it;
- selected package manifests and runtime component contracts match when route-component sources are assembled.

## Runtime policy

`LanguageRuntimePolicy` currently includes bounded controls such as:

- `RequireDeterminism`;
- `AllowHostInterop`;
- maximum source length;
- maximum external parameter count.

Runtime component traits are package attestations. The generic route provider rejects components whose declared traits violate the selected policy. These controls are not process isolation and do not prove that a hostile third-party component truthfully described itself.

## Component lifetimes

| Lifetime | Contract |
|---|---|
| `PerSession` | Default. A fresh component is created for each session and disposed by that session. |
| `SingletonStateless` | Explicit opt-in for immutable, thread-safe, non-disposable components implementing `IStatelessLanguageRuntimeComponent`. |

A `PerSession` factory that returns an instance previously used by another session is rejected. This prevents accidental sharing of mutable state through a cached factory result.

## Disposal and in-flight work

- new operations are rejected after disposal starts;
- disposal waits for in-flight operations to leave the lifetime gate;
- owned components are released in reverse construction order;
- async components are awaited by `DisposeAsync`;
- synchronous disposal bridges async-only components by waiting for `DisposeAsync`;
- multiple dispose calls are idempotent;
- execution after disposal throws `ObjectDisposedException`.

## Concurrency boundary

The lifetime gate coordinates disposal with concurrent runs. This does not make arbitrary language components thread-safe. A runtime session may contain mutable `PerSession` transformers or executors; package authors must document their own reentrancy contract and use independent runtimes when execution state is not safe for concurrent calls.

## Input and data retention

`LanguageExecutionRequest` owns a typed input artifact and an argument dictionary. Values are passed by normal CLR reference semantics; mutable object graphs are not deep-cloned.

For Wist-specific source retention and trace privacy, see [Lifecycle, concurrency and privacy](/reference/lifecycle-concurrency-privacy) and [Security](/SECURITY).
