# Architecture and research proposals

This directory contains design proposals, research directions and rollout plans.

Proposal documents are not implemented behavior by default. A proposal becomes
current architecture only after the implementation lands, tests cover it, and
the current-state documents are updated.

## Active proposals

| Proposal | Purpose | Status |
|---|---|---|
| [PlanFuzz](planfuzz/README.md) | Configuration-aware differential testing across programs, language plans, routes, backends and runtime lifecycles | Proposed research direction |
| [Typed module contracts and verifiers](typed-module-contracts-and-verifiers.md) | Typed ownership, fact/effect and verifier contracts for compiler modules | Design proposal |
| [Flame SSA optimizing backend](flame-ssa-optimizing-backend-design/index.md) | Target architecture and rollout plan for an SSA-oriented optimizing backend | Design proposal |

## Required promotion path

1. Implement through existing architecture boundaries.
2. Add or update fitness tests that protect the new rule or invariant.
3. Update `docs/CURRENT_ARCHITECTURE_STATUS.md`.
4. Link the implemented design from `docs/DOCUMENTATION_INDEX.md`.
5. Preserve reproducible evidence for any performance, correctness or research claim.
