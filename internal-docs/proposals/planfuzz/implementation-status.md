# PlanFuzz Phase 0–3 implementation status

Status: implemented experimental research tooling, not a public package or a shipped Wist feature.

```text
PlanFuzz proposal
-> Phase 0: language-neutral core and Acme vertical slice
-> Phase 1: Wist restricted Int32 matrix and route/fallback evidence
-> Phase 2: deterministic program/plan reduction with exact-fingerprint confirmation
-> Phase 3: explicit surface/activation evidence and absence/extension oracles
```

The implementation remains non-packable and does not extend the public `UniversalToolchain.Wist` or NuGet package surface.

## Implemented projects

| Project | Responsibility |
|---|---|
| `UniversalToolchain.PlanFuzz.Core` | deterministic PRNG, versioned testcase/observation contracts, exact replay fingerprints, normalized finding-class fingerprints, adapter/oracle registries, seven generic oracle families, explicit surface/activation evidence and language-neutral reduction contracts |
| `UniversalToolchain.PlanFuzz.Adapter.Acme` | independent structured pricing generator and reducer, registry-order and selected-but-unused extension variants, interpreter/compiled execution and three test-only seeded faults |
| `UniversalToolchain.PlanFuzz.Adapter.Wist` | structured restricted-`Int32` generator and reducer, opt-in regression corpus, interpreter/compiler variants, SSA `Disabled`/`Prefer`/`Require` policies and Wist-owned route evidence |
| `UniversalToolchain.PlanFuzz.Cli` | explicit adapter registration, generation, isolated workers, replay, deterministic reduction, bounded campaigns and recursive artifact manifests |
| `UniversalToolchain.PlanFuzz.Tests` | deterministic contracts, serialization, oracle behavior, evidence-completeness checks, class-fingerprint separation, reduction transforms and direct adapter tests |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | fresh-process Acme/Wist replay, exact-fingerprint reduction and CLI outcome-boundary tests |

The configuration-complete research graph is declared in `UniversalToolchain/PlanFuzz.sln`. Canonical Bash and PowerShell entrypoints build it alongside `Wist.sln` and use the shared test manifest.

## Current contracts

- PRNG: `xoshiro256starstar-v1` with SHA-256 domain-separated forks.
- Testcase schema: version 1 with canonical body hashing and recorded case identity.
- Observation schema: version 3 with optional selected-surface and activation evidence; schema-v1 and schema-v2 observations remain readable.
- Replay report schema: version 3 with a distinct `inconclusive` state.
- Campaign summary schema: version 3; it reports `distinctFindingClasses`, `inconclusiveCases` and whether the regression corpus was included.
- Typed values: `decimal`, `bool`, `string` and `Int32` snapshots without semantic comparison through `ToString()`.
- Generic oracles:
  - `O-001` backend parity;
  - `O-002` optimization/route parity;
  - `O-003` plan determinism;
  - `O-004` negative-surface preservation;
  - `O-005` extension noninterference;
  - `O-006` controlled fallback;
  - `O-009` canonical lock consistency.
- Wist route evidence records policy, route use, fallback state/classification, profile, instruction counts, executed passes and stable diagnostic code/stage pairs.
- Surface evidence records the selected and excluded surface, explicitly independent additions, activated owners, trace completeness, evidence-contract identity and selected route identity.
- `O-004` never passes an incomplete trace and detects any `excluded ∩ activated` owner.
- `O-005` applies only to a pure additive, explicitly independent surface delta and requires unchanged semantics, route identity and activation owners.
- Exact fingerprints preserve testcase-level evidence and remain authoritative for repeated replay confirmation.
- Coarser class fingerprints remove concrete values and duplicate diagnostics for campaign triage. They are not root-cause identities and are never reported as unique defects without manual analysis.
- A replay is clean only when it has at least one oracle result and every declared oracle returns `Passed`.
- A violation is confirmed only when every attempt has at least one violation, no infrastructure failure, no `Inconclusive`/`NotApplicable` result, and one stable exact fingerprint.
- Incomplete oracle evidence is reported as inconclusive, not clean or flaky.
- Replay, reduction and campaign output roots must be empty, and every result tree receives a recursive `MANIFEST.sha256`.
- Reduction candidates are adapter-owned structured models or generic plan-contract projections; raw-source regex rewriting is not used.
- A candidate is accepted only after complete fresh-process confirmation with the original exact fingerprint. Clean, flaky, inconclusive and infrastructure outcomes are retained as rejected evidence.

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

## Deterministic reduction

Phase 2 adds a bounded `reduce` command:

