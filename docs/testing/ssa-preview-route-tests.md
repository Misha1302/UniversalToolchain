# SSA preview route tests

The preview SSA route is expected to run profile-owned optimization passes between `AIR -> SSA` lowering and `SSA -> AIR` emission.

The focused roundtrip tests protect two separate contracts:

- `SsaRouteFactory.CreateRoundtripRoute(profile)` must use the profile optimizer pipeline before emission.
- Raw `new SsaRoundtripRoute(lowerer, emitter)` remains a plain roundtrip path and does not run optimizers implicitly.

This distinction matters because the preview arithmetic profile now owns the safe pass sequence:

1. `SsaConstantFoldingPass`
2. `SsaBranchFoldingAndCleanupPass`
3. `SsaDeadPureInstructionEliminationPass`

The route-level optimization test intentionally checks the emitted AIR shape for `2 3 add`: after SSA optimization it should emit a single `Push 5` after the entry label, not the original `Push 2`, `Push 3`, `Intrinsic add` sequence.
