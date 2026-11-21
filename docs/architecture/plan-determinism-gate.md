---
title: Plan determinism gate
navigation: hidden
status: Internal maintainer evidence for the planner determinism gate and its mutation self-test.
---

# Plan determinism gate

A `LanguagePlan` is expected to be a pure function of the *set* of selected features, backends and
packages — never of the order in which the language definition declared them. That property is true
today, but it is true for a fragile reason: each planning phase happens to sort before it emits.

The failure mode of losing that property is not a crash. It is the same language producing a different
route, or a different lock file, depending on declaration order. Two builds of identical input then
disagree, and nothing in a behavioural suite notices, because both plans are individually valid.

`PlanDeterminismGateTests` pins the property by measurement instead of by reading the sort calls.

## What is measured

| Artifact | Why it is the observable |
|---|---|
| `LanguageLockFile.SerializeCanonical` bytes | the value that gets committed and compared across machines |
| `PlanHash` | the identity the planner publishes |
| **raw** `plan.Contributions` sequence | runtime dispatch iterates this; canonical bytes are sorted by the serializer and therefore cannot observe a planner that emitted a different order |
| **raw** per-backend route step sequence | the execution order the backend actually receives |
| diagnostic sequence | user-facing output; reordering makes two appearances of one failure look like two failures |

The raw-sequence assertions exist because of a measured false negative. Removing the final canonical
sort in `LanguageContributionResolutionPhase` left every canonical-bytes comparison green: the
serializer re-sorted on the way out and hid the planner's changed order. Only the raw sequence saw it.
A gate that inspects the serializer instead of the planner verifies the serializer.

## Corpus

Every shipped Wist preset (9), every backend each preset declares (`cil`, `interpreter`), and eight
fixed seeds per preset. Each seed permutes the declaration order of selected features and backends
while keeping the content identical.

The generator is xorshift32 rather than `System.Random`, because `Random`'s output shape has changed
between .NET versions; a failure recorded today must reproduce tomorrow from the seed alone.

## Why `GateCanFail` matters

`GateCanFail` feeds a deliberately order-sensitive stub through the same comparison the corpus uses and
requires that comparison to report a difference. Without it, a refactor that accidentally stopped
comparing anything would leave the whole fixture green — the gate would assert nothing while looking
like coverage.

This is not theoretical. The first version of this fixture compared only canonical bytes and only
`PlanHash`. Two mutations were applied and both **survived**:

1. removing `OrderBy` over `definition.Backends` in `LanguageArtifactRoutePhase`;
2. removing the final slot/order/id canonical sort in `LanguageContributionResolutionPhase`.

The second one changed 30 plan instances and produced zero failures. That is exactly the false-negative
shape the gate exists to prevent, and it was found because the mutation was run rather than assumed.

## Verified sensitivity

Removing the final canonical ordering in `LanguageContributionResolutionPhase.ApplySlotPolicies` makes
the gate fail with 30 determinism violations across `EveryShippedPreset_...` and
`BackendDeclarationOrder_...`. Restoring it makes the fixture green again.

This is the only mutation that currently reproduces a red gate. It is worth knowing why: ordering is
stabilised redundantly at several layers (feature resolution, seed ordering, slot policies, definition
order, canonicalizer). That redundancy is real protection, and it is also why a mutation has to target
the outermost layer to be observable.

## Running it

```bash ci-timeout=300
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --filter "FullyQualifiedName~PlanDeterminismGateTests"
```

The fence declares `ci-timeout=300` because the command restores and builds the whole Release graph
before the two-second test run. Measured on this tree: 78 s when the Release binaries had to be rebuilt,
44 s on the immediately following run, against the runner's 60 s default. Without the attribute the block
passes only when the machine happens to be warm, so a clean checkout can fail on infrastructure rather
than on determinism.

The fixture is part of the `Core` suite and therefore part of `./build.sh --skip-pack`.
