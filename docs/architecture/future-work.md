---
title: Future work without pre-LangDev architecture expansion
description: Explicitly deferred architecture work and its activation triggers.
audience: maintainers, reviewers
navigation: hidden
status: proposed documentation-only hardening
---

# Future work without pre-LangDev architecture expansion

This document intentionally separates credible future directions from current guarantees. None of these items is required to defend the current LangDev story. They should not be implemented before LangDev unless current evidence exposes a real correctness bug that cannot be handled by documentation, examples or characterization tests.

## Performance-triggered work

### Planner cache

- Problem: repeated planning of identical large package sets may become expensive.
- Current behavior: planning cost is paid before runtime materialization; no general persistent cache should be claimed unless implemented and measured.
- Why not implemented now: no public measurement package demonstrates that planning dominates the intended workflow.
- Trigger: reproducible benchmark showing planning cost is material for realistic repeated builds.
- Candidate solution: content-addressed plan cache keyed by package identities, definition, policy and canonicalization version.
- Risks: stale cache bugs, hidden invalidation policy, new versioning surface.

### Route optimization

- Problem: many artifact transformations may create large route search spaces.
- Current behavior: route selection should remain deterministic and inspectable.
- Why not implemented now: optimizing an unmeasured search path risks adding solver complexity prematurely.
- Trigger: measured route-resolution bottleneck at 100/1000+ contributions or real package ecosystems.
- Candidate solution: bounded route graph indexing and explainable tie-break diagnostics.
- Risks: less obvious diagnostics, policy hidden in heuristics.

## Ecosystem-triggered work

### Richer version solving

- Problem: independent packages may eventually require semantic version constraints, compatibility ranges and migrations.
- Current behavior: package/version identity should be treated as evidence/provenance, not a full dependency-management claim.
- Why not implemented now: UniversalToolchain should not become a package manager without ecosystem pressure.
- Trigger: real third-party packages with incompatible versions and current manual resolution becomes a blocker.
- Candidate solution: narrow compatibility metadata over explicit language/toolchain contracts.
- Risks: dependency-manager complexity, confusing NuGet/package-manager overlap.

### Plugin isolation

- Problem: hostile or buggy extensions can execute code in-process.
- Current behavior: valid plans are not sandboxing.
- Why not implemented now: process/OS isolation is a separate threat-model decision.
- Trigger: untrusted extension execution becomes a supported product scenario.
- Candidate solution: out-of-process worker, OS sandbox, resource quotas, signed-package policy.
- Risks: performance cost, debugging complexity, security claims requiring serious audit.

## Explainability-triggered work

### PlanningReport

- Problem: users may need a human-readable explanation of why a plan selected or rejected components.
- Current behavior: `LanguageBuildResult`, diagnostics, `LanguagePlan` and lock projection are the source of truth.
- Why not implemented now: a separate report object risks becoming a second composition model.
- Trigger: repeated user confusion that cannot be addressed through existing diagnostics and docs.
- Candidate solution: generated projection from typed plan/diagnostics only.
- Risks: report drift, API bloat, false sense of semantic proof.

## Scale-triggered work

### Incremental planning

- Problem: planning the entire package graph after small edits may become slow at ecosystem scale.
- Current behavior: full planning is simpler and easier to verify.
- Why not implemented now: no evidence that current scale requires incremental invalidation.
- Trigger: realistic workspace benchmark where full planning prevents acceptable feedback loops.
- Candidate solution: dependency-aware invalidation over explicit plan inputs.
- Risks: invalidation bugs, hard-to-explain stale state.

### Three-repository split

- Problem: Wist, UniversalToolchain and PlanFuzz boundaries may become harder to enforce as the codebase grows.
- Current behavior: boundary should be enforced by documentation and dependency checks first.
- Why not implemented now: physical split before LangDev would create packaging and coordination risk without improving the talk claim.
- Trigger: repeated accidental cross-dependencies or release cadence conflict.
- Candidate solution: UniversalToolchain, Wist and PlanFuzz repos with package-level integration tests.
- Risks: operational overhead, broken local dev flow, premature release-management burden.

## Research work

### PlanFuzz comparative evaluation

- Problem: configuration-aware testing sounds useful, but its value relative to simpler baselines needs evidence.
- Current behavior: PlanFuzz should be positioned as research/testing that consumes explicit plans.
- Why not implemented now: building comparative experiments should not contaminate production APIs.
- Trigger: claim needs to move from plausible research direction to evidence-backed result.
- Candidate experiment: equal-budget comparison between program-only fuzzing, random configuration sampling, pairwise configuration sampling and PlanFuzz.
- Risks: biased benchmarks, overfitting to Wist, pressure to expose private planner state.

## Tactics decision table

| Tactic | Problem it would solve | Complexity introduced | Needed before LangDev? | Decision |
| --- | --- | --- | --- | --- |
| PlanningReport | human-readable explainability | possible second composition model | no | TODO, projection only |
| SAT solver | complex constraint solving | solver semantics, debugging, perf | no | REJECT |
| Persistent planner cache | repeated planning cost | invalidation/versioning | only after measurements | TODO/FUTURE |
| Explicit concurrency model | provider/session safety contracts | public API and compatibility burden | no | TODO |
| Three-repo split | ownership enforcement | packaging/release overhead | no | TODO, not before LangDev |
| More generic abstractions | potential second consumers | framework bloat | no | REJECT unless concrete second consumer exists |
| Source generators | AOT/trimming ergonomics | generated-code API surface | no | REJECT until deployment evidence |
| Dependency solver | package ecosystem conflicts | package-manager behavior | no | TODO only after ecosystem pressure |
