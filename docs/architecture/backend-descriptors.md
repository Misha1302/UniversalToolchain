---
title: Backend descriptors
navigation: hidden
status: Internal maintainer architecture evidence for backend descriptions (UT-A047).
---

# Backend descriptors

## What was wrong

Backend identity was a string compared against `"cil"` and `"interpreter"` in five places. Three of them
ended in `throw new InvalidOperationException("Unsupported Wist backend '...'")`:

| Site | What it branched on |
|---|---|
| `WistModuleSelection.GetExpectedRuntimeBackendAliases` | a switch whose arms returned the string they matched |
| `WistModuleContractRouteObserver.CreateBackendComponents` | component id and contract provider |
| `WistModuleContractRouteObserver.GetSupportedIntrinsics` | intrinsic list |
| `WistDirectRuntimeComponents.CreateCapabilityContext` | optimizer capability set |
| `WistBuiltArtifactActivation.Materialize` / `GetSsaReport` | durable program and report projection |

The compiler could not help. A string switch with a throwing default arm is only wrong **when it runs**,
so adding a third backend produced a runtime failure at whichever site the author missed.

## What replaced it

`WistBackends` declares one `WistBackendDescriptor` per backend: identity, supported intrinsics, pipeline
component id, contract provider factory, intrinsic capability set, durable-program projection and SSA
report projection. `WistBackendCatalog` resolves a descriptor by `BackendId`.

The pack is now the only place that names a backend to make a decision. Every other site asks the catalog.

## Why the catalog is an instance, not a static registry

The claim worth testing is "a third backend needs no edit to the consumers". A test can only demonstrate
that by constructing a catalog that contains a backend the pack does not ship. With a mutable static, the
test would have to mutate global state — which would poison every later test in the process.

`WistBackendCatalog.Canonical` is the shipped catalog; a test builds its own. The probe backend used in
`ThirdBackendAcceptanceTests` is deliberately useless for execution: it proves the **description** path is
complete, and it refuses to execute rather than pretending. What it cannot prove is that a real third
backend runs Wist bytecode — that needs an implementation and is UT-A044's job.

## The gate and what it allows

`BackendDescriptorTests.PackHasNoBackendNameComparison` scans the pack source. It rejects a comparison,
pattern, switch arm or string call on a backend literal, in any file and any position.

It **allows** the literal in a declaration (`new("cil")`), because a backend has to be named somewhere. A
gate that forbade every occurrence would push authors into obfuscating the name, which is worse than the
defect. `GateDetectsEachBypass` runs six mutant shapes — including the `==`, `is`, `switch =>` and
`string.Equals` forms — and requires a violation for each, so the scan is proven able to fail.

## What this does not change

No plan hash and no bytecode changed. The refactor moves the same decisions behind a lookup; the shipped
two backends produce identical artifacts, which the golden plan hashes and the canonical bytecode corpus
confirm.
