# PlanFuzz Phase 0–1 implementation status

Status: implemented experimental research tooling, not a public package or a shipped Wist feature.

```text
PlanFuzz proposal
-> Phase 0: language-neutral core and Acme vertical slice
-> Phase 1: Wist restricted Int32 matrix and route/fallback evidence
```

The implementation remains non-packable and does not extend the public `UniversalToolchain.Wist` or NuGet package surface.

## Implemented projects

| Project | Responsibility |
|---|---|
| `UniversalToolchain.PlanFuzz.Core` | deterministic PRNG, versioned testcase/observation contracts, exact replay fingerprints, normalized finding-class fingerprints, adapter/oracle registries and five generic oracle families |
| `UniversalToolchain.PlanFuzz.Adapter.Acme` | independent structured pricing generator, registry-order variants, interpreter/compiled execution and test-only wrong-arithmetic fault |
| `UniversalToolchain.PlanFuzz.Adapter.Wist` | structured restricted-`Int32` generator, opt-in regression corpus, interpreter/compiler variants, SSA `Disabled`/`Prefer`/`Require` policies and Wist-owned route evidence |
| `UniversalToolchain.PlanFuzz.Cli` | explicit adapter registration, generation, isolated workers, replay, bounded campaigns and recursive artifact manifests |
| `UniversalToolchain.PlanFuzz.Tests` | deterministic contracts, serialization, oracle behavior, evidence-completeness checks, class-fingerprint separation and direct adapter tests |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | fresh-process Acme/Wist replay and CLI outcome-boundary tests |

The configuration-complete research graph is declared in `UniversalToolchain/PlanFuzz.sln`. Canonical Bash and PowerShell entrypoints build it alongside `Wist.sln` and use the shared test manifest.

## Current contracts

- PRNG: `xoshiro256starstar-v1` with SHA-256 domain-separated forks.
- Testcase schema: version 1 with canonical body hashing and recorded case identity.
- Observation schema: version 2; schema-v1 observations remain readable.
- Replay report schema: version 3 with a distinct `inconclusive` state.
- Campaign summary schema: version 3; it reports `distinctFindingClasses`, `inconclusiveCases` and whether the regression corpus was included.
- Typed values: `decimal`, `bool`, `string` and `Int32` snapshots without semantic comparison through `ToString()`.
- Generic oracles:
  - `O-001` backend parity;
  - `O-002` optimization/route parity;
  - `O-003` plan determinism;
  - `O-006` controlled fallback;
  - `O-009` canonical lock consistency.
- Wist route evidence records policy, route use, fallback state/classification, profile, instruction counts, executed passes and stable diagnostic code/stage pairs.
- Exact fingerprints preserve testcase-level evidence and remain authoritative for repeated replay confirmation.
- Coarser class fingerprints remove concrete values and duplicate diagnostics for campaign triage. They are not root-cause identities and are never reported as unique defects without manual analysis.
- A replay is clean only when it has at least one oracle result and every declared oracle returns `Passed`.
- A violation is confirmed only when every attempt has at least one violation, no infrastructure failure, no `Inconclusive`/`NotApplicable` result, and one stable exact fingerprint.
- Incomplete oracle evidence is reported as inconclusive, not clean or flaky.
- Replay and campaign output roots must be empty, and every result tree receives a recursive `MANIFEST.sha256`.

## Wist Level 0 scope

The adapter owns a structured expression model rather than reparsing source text:

```text
IntExpression := Constant
               | ExternalParameter(x)
               | Add
               | Subtract
               | Multiply
```

The only external parameter is exactly `x`. Backend/configuration pairs are validated fail-closed before execution. Values are deliberately bounded to avoid overflow in the valid deterministic profile. Every testcase compares:

```text
interpreter + SSA Disabled
compiler    + SSA Disabled
compiler    + SSA Prefer
compiler    + SSA Require
```

The generic core contains no Wist syntax, feature IDs, backend classes or diagnostic allowlist. Wist-specific fallback classification remains inside `UniversalToolchain.PlanFuzz.Adapter.Wist`.

## Discovery versus regression verification

Default Wist generation is discovery-only. It does not prepend known issue triggers.

