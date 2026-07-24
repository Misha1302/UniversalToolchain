# PlanFuzz Phase 0 and Acme implementation status

Status: implemented experimental research tooling, not a public package or Wist product feature.

Baseline: external language-authoring SDK commit `7f2b5819f712d03c39270349b6b39e914b79e008` plus the stacked PlanFuzz proposal branch.

## Implemented projects

| Project | Responsibility |
|---|---|
| `UniversalToolchain.PlanFuzz.Core` | deterministic PRNG, testcase and observation schemas, adapter registry, three generic oracle families and replay contracts |
| `UniversalToolchain.PlanFuzz.Adapter.Acme` | independent structured pricing-language generator, equivalent registry-order variants, interpreter/compiled execution and test-only wrong-arithmetic fault |
| `UniversalToolchain.PlanFuzz.Cli` | explicit adapter registration, testcase generation and inspection, isolated workers, replay, bounded sequential campaigns and artifact manifests |
| `UniversalToolchain.PlanFuzz.Tests` | PRNG golden vectors, serialization identity, direct Acme oracle and seeded-fault tests |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | fresh-process clean replay and stable three-of-three seeded-fault confirmation |

None of these projects is packable. They do not modify the public `UniversalToolchain.Wist` surface or package matrix.

The bounded research graph is declared in `UniversalToolchain/PlanFuzz.sln`. The canonical Bash and PowerShell build entrypoints restore and build both `Wist.sln` and `PlanFuzz.sln` before running the shared test manifest. This keeps experimental projects out of the large historical solution while preserving one observable build gate.

## Current contracts

- PRNG algorithm: `xoshiro256starstar-v1` with SHA-256 domain-separated forks.
- Testcase schema: version 1 with canonical body hashing and recorded case identity.
- Observation schema: version 1 with typed decimal snapshots and plan/lock identities.
- Generic oracles:
  - `O-001` backend parity;
  - `O-003` plan determinism;
  - `O-009` canonical lock consistency.
- Strict replay starts one fresh worker process per testcase attempt, executes the requested variants inside that isolated process, applies a timeout to the attempt, captures bounded process evidence and writes atomic observation files. Variants share the process only within one attempt so backend parity does not pay process-startup cost per variant; repeated confirmations always use new processes.
- Replay and campaign output roots must be empty before execution, preventing stale evidence from being mixed into a new result.
- A finding is confirmed only when every requested replay contains the same violation fingerprint and no infrastructure failure.
- A replay is classified as clean only when every declared oracle returns `Passed`; `NotApplicable` and `Inconclusive` are not silently counted as clean.
- Every replay and campaign directory receives a recursive `MANIFEST.sha256`.

## Seeded fault boundary

`SF-001-wrong-backend-arithmetic` changes only the test-owned compiled Acme implementation from subtraction to addition. It validates discovery and confirmation but is never counted as a real UniversalToolchain defect.

The clean Acme baseline remains present in the same testcase and must continue to pass backend parity. This prevents the seeded fault from hiding a broken baseline.

## Verified locally

The focused verification performed for this branch includes:

```text
UniversalToolchain/PlanFuzz.sln: build succeeded, 0 warnings, 0 errors
UniversalToolchain.PlanFuzz.Tests: 8 passed
UniversalToolchain.PlanFuzz.IntegrationTests: 3 passed
clean strict replay: 2/2 attempts clean
seeded-fault strict replay: 3/3 attempts, one stable fingerprint
clean bounded campaign: 100/100 cases clean, 0 flaky, 0 infrastructure failures
seeded-fault bounded campaign: 10/10 confirmed findings at 3/3 replay
```

A full canonical repository build remains a separate required merge gate. This status page must not be used to upgrade the historical `VERIFICATION.md` baseline.

## Not yet implemented

- Wist adapter and structured Wist generator;
- SSA policy variants;
- negative-surface and controlled-fallback oracles;
- lifecycle schedules and concurrency cases;
- testcase reduction;
- order-dependent plan seeded fault;
- worker-hang seeded fault;
- publication-scale campaigns and external-system evaluation.

## Next mergeable milestone

1. Add deterministic worker-delay and order-dependent plan faults owned by test fixtures.
2. Run a bounded clean Acme campaign and preserve the raw artifact.
3. Add a Wist Level 0 adapter without changing generic core contracts.
4. Add O-002 route parity and O-006 controlled fallback only after SSA applicability is explicit.
