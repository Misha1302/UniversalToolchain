---
title: Single planner authority
navigation: hidden
status: Internal maintainer evidence for ADR-001 — one planner, runtime never plans.
---

# Single planner authority

`LanguageCompiler` owns three resolution phases:

| Phase | Responsibility |
|---|---|
| `LanguageFeatureResolutionPhase` | which features are selected, and their closure |
| `LanguageContributionResolutionPhase` | which contributions those features require |
| `LanguageArtifactRoutePhase` | which transformation route each backend receives |

`UniversalToolchain.Runtime` executes a plan it is handed. It must never decide what was selected.

## Why "it is internal" is not enough

All three phases are declared `internal`, and `UniversalToolchain.LanguageSdk` grants no
`InternalsVisibleTo`. Under those two facts a runtime reference is a *compile error*, not a review
question — verified: adding `nameof(LanguageArtifactRoutePhase)` inside the Runtime assembly fails with

```text
error CS0122: 'LanguageArtifactRoutePhase' is inaccessible due to its protection level
```

That is the strongest possible enforcement and it is also silently removable. One visibility edit, or
one `InternalsVisibleTo` attribute, and a second planning authority becomes reachable while every test
still passes. Nothing in the suite observes the difference until the two authorities disagree at
runtime — which is a class of bug that is very expensive to attribute.

So the fact is asserted rather than assumed.

## Assertions

| Test | Rejects |
|---|---|
| `ResolutionPhases_AreInternalAndNotPublic` | a phase declared `public`, or missing `internal` |
| `LanguageSdk_GrantsNoInternalsToAnyAssembly` | any `InternalsVisibleTo` in the SDK |
| `Runtime_ReferencesNoResolutionPhaseType` | a runtime code reference to any resolution phase |
| `Runtime_ReferencesNoPlannerFeatureType` | a runtime reference to `LanguageFeatureId` — features are a planning concept |

`GateDetectsEachBypass` pushes four mutations through the same analysis and requires a reported
violation for each, so the source scan cannot quietly stop matching.

## Mutation evidence

Each shape was applied to the real tree and the gate observed red:

| Mutation | Result |
|---|---|
| `internal static class LanguageArtifactRoutePhase` → `public static class ...` | **failed**: `not declared internal`, `declared public` |
| `[assembly: InternalsVisibleTo("UniversalToolchain.Runtime")]` added to the SDK | **failed**: names the offending line |
| `_probe = nameof(...LanguageFeatureId)` added to a Runtime class | **failed**: `line 1251: LanguageFeatureId in ...` |
| real reference to a phase type in Runtime | **compile error CS0122**, before the gate runs |

The fourth row is the reason the gate exists at all: it documents that the *current* enforcement is the
compiler, and the gate is what keeps that state from being changed without anyone noticing.

## Limitation

The gate is a source scan, not a semantic analysis. A reference constructed through reflection or an
alias would not be caught. That is an accepted limit: reflection into a planner phase from the runtime
would be a far more visible change than an `InternalsVisibleTo`, and the alternative — an IL-level
scan — costs more than the risk it removes.