```text
planfuzz reduce
  --case case.json
  --output artifacts/reduction
  --repeat 3
  --timeout-seconds 30
  --max-candidates 100
```

The generic coordinator never parses a language's source text. Acme and Wist expose deterministic candidates from their structured program models, while the core can remove passing oracle contracts and prune variants that no longer contribute to a violation. Every candidate is replayed in fresh worker processes and is accepted only when the original exact confirmed fingerprint remains unchanged.

The Acme seeded wrong-arithmetic integration case is reducible: the verified smoke evaluated 29 candidates, accepted two steps, preserved fingerprint `e71be55efb8304d2f3491e4de5f4745706a52de0eb728359ea908d1feae3f0a2`, and reduced the plan to the variants and contract required by the violation. Re-running reduction from the same testcase produces the same ordered candidate set and final testcase; this is covered by deterministic unit contracts rather than an additional long-running fresh-process loop in CI.

A clean testcase is rejected as a reduction precondition failure, but still receives `original-case.json`, `reduced-case.json`, `reduction-report.json`, replay evidence and a recursive manifest.

## Expanded post-fix discovery smoke

A larger non-publication smoke used seed `20260725` with the regression corpus disabled:

```text
Acme: 50/50 clean, repeat 2, 0 findings, 0 flaky, 0 inconclusive, 0 infrastructure failures
Wist: 20/20 clean, repeat 1, 0 findings, 0 flaky, 0 inconclusive, 0 infrastructure failures
```

The machine-readable summary is [phase2-reducer-smoke-summary.json](evidence/phase2-reducer-smoke-summary.json). Raw evidence trees were generated with recursive manifests and remain external research artifacts. This smoke establishes post-change stability only; it is not an equal-budget baseline comparison and does not establish superiority or novelty.

## Surface and extension contracts

Phase 3 adds two generic oracle families without adding Acme or Wist identifiers to the core:

- `O-004` negative-surface preservation checks complete activation traces and rejects activation of any explicitly excluded owner;
- `O-005` extension noninterference compares a baseline with a pure additive, explicitly independent extension and requires equal semantics, selected route identity and activated-owner evidence.

The Acme adapter now includes a real selected-but-unused extension contribution on an unreachable artifact route. Baseline variants declare that feature and contribution excluded; extension variants select them and declare them independent. The route-runtime evidence contract derives activated owners from the exact selected transform steps, backend executor owner and runtime-provider owner.

Two test-owned seeded faults validate oracle adequacy in three fresh processes each:

```text
SF-002 excluded-owner activation: confirmed 3/3, exact fingerprint baaf6e71d0845061fbb529edd77dd209fb34828d06ef9edffcc0c01d0a414a02
SF-003 extension interference:     confirmed 3/3, exact fingerprint 2e00f4d3a7184be2f4691e3ed170027f45d14888e086d57f6e50c2b90baa18cb
```

A clean Acme campaign using seed `20260725` completed 25/25 cases with two fresh-process attempts per case and zero findings, flaky, inconclusive or infrastructure outcomes. The machine-readable record is [phase3-surface-oracles-smoke-summary.json](evidence/phase3-surface-oracles-smoke-summary.json). This is bounded stability and oracle-adequacy evidence, not a controlled research baseline.

## Verified integration gate

GitHub Actions executed the canonical repository entrypoint after evidence hardening:

```text
Tests:                                       483 passed
UniversalToolchain.Modules.Tests:            290 passed
UniversalToolchain.Dialects.Tests:           588 passed
UniversalToolchain.LanguageSdk.Tests:         53 passed
UniversalToolchain.PlanFuzz.Tests:             35 passed
UniversalToolchain.PlanFuzz.IntegrationTests:  10 passed
--------------------------------------------------------
Total:                                      1459 passed
Failed:                                        0
Skipped:                                       0
```

The same gate completed Release builds with zero warnings and errors, verified nine packages, checked the Wist package surface and passed clean template and cross-package consumer smokes.

## Not yet implemented

- lifecycle/session/concurrency schedules and schedule reduction;
- order-dependent plan, worker-hang and Wist optimizer seeded faults;
- equal-budget program-only/pairwise/full-PlanFuzz comparison;
- third external adapter and publication-scale clean-machine evaluation.

## Next milestone

1. Add lifecycle/session/concurrency schedules, then extend reduction to the schedule dimension.
2. Add worker-timeout, order-dependent-plan and Wist-optimizer seeded faults and prove they are detected and reducible.
3. Run equal-budget program-only, pairwise and full-PlanFuzz baselines after schedule evidence is stable.
4. Add a third external adapter and clean-machine publication artifact.
