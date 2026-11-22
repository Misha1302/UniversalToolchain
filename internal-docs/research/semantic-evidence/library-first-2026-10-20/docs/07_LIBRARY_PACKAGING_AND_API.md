# 07 — Portable framework packages, API constraints and distribution

## Package topology (the actual design change)

```text
SemanticEvidence/                           # standalone solution/repository-capable subtree
  src/SemanticEvidence.Core/                 # library, no host/compiler dependencies
  src/SemanticEvidence.Rules.Numeric/        # optional closed trusted numeric rules
  tests/SemanticEvidence.Core.Tests/
  tests/SemanticEvidence.Rules.Numeric.Tests/
  samples/ToyHost/                           # MUST NOT reference UniversalToolchain
  samples/IndependentP1/
  samples/IndependentP2/
  samples/IndependentQ/

UniversalToolchain/                          # existing repo; only integration-specific changes
  ...existing runtime/SSA projects...
  UniversalToolchain.SemanticEvidence/       # host mapping, admission, guarded rewrite
  Tests/SemanticEvidenceIntegration/         # source/backend+P1/P2/Q regression
```

Folder names are provisional; **dependency direction and ownership are binding**. For initial delivery, keep standalone projects as a subtree or local sibling with explicit project references; move to a separate repository/NuGet only after portable API stabilizes and genuine two-host evidence exists. A second GitHub repo is **not** a prerequisite.

## Required public core surface

- Read-only typed `Judgment<TPredicate>` and `FactQuery<T>`; library-owned `FactHandle` and `DerivationInfo` (rule ID/version, immutable parents).
- Context-agnostic `ContextKey`, `RevisionKey`, `ScopeKey`, `SubjectKey` with strict exact equality, typed domain/semantic profile labels and a host-provided identity binding token. No `SsaValueId`, `AirOperation`, `LanguagePlan` or public `IrStageContract`.
- Host-authorized root admission: avoid exposing unconstrained `session.AddVerifiedRoot` to arbitrary producer-facing API. Actual selected host owns capability construction. Within same process this is a correctness API, not a sandbox against malicious reflection.
- Trusted-rule invocation interface restricted to a host-owned selected closed catalog. Rules can create derived nodes only from live handles + inspected typed premises. Graph invalidation and context advancement have owner-only entry points.
- Facts are **read-only** to consumer Q. A query result does not include a UT write permit, candidate mutation or backend execution handle.
- Explicit outcome enum/union: Known / Unknown / Conflict / Stale / Unsupported / Exhausted; avoid silent Boolean coercions.

## What belongs to Rules.Numeric

The portable Numeric package needs an explicitly selected mathematical domain: signed 32-bit **non-overflowing** arithmetic with checked endpoints; record rule IDs and parent handles. If the host language wraps Int32 or throws on overflow, adapter supplies only premises compatible with the chosen domain and refuses unsupported cases. Test `int.MaxValue` and negative endpoints. Avoid assuming one language's arithmetic semantics for every consumer.

## What belongs to UT adapter

- Materialization of `ContextKey` from **actually selected** `LanguagePlan`, selected module/assembly identities, target semantic policy, rule-set version.
- Mapping of `SubjectKey` to **actual immutable SSA snapshot**: function/block/instruction identity, type, dominance/scope and ownership generation.
- Mapping RootProducer registration to selected-plan-approved `RootAdmission` capability; prevention of provider metadata impersonation by comparing actual selection identity.
- Observing world/epoch advances and revoking session handles; no source evidence reuse across plan or selected module changes.
- A **host-owned** guarded rewrite: check candidate, bounded semantic predicate, SSA structural verifier, at-most-once publication under selected owner lock/CAS; no public arbitrary rewrite gate in Core.

## ABI stability and versioning

During pilot: mark Core as experimental; do not commit to stable SemVer, public NuGet availability or binary plugin compatibility. Nonetheless test Q DLL immutability across independent P2 addition, deterministic package graphs and no retroactive breaks to existing profile defaults. When deciding to publish, require compatibility tests for prior public assemblies, package license/README, deterministic build/SBOM/security review and CI on supported platforms.

## Library vs framework

**Library-first; framework-capable.** No custom scheduler, DI container, plugin discovery, RPC, serialized graph engine or automatic global rule execution in v1. A future convenience layer may exist, but it must be optional and host-owned. At this stage the output is reusable inference functionality plus adapters, not a platform that replaces UT's compilation architecture.

## Architecture acceptance metrics

- `Core` and `Numeric` project references contain zero UT dependencies; clean build without UT checkout and without accidentally inherited Directory.Build.props from UT.
- Reflection/source scan of public API finds zero UT type names, `TryCommit`, `RewriteProposal`, `SsaSnapshot` or `LanguagePlan`.
- Second host uses same exact library build artifacts and applies independent subjects/scopes; this is stronger than compiling a renamed `Contracts` assembly.
- UT integration copies **zero** inference logic from Core; all special UT transformation logic resides adapter-side.
- P1 and Q compile before P2; Q byte hash unchanged, no central registration branch and no forced recompile.
