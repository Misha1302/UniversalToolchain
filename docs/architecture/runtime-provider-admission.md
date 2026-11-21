---
title: Runtime provider admission
navigation: hidden
status: Internal maintainer evidence for the runtime provider admission checks and their negative tests.
---

# Runtime provider admission

A `LanguagePlan` is built against an **exact** runtime provider identity:
`(ProviderId, ProviderVersion)` plus the contributed runtime contribution, the Toolchain API major, and
the set of supported backends. Execution must refuse anything else.

These refusals existed with **no tests**, which is the dangerous state: each check is cheap to weaken
during a refactor, and a weakened admission check does not fail loudly. It *succeeds*, and the wrong
component runs against a plan it was never selected for.

`RuntimeProviderAdversarialTests` covers each refusal.

## Where each refusal actually happens

The tests were written on the assumption that all four checks live in `LanguageRuntime`. Measuring the
real behaviour showed they do not, and the split matters for anyone reading an error message:

| Corrupted field | Refused by | Message |
|---|---|---|
| `ProviderId` | **provider registry** | `... is not registered.` |
| `ProviderVersion` | **provider registry** | `... version 'X' is required; registered version(s): Y.` |
| `ToolchainApiVersion` | `LanguageRuntime` | `Runtime provider Toolchain API version does not match the language plan.` |
| `RuntimeContributionId` | `LanguageRuntime` | `... implements contribution 'A', but the plan selected 'B'.` |

The registry is keyed by the provider's *own* `(id, version)`, so a foreign identity is a **lookup
miss**, not a contract violation — it never reaches the runtime's identity comparison. That comparison
is a second line of defence that the public API cannot currently reach, and pretending otherwise would
have produced a test that passes for the wrong reason.

`IdentityMismatchAndContractMismatch_AreReportedAsDifferentProblems` pins the distinction: collapsing
both into one message would send a caller looking in the wrong place.

## What is deliberately not covered

An unreachable check is not made "covered" by a test that cannot reach it. The runtime also refuses a
provider that does not implement `ILanguageRuntimePolicyValidator` when the plan requires policy
validation — but every shipped Wist provider implements it, and `full-default` runs with
`AllowHostInterop: true`, so no shipped preset exercises that path. A draft test for it failed because
the condition never held; it was removed rather than bent to fit.

## Mutation evidence

| Mutation | Result |
|---|---|
| contribution check disabled (`if (false && ...)`) | **2 tests failed** |
| API version check disabled | **1 test failed** |

Both were applied to `LanguageRuntime.cs` and reverted.

## Verification

`./build.sh --skip-pack` → `EXIT=0`, 1390/1390 (LanguageSdk 209 → 214).
