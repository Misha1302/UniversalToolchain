# 00 — ADR-02: standalone SemanticEvidence, UT as first consumer

**Status:** ACCEPTED for a controlled pilot, 2026-10-20. Supersedes ADR-01's *location* of the kernel inside UT, **not** its trusted inference semantics. Production, public package release and rollout remain **NO-GO** pending integration and portability tests.

## Decision

Implement Level 2 and Level 3 as **one independent .NET library** with opt-in typed rules:

- **L2:** trusted/authorized root judgments in an isolated session, with subject, scope, semantic context and revision binding.
- **L3:** derived judgments constructed exclusively by **trusted C# rules** under checked application preconditions, with dependency links forming a bounded in-memory DAG.
- No universal Proof Checker is needed under this **trusted-rule axiom**. It would become relevant for *untrusted serialized proofs* or external, unchecked rule implementations.
- A bare derived fact is **not** an arbitrary permission to rewrite IR.

Split implementation into **three distinct responsibilities**:

1. `SemanticEvidence.Core` — general-purpose in-memory facts, provenance DAG, revocation/expiry, typed context/scope/subject keys, bounded rule execution and queries. **No reference to UT, SSA, AIR, Wist, LanguagePlan, or IR mutation.** The library manages logical applicability inside its supplied context, not the authenticity of external context metadata.
2. `SemanticEvidence.Rules.Numeric` (optional second package for v1) — int32 interval/affine rules, checked overflow and explicit arithmetic model. Depends only on Core. Do not expose a general dynamic rule-code registry to arbitrary callers in v1.
3. `UniversalToolchain.SemanticEvidence` — UT-only adapter and source authority binding to actual selected `LanguagePlan`, SSA subjects and immutable snapshots; owner-controlled **Rewrite Gate + atomic IR commit**. Depends on Core/Rules and existing UT modules. Core must **never depend back** on this adapter.

Keep a single trusted selected `LanguagePlan`; no second optimizer planner, no global fact DB, no own plugin runtime, no general theorem prover, no direct plugin write access to IR publication.

## Why this boundary

| Alternative | Advantages | Cost / risk | Result |
|---|---|---|---|
| A. Direct typed C# service per compiler | Fewest abstractions; baseline for correctness/performance | Custom provenance and reuse per host | Keep as mandatory **benchmark** |
| B. Entire Level 2/3 inside UT | Simplest immediate integration | Tightly couples knowledge representation to SSA and UT lifecycle | **Reject for architecture**; may be useful as short-lived spike only |
| C. Portable core + UT adapter | Reuse in analyzers/other hosts; clear reusable fact semantics; UT owns its IR | Need strict portability, ownership, data model and ABI tests | **Selected pilot** |
| D. General-purpose framework/plugin planner | More convenient for many extensions eventually | Duplicated discovery/DI, bigger TCB, versioning burden | **Defer** |
| E. Arbitrary proof certificate checker | Checks separately supplied proofs without trusting producer code | Greater complexity and different trust model | **Defer** |

**Important:** This is a design preference, *not empirical proof* that the separate library has lower maintenance cost than embedding it into UT. Require a non-UT independent consumer and measured UT integration before publishing a stable public package.

## Ownership / trust model

- Host authenticates which source implementation is selected and is permitted to assert which root predicates. The library cannot infer that from a caller string, `Type` alone, or `IsTrusted` flag.
- Host-approved inference rule assembly/code belongs to the trusted computing base. At runtime, the library checks **correct application** (types/subject/scope/context/overflow); it does not re-prove the rule's mathematical theorem.
- Context and subject keys are opaque *values* bound by host adapters. The portable library enforces equality and validity in the hosted session but cannot independently establish that an SSA ID maps to the intended instruction.
- The host owns the immutable input IR snapshot and final publication. An external library must not mutate UT IR or mint a UT commit permit.
- This is **not malicious in-process plugin isolation**; CLR co-resident malicious code is outside the guarantee. Debugging bugs in trusted rules still requires tests and reviews.

## Frozen scope

- One session per compilation or function; bounded deterministic parent graph; no cross-build cache initially.
- Closed rule set: same-subject interval intersection; typed affine `int32` range with checked overflow; fact-level fixed-bound implication (not edit permission).
- First UT rewrite: one selected pure, signed-Int32 comparison in an explicitly supported SSA subset. Unsupported facts/semantics => `Unknown` / no commit.
- Default `Disabled`; separate shadow-only UT mode before guarded mode.

## Gates

**G1 — PORTABLE:** Core + Numeric build with zero references to any `UniversalToolchain.*` assembly/package/namespace; a small **second non-UT host** executes P1→P2→Q on the same public contracts.

**G2 — HOST BINDING:** Real UT selected plan controls root admission and snapshot identity; spoofed producers and stale facts fail closed.

**G3 — SAFE COMMIT:** Only the selected proof-enabled UT route can commit the one recognized rewrite; exact-world/revision re-check, no partial publish, preserve original on failure.

**G4 — BENEFIT:** Real source-to-backend differential (or transparently scoped lower-level oracle) and honest direct-C# baseline; assess performance and integration complexity.

Stop/reassess if portable core requires pervasive UT abstractions, one unguarded publication bypass exists in declared scope, any false permit appears, or no operational benefit over A is measurable.
