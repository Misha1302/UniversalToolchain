---
title: Intrinsic catalog trust boundary
navigation: hidden
status: Internal maintainer evidence — why the catalog builder owns its own guards.
---

# Intrinsic catalog trust boundary

## The observation

Three validators are registered in `BasicCore.Registration.CoreIntrinsicServiceCollectionExtensions`:

- `IntrinsicDescriptorProviderMetadataValidator`
- `IntrinsicSemanticCoverageValidator`
- `IntrinsicSemanticStartupValidator`

**None of them is ever resolved in production code.** Measured by grep over the whole tree: the only
occurrences outside their own files and `Tests/` are the three `TryAddSingleton` lines that register them.
A registered service that nothing resolves is inert: DI will happily construct it if asked, and nothing
asks.

`IntrinsicSemanticStartupValidator.BuildResult` is called only from `IntrinsicSemanticStartupValidatorTests`.
`IntrinsicDescriptorProviderMetadataValidator.Validate(IServiceCollection)` — the overload that inspects the
service collection itself — has **zero** call sites anywhere, including tests.

## Why that is a real gap, not just dead code

`IntrinsicCatalogBuilder` is the opposite: it is the only place a production catalog is built.

```
CoreIntrinsicServiceCollectionExtensions  (DI factory for IIntrinsicCatalog)
IntrinsicSemanticStartupValidator         (its own catalog, for validation only)
WistModuleContractRouteObserver           (AirVerifier's catalog)
AirVerifier                               (per-backend catalog)
```

Before this change the builder checked exactly two things per descriptor: `StackRule != null` and
`ValidationRule != null`. The metadata validator checked considerably more — null provider registration,
null descriptor collection, null descriptor instance, default symbol, duplicate symbol across providers,
duplicate provider registration.

Four of those six checks existed **only** in code that never runs. A descriptor with a `default` symbol is
the sharpest case: it is not null, so it passes a null check, and it lands in the dictionary under the
all-empty key. Because `IntrinsicSymbol` is a `readonly record struct`, *every* descriptor that forgot to set
a symbol compares equal to every other one — so two such descriptors do not collide loudly, they silently
keep whichever was inserted first, and neither can ever be resolved by a real invocation.

`IntrinsicCatalogBuilderTests` previously had no case for any of this. The duplicate-symbol test covered the
one check the builder actually had.

## What changed

The builder now owns the guards it needs, with messages that name the provider:

| Guard | Prevents |
|---|---|
| null provider registration (with index) | a null entry in a selected set surviving to `GetDescriptions()` |
| null descriptor collection | a provider returning `null` instead of an empty list |
| null descriptor element | a `null` inside an otherwise valid list, which only fails later at `Symbol` access |
| default symbol | two symbol-less descriptors silently collapsing to one key |
| duplicate symbol | already present; unchanged |
| `StackRule` / `ValidationRule` null | already present; unchanged |

The three DI-registered validators are **left registered and left uncalled**. Removing them would be an API
change to a published package (`BasicCore` is referenced widely), and their presence is what made the gap
measurable in the first place. What changes is that the production path no longer *depends* on them.

## Evidence

Four new tests in `IntrinsicCatalogBuilderTests`, all RED before the change and GREEN after:

```
Build_ShouldFail_WhenProviderReturnsDescriptorWithDefaultSymbol
Build_ShouldFail_WhenProviderReturnsNullDescriptor
Build_ShouldFail_WhenProviderReturnsNullCollection
Build_ShouldFail_WhenProviderReturnsNullInstance
```

Mutation-checked — each guard was removed individually and a real test failed:

| Mutant | Result |
|---|---|
| `no-null-provider-guard` | killed (1 test) |
| `no-null-collection-guard` | killed (2 tests) |
| `no-null-descriptor-guard` | killed (1 test) |
| `no-default-symbol-guard` | killed (1 test) |

The `no-null-descriptor-guard` mutant survived the **first** version of its test, and that is worth
recording. `new FakeProvider(null!)` passes a `null` *array*, not an array containing `null` — so the test
was really exercising the null-collection guard and would have claimed coverage it did not have. The test
now uses a provider that returns `[null!]`, which is the defect the guard actually names.

Wider checks after the change: `Tests` intrinsic filter 122/122, `Dialects.Tests` intrinsic filter 7/7,
`LanguageSdk.Tests` intrinsic filter 5/5.

## What this does not do

- It does not delete the three validators, and it does not wire them in. Wiring them in would make startup
  fail closed on the current tree, which needs its own measurement and its own PR.
- It does not add the plan-time intrinsic support check from `UT-A043`; that gap is separate and still open.
- It does not change any plan, plan hash, or bytecode.
