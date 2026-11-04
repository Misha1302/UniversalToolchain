# 03 — Updated execution roadmap: portable library first, UT adapter second

**Reference source baseline:** `Misha1302/UniversalToolchain master@886bf241623e83566585bf2aec5b635fefe1675f` (last audited 2026-10-16; independently compare current HEAD before coding). This document is a plan, not permission to write to GitHub. The original P1/P2/Q model code is preserved as an oracle; first work is extracting a real portable library, not checking its old `.csproj` name.

## Verified existing UT owners (historical source audit)

- `UniversalToolchain.Runtime/LanguageArtifactBuildPipeline.cs`: selected route and artifact owner/plan binding; SHA `7b65b7f147cece9d8e3148b987fd5a194eb1d5e933b2f1c965041789d445f9d3`.
- `UniversalToolchain.Ssa.Optimization/SsaOptimizerPipeline.cs`: structural verification after each pass; SHA `c2118ec97fff44333d0d8ceb8e2e434b463ea885d96c8c29ad6e44ea289c2f1c`.
- `UniversalToolchain.Ssa.Optimization/SsaRoundtripRoute.cs`: AIR↔SSA and Prefer/Require; SHA `846952af3a8d66e927834e940d0cc17a721925d517a2c93bfea1ab7553dc6bf4`.
- `UniversalToolchain.Ir.Abstractions/IrStageContract.cs`: pipeline `FactId` scheduling, not semantic evidence; SHA `ca7b6b3285732570565136f67c736358f77e4e7682318402cf716ff019a0c38a`.

### LF-00 — freeze public boundary (no UT change)

**Deliverable:** one dependency diagram and compilable `SemanticEvidence.Core` project with no UT package/namespace; choose immutable opaque context/snapshot/subject keys; declare thread/lifecycle semantics, failure statuses and assembly visibility.

**Acceptance:** automated dependency scan rejects `ProjectReference`/`PackageReference` to UT and banned UT namespaces; no Core IR or `RewritePermit`/`TryCommit` API. Legacy model remains intact in its own folder. **The old model alone does not satisfy this task.**

### LF-01 — port trusted inference and DAG

**Deliverable:** `SemanticEvidence.Core` facts/session/admission, derivation handles, bounded parent DAG, revocation/staleness; `SemanticEvidence.Rules.Numeric` for checked Int32 range/affine/intersection. Keep rules trusted and closed for v1; subject/scope preconditions enforced in code.

**Acceptance:** build tests for wrong subject/type, same name different owner, wrong scope/context, overflow, contradictory facts, legal DAG diamond, cyclic input protection, all-or-nothing root batch, foreign session handle, stale revision, root revoke, cancellation/resource bound and parallel read/derive. No compilation-specific types in core.

### LF-02 — genuine second non-UT host and library ABI freeze for pilot

**Deliverable:** toy AST analyzer or SQL-plan optimizer independently instantiates Core+Numeric, supplies its own subject/context adapter and independent P1/P2/Q. Build Q before separately adding P2.

**Acceptance:** test runner compiles WITHOUT any UT source; Q DLL SHA identical after P2 added; two hosts share exactly the same Core/Rules assemblies/API; no P2-specific switch in central library. This tests portability, not just a file name.

**Checkpoint:** If second host requires SSA-specific assumptions or a second discovery planner, repair Core representation *before UT integration*. Do not publish NuGet or freeze SemVer yet.

### UT-L3-00 — actual UT vertical slice + one supported rewrite

**Deliverable:** source/SSA fixture for checked Int32 `i=x+5`, pure `i<bound` comparison (one block, no observable effects); actual selected profile and owner. Explicitly document whether Wist frontend supports all nodes; if not, use a properly labeled SSA-level oracle.

**Acceptance:** inspected opcode/mapping and independent reference semantics, counterexample for `x=7`, source SHA.

### UT-L3-01 — thin UT adapter, no duplicated planner

**Deliverable:** `UniversalToolchain.SemanticEvidence` references Core+Numeric; selected `LanguagePlan`/module/producer identity mapped into opaque Core keys; selected root admission; snapshot epochs owned by UT. **No** reflection loader, `LanguagePlan` recreation or Core dependency back to UT.

**Acceptance:** selected/unselected P2 differ; forged ID/assembly rejected; wrong profile/snapshot rejected; default existing optimization behavior unaffected. Test the actual public Core API in the adapter, no internal copy of DAG implementation.

### UT-L3-02 — UT-only Rewrite Gate and atomic owner publication

**Deliverable:** host-only `IHostGuardedSsaRewrite` wrapper on selected route. Candidate constructed from immutable snapshot; Core merely answers factual/predicate queries. Guard validates specific SSA comparison, operand identity, semantic profile and root liveness; structural verifier runs before atomic single-owner commit, which rechecks plan, epoch, policy, selected producer bindings and world.

**Acceptance:** structurally valid 0→1 wrong rewrite rejected **in guarded path**, well-formed valid rewrite accepted, malformed SSA rejected, stale/revoke/policy/parallel commit rejected, exceptions do not modify published artifact. Audit alternate publication routes; explicitly list trusted existing passes OUTSIDE proof coverage.

### UT-L3-03 — true UT P1/P2/Q N+1 differential

**Deliverable:** P1/Q built first; independent P2 built later and selected via actual UT module route; Q unchanged SHA. Test source→AIR→SSA→guarded rewrite→AIR→backend where supported, compare return/exception/observable traces in declared subset. Re-run with P2 absent / incompatible.

**Acceptance:** exact commit and scripts/logs; all eligible cases match reference; zero false permits on attempted negatives; unsupported language constructs flagged, not silently generalized. Honest direct typed C# reference on same workloads.

### UT-L3-04 — performance / release decision

Measure 30+ warm builds per variant, total p50/p95 and allocations, memory, ABI/API versioning effort, diff LoC to add another producer and second host cost. Initial *targets* (NOT measured): added full compile p95 ≤10%, peak memory ≤20%, CI duration ≤20%. Confirm CI Linux/Windows, benchmark selection, rollback via feature flag; default off until signed-off pilot. If library boundary degrades all consumers or brings no real operational benefit, use simple typed C# within UT.

## Stop conditions

- Core needs a UT reference, `LanguagePlan` or SSA value type.
- The actual compiler cannot distinguish valid host-produced context/root admission from untrusted metadata.
- One approved proof-enabled rewrite path can publish without revalidation; rollback fails on exception.
- Incorrect semantics permitted under supported subset, or performance cost no longer justifiable.
- Independent P2 requires Q recompilation or host-code special-case.

## Mutation allowlist when coding

Allowed: new standalone `SemanticEvidence.*` projects; new *opt-in* UT adapter and isolated test/bench files; selected host-only guarded route hook after source audit. Protect: existing optimizer semantics, Wist public facade, `IrStageContract`, default SSA settings, original legacy evidence/logs and upstream GitHub state. Compare protected source checksums with baseline.

## Stage outputs

Each stage saves exact source commit, changed-file list, commands, test logs, hashes, reproducible fixture and explicit PASS/FAIL/NOT_RUN statuses. A compiled toy core cannot be called a UT integration; a source audit cannot be called an executable backend proof.
