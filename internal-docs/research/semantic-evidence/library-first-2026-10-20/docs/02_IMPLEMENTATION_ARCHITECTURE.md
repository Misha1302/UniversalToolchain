# 02 — Library-first architecture: contracts, authority, and data flow

## 1. Central invariant

A semantic fact means:

`ContextKey ; RevisionKey ; ScopeKey ⊢ Judgment(SubjectKey, Predicate)`

The portable library manages **context-bound logical facts**, not SSA artifacts. The **host** constructs opaque context/snapshot and subject keys from its own real objects. A rule is trusted C# code, so the correctness claim is conditional: roots are true by approved producer contract, trusted rules are sound for their declared semantics, and current applicability is checked. Nothing in this statement proves the correctness of the host, plugins, or generated code.

## 2. Dependency diagram

```text
                                  CONSUMING HOST
  +---------------------------------------------------------------+
  | UniversalToolchain                                             |
  |  LanguagePlan/selected bindings   SSA ownership/backend        |
  |        |                             ^                         |
  |        v                             | host-only commit        |
  |  UniversalToolchain.SemanticEvidence (adapter + Rewrite Gate)   |
  |        | context/snapshot/subjects    | candidate validation    |
  +--------|-----------------------------|-------------------------+
           |                             ^
           v                             | read-only fact result
  +---------------------------------------------------------------+
  |             SemanticEvidence.Core (INDEPENDENT .NET)            |
  | Session / root admission interface / typed facts / dependencies|
  | trusted inference invocation / revocation / bounded DAG        |
  +-------------------------^-------------------------------------+
                            |
                 SemanticEvidence.Rules.Numeric
                (interval / affine / overflow)

  Another independent host (NOT UT): same Core + Rules, own key adapter.
```

**Allowed:** UT adapter → Core; Rules.Numeric → Core; UT adapter → Numeric. **Forbidden:** Core → UT, Numeric → UT, Core → SSA/AIR/Wist/LanguagePlan; any Core-controlled publishing of UT IR. Do not create a second UT discovery registry or planner inside Core.

## 3. Responsibility/ownership table

| Concern | Portable Core / Numeric | UT adapter / current compiler |
|---|---|---|
| Facts, judgment kinds, opaque parent handles, DAG | **Own** | Consumes and maps |
| Generic `ContextKey`/`RevisionKey`/`SubjectKey` equality and rule applicability | **Own comparisons** | **Own authentic construction/mapping** |
| Actual selected `LanguagePlan` and producer registration | No idea what those are | **Own** |
| Root admission | Consume host-minted admission through restricted API | **Own authorization of producer and root predicate** |
| Trusted C# rule correctness | Trust + ordinary tests; Numeric owns arithmetic rules | Trust selected rule implementations, policy and language arithmetic profile |
| World/plan changes, IR epoch and aliasing | Invalidate provided context and reject stale handles | Own epoch, selected-plan binding, snapshot lifetime |
| Effect/overflow/exception semantics | Numeric rules require explicit declared model; no implicit global purity | Supply valid model from language + verified source facts |
| Proposal/permit/atomic SSA publication/rollback | **NO** UT rewrite concepts | **Own** fixed proof-enabled rewrite gate |
| Diagnostics and graph export | Limited read-only metadata and bounded tracing | Translates to UT diagnostics and policies |

### Avoid a hidden generic rewrite API

The old prototype exposes `EvidenceSession.PermitUpperCheckElimination` and `TryCommit`. This is **experimental model-only design**, not the public Core surface. The new Core should return **facts/query results**. The UT adapter checks a closed host-owned rewrite's premises and performs publication atomically. The portable library does not get `SsaSnapshot`, `IIrOptimizationPass`, `RewriteProposal` or `CommitResult` APIs.

## 4. Layered packages and naming

- `SemanticEvidence.Core` — one internal experimental .NET assembly/package. Public contracts minimal, host-agnostic. No dependence on `Microsoft.Extensions.DependencyInjection` at v1; optional composition stays with each host.
- `SemanticEvidence.Rules.Numeric` — separate *optional* package for range/affine/range implication. If small and no practical consumer difference, can initially be a source project referencing Core and only package after validation.
- `UniversalToolchain.SemanticEvidence` — UT-specific project internal to UT solution for first pilot. The actual UT branch/package structure to be chosen after verifying current dependency graph. It may not be a separately published NuGet immediately.

No invented multi-package ecosystem or hard-coded `SemanticEvidence.Hosting` container in v1. Promote to a public package only after reference consumers and compatibility tests succeed.

## 5. Portable domain identities

