# SSA route correctness release note - 2026-07-04

This release finishes the no-optimization SSA pre-release route on top of the
callable-first baseline archive:

- baseline archive SHA-256:
  `693e4ff46e2bb099c7571061f9b4ae035ea3109bf2b09eaeca79708bb6169394`;
- route scope: `AIR -> SSA -> AIR`;
- optimization scope: unchanged, no new optimization pass is introduced;
- verified focused SSA tests: 70/70;
- verified full `Tests.csproj`: 379/379.

## Correctness fixes

- Callable lowering now chooses the best supported target priority bucket and
  reports ambiguity only when more than one supported target exists in that
  same bucket.
- Managed-call lowering validates the complete descriptor signature against the
  resolved managed member descriptor, including parameter and result type
  sequences.
- The roundtrip route has explicit `Off`, `Prefer`, `Require` and `Debug`
  policies for no-optimization routing.
- Alpha arithmetic and managed-call support are now selected through explicit
  SSA route profiles instead of being hidden in default AIR/SSA converters.

## Release surface cleanup

- Removed the unsupported `LogsViewer` UI from the release package.
- Removed `ExecutorLoggerModule` and `logs.txt` tests from the public solution
  surface.
- Added the Debug Trace v2 decision record and planned schema reference.
- Kept current diagnostics, verifier and observer infrastructure as the
  supported near-term debugging surface.

## Regression coverage

- same-priority supported targets produce an ambiguity diagnostic;
- different-priority supported targets select the best priority target;
- mismatched managed descriptor types produce
  `ssa.to-air.managed-call-descriptor.shape`;
- CIL-only targets are rejected before AIR emission;
- unsupported stack reuse remains rejected by the minimal AIR emitter;
- supported debug roundtrip preserves the expected AIR instruction shape.

## Still outside this release

- full SSA scheduling for arbitrary stack shapes;
- executable CIL and interpreter target routes;
- broader CLR type mapping beyond bool, int32, float64 and managed object
  references;
- performance claims or new optimization behavior.
