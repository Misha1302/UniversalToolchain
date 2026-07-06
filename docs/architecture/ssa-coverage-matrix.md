---
title: SSA Coverage Matrix
description: Current supported and unsupported AIR to SSA shapes.
---

# SSA Coverage Matrix

Status: preview infrastructure with oracle-first expansion.

This page records the current SSA route as implemented in `UniversalToolchain.Ssa.*`.
It is a coverage contract, not a promise that SSA is the default optimizer/backend layer.

## Current Route

| Area | Status | Evidence |
|---|---|---|
| AIR artifact input | Supported for `AirArtifact` only | `AirToSsaConverter.Run` rejects non-AIR artifacts |
| Structural AIR verification before lowering | Supported | `StructuralAirVerifier` is called before conversion |
| Structural SSA verification after lowering | Supported | `StructuralSsaVerifier` is called before returning SSA |
| `Nop`, `Label`, `Annotate` | Supported as no-op SSA-lowering metadata | `AirToSsaConverter.LowerInstruction` |
| `Push bool` | Supported | lowers to `ssa.const.bool` |
| `Push int` | Supported | lowers to `ssa.const.i32` |
| `Push double` | Supported | lowers to `ssa.const.f64` |
| `Drop` | Supported with stack-underflow diagnostic | `air.to-ssa.stack-underflow` |
| `Intrinsic` with descriptor/callable mapping | Supported for registered shapes | `LowerIntrinsic` and managed-callable tests |
| Branch terminators | Preview-supported through CFG/block lowering | `SsaRoundtripRouteTests` |
| Zero or one return value | Supported | `air.to-ssa.return-arity` rejects multi-return |
| Multiple incompatible return types | Rejected | `air.to-ssa.return-type` |
| Unsupported push value type | Rejected | `air.to-ssa.push-type` |
| Unsupported opcode | Rejected | `air.to-ssa.opcode` |
| SSA to AIR emission | Preview-supported for current SSA subset | `SsaToAirConverter` tests |
| SSA local constant folding | Preview-supported with facts | `SsaConstantFoldingPass` tests |

## Not Production-Ready Yet

| Gap | Required next gate |
|---|---|
| Complete AIR intrinsic coverage | Differential tests per intrinsic family |
| Full runtime value type mapping | Type mapping table and verifier tests |
| Multi-return SSA shapes | Explicit function signature model and AIR lowering policy |
| Arbitrary SSA scheduling | Dominance/use verification and emission legality tests |
| SSA-native backend | Separate backend contract or proven AIR-lowering contract |
| Optimizer suite beyond local constant folding | Required/preserved/invalidated facts per pass |

## Expansion Rule

Do not add broad SSA conversion or optimization support before adding oracle coverage:

1. Add a focused unsupported-shape or differential test.
2. Add the matrix row or update the status.
3. Implement conversion or optimization.
4. Verify AIR route and AIR -> SSA -> AIR route produce equivalent observable behavior for the supported case.