- `ContextKey`: opaque host-authorized compilation/analysis world identity (target and semantic profile **encoded by host**, not readable `LanguagePlan` fields in Core).
- `RevisionKey`: host-owned generation/version. Must never reuse a live identity for distinct states (ABA protection). Core rejects stale session values.
- `ScopeKey`: typed, host-created scope identity with exact equality (e.g., CFG block, AST region, SQL plan node). No automatic dominance or alias inference.
- `SubjectKey`: opaque host-created typed subject + declared type/model; `x` in one AST/SSA revision is not assumed equal to `x` in another.
- `FactHandle`: session-opaque handle, not a serialized ID, and not a token that grants host write authority.
- `RootFact`: typed judgment plus source admission. All root batches validated before publication.
- `DerivedFact`: typed judgment + trusted rule ID/version and ordered immutable parent handles. Derived nodes are constructed only by library-trusted rule invocation.
- `FactQuery`: `Known(value, handle) | Unknown | Stale | Unsupported | Conflict` (naming illustrative; exact union may change during pilot). Unknown/Conflict never mean true.

Avoid public setters for derived handles, arbitrary `Verified=true`, string-only source identity, user-controlled import of precomputed DAGs, and reflection-loaded arbitrary rule functions.

## 6. Initial rule semantics and non-goals

Implement `IntersectRanges32` only for identical subjects in identical scope/world/revision; refuse empty intersection (report explicit conflict/unknown). Implement `AffineShiftRange32` only when source range and source side of affine relation refer to the same subject and arithmetic is checked for overflow; the target's result must inherit the correct identity/scope. Implement `RangeImpliesUpperBound32` as a **typed semantic predicate** (`i < 8` known true), NOT a generic rewrite permit. Each rule records parent handles and version, not a new theorem proof program.

Explicitly **not** in first implementation: shared memory aliasing, effect inference from arbitrary calls, exceptions/purity proofs, FP/NaN, recursive rules, interprocedural fixpoint, CFG path-join, cross-scope promotion, persistent fact DB, in-process malicious plugin isolation, rule DSL, general proof checker, remote proof import.

## 7. Canonical interaction example

```text
P1 (host-authorized)  --RootFact: Range(x,[0,2])--+
                                               |    library rules:
P2 (host-authorized)  --RootFact: i = x + 5-----+--> Range(i,[5,7])
                                                        |
Q: independent consumer retrieves Range(i) or derives a typed bounded predicate
                                                        |
UT adapter (NOT Core): checks actual SSA comparison i < 8, purity, context,
    selection, epoch, producer/root validity, structural result and host policy
                                                        |
UT owner: atomic publish one candidate; failure returns original IR unchanged
```

Adding P2 must not recompile Q or add a central P2-specific branch. The same public Core contract must work in a second non-UT host with unrelated domain subject names.

## 8. API boundary sketch (proposed, not compiled implementation)

```csharp
// In SemanticEvidence.Core. No UT types anywhere.
public interface IEvidenceSession
{
    ContextKey Context { get; }
    RevisionKey Revision { get; }
    FactQuery<TJudgment> Query<TJudgment>(SubjectKey subject, ScopeKey scope);
    // Fact/derivation creation requires capabilities controlled by host composition.
    IReadOnlyList<FactDependency> Explain(FactHandle handle);
}

// In SemanticEvidence.Rules.Numeric, selected trusted rule:
public interface IIntervalInference32
{
    FactQuery<Range32> TryAffineShift(FactHandle sourceRange, FactHandle affineRelation);
}

// In UniversalToolchain.SemanticEvidence ONLY (conceptual internal API):
internal interface IHostGuardedSsaRewrite
{
    GuardedProposal Propose(ImmutableSsaSnapshot snapshot, IEvidenceSession evidence);
    GuardedCommitResult TryCommit(GuardedProposal candidate); // host owns recheck
}
```

Do **not** ship this exact sketch before compilation/ABI review; it encodes dependency direction, not a promise that these exact names/types are finalized.

## 9. Lifetime and negative paths

- Each session bounded by maximum nodes and inference depth. `Unsupported` and resource exhaustion must return no optimizer permission.
- Successful IR commit advances epoch; invalidate all snapshot facts initially. Selective dependency invalidation is an optional later optimization.
- Root revoke invalidates descendants; memoized DAG traversal distinguishes legal diamonds from actual cycles; parents are earlier/owned nodes only.
- Failure/throw during producer, inference or proposal must not partially publish roots or transformed IR. Single owner lock/CAS protects final UT write.
- Core's lock is **not** a substitute for UT owner atomic commit; other UT passes need actual routing/ownership audit.

## 10. Evidence status

Historical isolated C# prototype demonstrated trusted inference and selected concurrency behavior, but it currently mixes generic facts with simulated rewrite commit and SSA-like subject fields. **Neither this new package split nor an actual UT adapter has been implemented/tested in the present revision.** See `08_MIGRATION_AND_ACCEPTANCE.md` for exact tasks.