Known minimized cases for #302, #303 and #307 are available only through the explicit CLI option:

```text
--include-regressions
```

Adapters that do not advertise the `regression-corpus` capability reject that option instead of silently ignoring it. Campaign summaries record whether the corpus was included.

## Preserved Phase 1 pilot

The preserved pilot used seed `20260724`, 25 cases, three fresh worker processes per case, and **included the regression corpus**:

```text
4 clean cases
21 confirmed violating cases
2 normalized finding classes
0 flaky cases
0 infrastructure failures
```

Twenty-one violating testcases do **not** mean twenty-one defects. The two normalized classes are only triage groups:

1. interpreter success versus compiler failure with `air.stack.invalid` route evidence — includes minimized issues #302 and #303;
2. unclassified `ssa.operation.descriptor.missing` fallback/failure — includes issue #307.

The machine-readable record is [phase1-wist-pilot-summary.json](evidence/phase1-wist-pilot-summary.json). Raw replay trees remain a separate research artifact rather than source-tree content. The old pilot is preserved for reproducibility, not presented as a clean discovery-yield experiment.

## Seeded and real-evidence boundary

`SF-001-wrong-backend-arithmetic` changes only the test-owned compiled Acme implementation. It validates discovery and confirmation but is never counted as a UniversalToolchain defect.

The Wist regression corpus preserves the historical triggers for #302, #303 and #307. Their root causes are now confirmed and the current source tree contains owner-layer fixes plus direct regression tests. These cases validate non-regression and do not count as independent rediscoveries. No publication claim follows from these results; reducer output and controlled baseline comparisons remain required.

## Post-fix regression verification

The repaired source state addresses the three tracked behaviors without weakening PlanFuzz or expanding fallback:

- **#302:** the arithmetic peephole no longer treats the slot operand inside a multi-instruction external-load sequence as a complete arithmetic operand; zero-multiplication folding requires the opposite side to be a proven single-value producer.
- **#303:** typed zero materialization boxes the `i32` branch before switch-expression numeric unification, preserving `System.Int32` instead of widening it to `System.Int64`.
- **#307:** dynamically reconstructed SSA verifiers retain all core operation descriptors when managed-call descriptors are added, so `core.external.load.i32` remains available through lowering and emission.

Direct owner tests and Wist public-path PlanFuzz tests cover all three fixes. Two bounded smoke campaigns used seed `20260724`, three cases and three fresh-process attempts per case:

```text
discovery-only:       3 clean, 0 findings, 0 flaky, 0 inconclusive, 0 infrastructure failures
regression-inclusive: 3 clean, 0 findings, 0 flaky, 0 inconclusive, 0 infrastructure failures
```

These are deterministic post-fix smokes, not publication-scale discovery or baseline evidence.

## Verified integration gate

GitHub Actions executed the canonical repository entrypoint after evidence hardening:

```text
Tests:                                       483 passed
UniversalToolchain.Modules.Tests:            290 passed
UniversalToolchain.Dialects.Tests:           588 passed
UniversalToolchain.LanguageSdk.Tests:         53 passed
UniversalToolchain.PlanFuzz.Tests:             26 passed
UniversalToolchain.PlanFuzz.IntegrationTests:   6 passed
--------------------------------------------------------
Total:                                      1446 passed
Failed:                                        0
Skipped:                                       0
```

The same gate completed Release builds with zero warnings and errors, verified nine packages, checked the Wist package surface and passed clean template and cross-package consumer smokes.

## Not yet implemented

- negative-surface and extension-noninterference oracles;
- lifecycle/session/concurrency schedules;
- testcase and plan reduction;
- order-dependent plan, worker-hang and Wist optimizer seeded faults;
- equal-budget program-only/pairwise/full-PlanFuzz comparison;
- third external adapter and publication-scale clean-machine evaluation.

## Next milestone

1. Scale post-fix discovery-only and regression-inclusive campaigns while keeping their evidence separate.
2. Implement multidimensional reduction while preserving exact fingerprints.
3. Add negative-surface and lifecycle traces without widening the public Wist package surface.
4. Run equal-budget program-only, pairwise and full-PlanFuzz baselines after the reducer and additional oracles are stable.
