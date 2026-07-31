# CGO 2027 verification policy specification

Status: executable specification for the four-policy boundary runner.
Baseline: `master` at `c73b418c6e72e8b92371753a3a7b4a9f7adaa5f1`.

## Objective

The policy dimension controls **only semantic-verifier scheduling**. It may not change source text, selected modules, compiler passes, backend, mutation, oracle, or artifact supplied to a verifier. Structural pipeline checks remain outside the scheduler and therefore remain identical for all policies at the same represented boundary.

The scheduler receives one typed input:

```text
policy
available canonical verifier routes at this boundary
reverification requests created by invalidated facts
```

It returns an ordered list of semantic verifier invocations. A route is identified by a `VerifierRuleId` and exactly one canonical owner.

## Policies

### `P0_STRUCTURAL`

Runs no semantic verifier through obligation routing. Existing structural/capability checks executed outside the scheduler remain active.

### `P1_INVALIDATION`

Computes and records `requires/produces/preserves/invalidates` state, but schedules no semantic verifier automatically. An invalidated fact remains unavailable until an explicit later producer or verifier establishes it.

### `P2_SELECTIVE`

Schedules exactly the canonical routes requested by invalidated facts:

1. merge duplicate requests for the same rule;
2. deduplicate and sort invalidated facts;
3. resolve each requested rule to one canonical owner;
4. fail closed if a request has no route or conflicting owners;
5. invoke scheduled rules in ordinal `VerifierRuleId` order.

No clean boundary without an obligation receives an invocation from this scheduler.

### `P3_ALWAYS`

Schedules every canonical semantic verifier available at the represented boundary, independently of invalidation. Existing requests are retained on the corresponding invocation for obligation accounting. Unknown requested routes and owner conflicts still fail closed. Ordering is identical to `P2_SELECTIVE`.

## State transition model

For each pass effect, the reference state transition is:

```text
require f: reject if f is not available
invalidate f: remove f from available; add f to invalidated; create obligation when registry maps f to a verifier
produce f: add f to available; remove f from invalidated
preserve f: reject if f was not available before the pass
successful verifier for facts F: add F to available; remove F from invalidated; discharge their obligations
failed verifier: keep facts invalidated; fail the boundary
unresolved obligation at a consuming/final boundary: fail closed
```

`P1_INVALIDATION` stops before automatic verifier routing. `P2_SELECTIVE` discharges only created obligations. `P3_ALWAYS` executes all available rules and discharges any matching obligations.

## Boundary route catalog

The current frozen runner represents:

| Boundary | Canonical semantic rule | Implementation |
|---|---|---|
| Bytecode | `core.verifier.bytecode-contract` | production `BytecodeVerifier` |
| AIR / optimized AIR | `core.verifier.air-contract` | production `AirVerifier` |

Backend-input verification is not represented by the historical corpus and must not be counted as available until an executable backend-input route and artifact are added.

## Deterministic invariants

- One runner, corpus, mutation representation, artifact, and oracle serve all policies.
- `P2_SELECTIVE` scheduled rules are a subset of `P3_ALWAYS` rules at the same boundary.
- P2 and P3 use the same canonical owner and verifier implementation for a rule.
- No telemetry field can create, suppress, or discharge an obligation.
- Unknown rules, missing owners, and conflicting owners fail closed.
- Input route/request collections are not mutated.
- Policy output is deterministic under input permutation.
- P2/P3 correctness comparison includes outcome, diagnostic family, and first detection boundary.

## Executable focused tests

`VerificationPolicySchedulerTests` is run before every canonical experiment by:

```bash
 dotnet run -c Release --no-build --no-restore \
   --project UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/UniversalToolchain.ContractExperiments.csproj \
   -- --policy-self-test
```

The gate covers P0/P1 isolation, selective-only routing, P3 full routing, deterministic ordering, request merging, unknown-route rejection, conflicting-owner rejection, immutability, and the P2-subset-of-P3 invariant. Failure prevents evidence generation.
