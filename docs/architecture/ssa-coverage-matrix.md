---
title: SSA Coverage Matrix
description: Current supported and unsupported AIR to SSA shapes.
---

# SSA Coverage Matrix

Status: alpha infrastructure with oracle-first expansion.

This page records the current SSA route as implemented in `UniversalToolchain.Ssa.*`.
It is a coverage contract, not a promise that SSA is the default optimizer/backend layer.

## Current Route

| Area | Status | Evidence |
|---|---|---|
| Public Wist opt-in | Supported | `WistEngineOptions.Optimization.Ssa` and facade end-to-end tests |
| Policies | `Disabled`, `Prefer`, `Require`, `Debug` | `SsaRoundtripRouteTests`; only known unsupported diagnostics may fall back under `Prefer` |
| Observability | Supported | `WistOptimizationReport` exposes route, fallback, profile, counts, passes, diagnostics, and trace |
| AIR artifact input | Supported for `AirArtifact` only | `AirToSsaConverter.Run` rejects non-AIR artifacts |
| AIR/SSA/AIR verification | Supported | structural verifiers run before lowering, after lowering/passes, and after emission |
| External parameter values | Supported for current typed subset | AIR external-value references lower to SSA parameters |
| `Push bool/int/double` | Supported | canonical SSA constants |
| `Drop` | Supported with deterministic underflow diagnostics | `air.to-ssa.stack-underflow` |
| Native Wist int32 add/subtract/multiply | Supported and optimizable | `WistNativeArithmeticSsaProjection` maps to canonical callable IDs |
| Registered AIR intrinsics | Supported for registered descriptors | descriptor-driven lowering tests |
| Managed static/instance/constructor calls | Supported for mapped CLR types | exact execution-scoped binding set, no production rediscovery |
| Duplicate equivalent managed bindings | Supported | structural binding equivalence regression |
| Conflicting/missing bindings | Rejected | deterministic lowering/emission diagnostics |
| Branch terminators and block arguments | Alpha-supported | CFG, dominance, block-argument, and route tests |
| Zero or one return value | Supported | multi-return is explicitly rejected |
| SSA to AIR emission | Supported for current legal stack-schedulable subset | `SsaToAirConverter` tests |
| Constant folding | Supported for trusted deterministic descriptors | `SsaConstantFoldingPass` tests |
| SCCP-lite | Supported | block-argument constant propagation tests |
| Local common-subexpression elimination | Supported for trusted deterministic pure expressions within one block | local CSE safety and substitution regressions |
| Branch/unreachable cleanup | Supported | branch folding and cleanup tests |
| Dead pure instruction elimination | Supported | effect-aware elimination tests |
| Extension-pack/pass conflicts | Fail-fast | route profile construction tests |

## Stable Unsupported-Shape Diagnostics

The route rejects unsupported shapes with explicit codes rather than guessing:

- `air.to-ssa.stack-underflow` — an instruction consumes more values than the analyzed AIR stack contains;
- `air.to-ssa.return-arity` — more than one return value is requested;
- `air.to-ssa.return-type` — return paths disagree on the current supported result type;
- `air.to-ssa.push-type` — a pushed CLR/runtime value type has no current SSA mapping;
- `air.to-ssa.opcode` — the AIR opcode is outside the registered lowering surface;
- `air.to-ssa.managed-call.projection.unregistered` — a projection selected a callable with no active semantic descriptor;
- `ssa.optimization.managed-call.binding.missing` and `ssa.to-air.managed-call.binding.missing` — a managed callable lost its execution-scoped binding.

Under `Prefer`, only explicitly classified unsupported-shape diagnostics may trigger a controlled AIR fallback. `Require` and `Debug` preserve the route report and fail instead of silently changing execution strategy.

## Not Production-Ready Yet

| Gap | Required next gate |
|---|---|
| Complete AIR intrinsic coverage | Differential tests per intrinsic family |
| Full runtime value type mapping | Type mapping table and verifier tests |
| Multi-return SSA shapes | Explicit function signature model and AIR lowering policy |
| Arbitrary SSA scheduling | Dominance/use verification and emission legality tests |
| SSA-native backend | Separate backend contract or proven AIR-lowering contract |
| Broader optimizer suite (cross-block GVN, LICM, inlining) | Required/preserved/invalidated facts and differential tests per pass |

## Expansion Rule

Do not add broad SSA conversion or optimization support before adding oracle coverage:

1. Add a focused unsupported-shape or differential test.
2. Add the matrix row or update the status.
3. Implement conversion or optimization.
4. Verify AIR route and AIR -> SSA -> AIR route produce equivalent observable behavior for the supported case.

