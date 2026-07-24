# PlanFuzz Phase 0–1 implementation status

Status: implemented experimental research tooling, not a public package or a shipped Wist feature.

Branch stack:

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
| `UniversalToolchain.PlanFuzz.Adapter.Wist` | structured restricted-`Int32` generator, interpreter/compiler variants, SSA `Disabled`/`Prefer`/`Require` policies and Wist-owned route evidence |
| `UniversalToolchain.PlanFuzz.Cli` | explicit adapter registration, generation, isolated workers, replay, bounded campaigns and recursive artifact manifests |
| `UniversalToolchain.PlanFuzz.Tests` | deterministic contracts, serialization, oracle behavior, class-fingerprint separation and direct adapter tests |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | fresh-process Acme replay and a clean Wist external-parameter SSA control |

The configuration-complete research graph is declared in `UniversalToolchain/PlanFuzz.sln`. Canonical Bash and PowerShell entrypoints build it alongside `Wist.sln` and use the shared test manifest.

## Current contracts

- PRNG: `xoshiro256starstar-v1` with SHA-256 domain-separated forks.
- Testcase schema: version 1 with canonical body hashing and recorded case identity.
- Observation schema: version 2; schema-v1 observations remain readable.
- Replay report schema: version 2.
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
- A replay is clean only when every declared oracle returns `Passed`; `NotApplicable` and `Inconclusive` are not silently accepted.
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

Values are deliberately bounded to avoid overflow in the valid deterministic profile. Every testcase compares:

```text
interpreter + SSA Disabled
compiler    + SSA Disabled
compiler    + SSA Prefer
compiler    + SSA Require
```

The generic core contains no Wist syntax, feature IDs, backend classes or diagnostic allowlist. Wist-specific fallback classification remains inside `UniversalToolchain.PlanFuzz.Adapter.Wist`.

## Focused verification

Observed locally with the repository's .NET 10/offline package sidecar:

```text
PlanFuzz solution build: 0 warnings, 0 errors
UniversalToolchain.PlanFuzz.Tests: 16 passed
UniversalToolchain.PlanFuzz.IntegrationTests: 4 passed
clean external-parameter SSA control (`x + 3`): 2/2 fresh-process attempts clean
curated issue #302 replay: 3/3, stable exact fingerprint
curated issue #303 replay: 3/3, stable exact fingerprint
curated issue #307 replay: 3/3, stable exact fingerprint
full canonical `./build.sh --skip-docs`: passed
canonical tests: 1431 passed, 0 failed, 0 skipped
package matrix: 9 packages verified
clean template and cross-package consumer smokes: passed
```

The strict Wist pilot used seed `20260724`, 25 cases and three fresh worker processes per case:

```text
4 clean cases
21 confirmed violating cases
2 normalized finding classes
0 flaky cases
0 infrastructure failures
```

Twenty-one violating testcases do **not** mean twenty-one defects. The two normalized classes are also only triage groups:

1. interpreter success versus compiler failure with `air.stack.invalid` route evidence — includes minimized issues #302 and #303;
2. unclassified `ssa.operation.descriptor.missing` fallback/failure — includes issue #307.

The machine-readable record is [phase1-wist-pilot-summary.json](evidence/phase1-wist-pilot-summary.json). Raw replay trees remain a separate research artifact rather than source-tree content.

## Seeded and real-evidence boundary

`SF-001-wrong-backend-arithmetic` changes only the test-owned compiled Acme implementation. It validates discovery and confirmation but is never counted as a UniversalToolchain defect.

The Wist Phase 1 pilot uses real current implementation behavior. Its first two cases are curated regressions for already opened issues #302 and #303; the third is the minimized #307 trigger. Their inclusion protects reproducibility but is not counted as independent rediscovery.

No publication claim follows from these results. Root-cause confirmation, regression fixes, reducer output and controlled baseline comparisons remain required.

## Not yet implemented

- negative-surface and extension-noninterference oracles;
- lifecycle/session/concurrency schedules;
- testcase and plan reduction;
- order-dependent plan, worker-hang and Wist optimizer seeded faults;
- equal-budget program-only/pairwise/full-PlanFuzz comparison;
- third external adapter and publication-scale clean-machine evaluation.

## Next mergeable milestone

1. Land the proposal, Acme core and Wist Level 0 stack with green canonical CI.
2. Implement multidimensional reduction while preserving exact fingerprints.
3. Add negative-surface and lifecycle traces without widening the public Wist package surface.
4. Fix and regression-protect #302, #303 and #307, then rerun the same preserved pilot.
