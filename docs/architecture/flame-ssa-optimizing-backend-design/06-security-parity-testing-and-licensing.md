# Security, parity, testing, diagnostics, and licensing

## 28. Security and trust boundaries

### 28.1 No new sandbox claim

Compiled execution remains a performance mechanism, not a sandbox boundary.

### 28.2 Managed call allowlists

The backend may emit only managed members already approved by selected runtime policy.

No backend-local APIs should permit source code to request arbitrary:

- `Assembly.Load`;
- `Type.GetType`;
- `MethodInfo.Invoke`;
- `Process.Start`;
- file or network APIs;
- environment termination;
- reflection emit;
- unmanaged calls.

Trusted profiles may register broader APIs, but registration must remain explicit and auditable.

### 28.3 Reflection policy

Reflection is allowed during bounded composition and binding resolution. It should not be repeated in the hot execution path.

### 28.4 Resource isolation

Flame compilation does not enforce time, memory, or recursion limits. Untrusted code still requires process/environment isolation and resource limits.

### 28.5 Artifact loading

Persistent assemblies must be treated as executable code. Loading should validate:

- artifact provenance;
- expected hashes where configured;
- ABI version;
- selected trust policy;
- dependency identities;
- backend compatibility.

## 29. Semantic parity contract

### 29.1 Result parity

For supported shared programs:

```text
interpreter result == cil result == optimized-cil result
```

under the project's exact numeric, exception, and binding semantics.

### 29.2 Observable-effect parity

Result equality is insufficient. Tests must verify:

- call count;
- call order;
- external state reads;
- writes and mutations;
- exception point and type;
- short-circuit behavior;
- allocation-sensitive behavior where observable;
- execution-scoped provider identity;
- local-variable isolation between sessions;
- no duplicated side effects after inlining or CSE.

### 29.3 Floating-point parity

Tests should cover:

- NaN behavior;
- positive and negative infinity;
- signed zero;
- conversion behavior;
- checked/unchecked overflow where defined;
- comparison semantics;
- decimal method/operator behavior.

Optimization must not enable algebraic transformations that violate required IEEE or CLR-observable behavior.

### 29.4 Failure parity

When all backends support a construct, exceptions and diagnostics should be aligned where the architecture defines a stable contract. Backend-specific implementation failures must not leak as language semantics.

## 30. Test strategy

### 30.1 Shared AIR verifier tests

Required cases:

- empty program according to current contract;
- straight-line stack effects;
- duplicate label;
- unknown jump target;
- conditional branch fallthrough;
- unconditional branch termination;
- incompatible incoming stack depths;
- incompatible incoming stack types;
- loops with stable fixed points;
- nested loops;
- unreachable blocks;
- multiple predecessors;
- back edges to entry-like blocks;
- malformed intrinsic stack effect;
- deterministic diagnostic ordering;
- concurrent verifier use;
- no cross-compilation state leakage.

### 30.2 Stack-to-SSA golden tests

Snapshot tests should cover:

- constants and arithmetic;
- one `if` merge;
- nested conditions;
- loop-carried values;
- multiple stack values at a join;
- dropped pure values;
- dropped side-effecting calls;
- external arguments;
- local variable runtime calls;
- statically resolved managed calls;
- constructors;
- unsupported intrinsic diagnostics.

Golden output must use deterministic names.

### 30.3 Backend parity tests

Three-way tests:

```text
interpreter
cil
optimized-cil
```

Coverage:

- numbers and arithmetic;
- Booleans;
- comparisons and equality;
- conditions;
- loops;
- scopes and shadowing;
- external bindings;
- local variables;
- SafeMath MVP;
- C# static and instance calls;
- F# facade calls;
- constructors;
- side-effecting test services;
- nondeterministic calls;
- exceptions;
- disabled dialect capability rejection;
- optimizer enabled/disabled.

### 30.4 Artifact lifecycle tests

- multiple sessions from one artifact;
- session argument isolation;
- concurrent invocation;
- artifact disposal;
- collectible load context collection;
- cache eviction unload;
- no global delegate retention;
- dependency resolution failures;
- generated assembly load in a fresh process;
- persistent artifact hash verification.

### 30.5 Security tests

- disallowed managed member cannot be emitted;
- backend does not perform arbitrary assembly scans;
- trusted and restricted profiles remain distinct;
- source cannot inject type/member names around structured resolution;
- artifact provenance mismatch is rejected;
- unsupported reflection calls fail before emission;
- unknown effects remain conservative.

### 30.6 Architecture guardrail tests

Protect:

- BasicCore has no Flame reference;
- Wist frontend modules have no Flame reference;
- generic runtime code does not branch on `optimized-cil` or `flame`;
- Flame types do not appear in public framework contracts;
- existing `compiler` alias still resolves to `cil`;
- third-backend activation remains manifest-driven;
- interpreter intrinsic policy remains minimal;
- backend alias lists are not the long-term semantic capability source;
- no raw-source parsing is introduced.

### 30.7 Upstream version tests

When pinning or updating Flame:

- compile the adapter against the exact pinned revision/package;
- run focused lowering tests;
- run full parity tests;
- compare serialized SSA and generated IL changes;
- review upstream license changes;
- record the upstream revision in release metadata.

## 31. Benchmark methodology

### 31.1 Measurements

Measure separately:

- source-to-artifact compilation latency;
- AIR verification latency;
- stack-to-SSA lowering latency;
- optimization latency per profile;
- CIL emission latency;
- assembly load latency;
- first invocation;
- steady-state invocation;
- allocations during compilation;
- allocations during invocation;
- artifact size;
- working set retained by cached artifacts;
- unload time/collectability;
- break-even invocation count against `cil`.

### 31.2 Workload classes

Include:

- tiny arithmetic expression;
- medium expression tree;
- branch-heavy rule;
- loop-heavy program;
- repeated common subexpressions;
- many local variables;
- managed-call-heavy program;
- side-effecting calls that restrict optimization;
- large multi-function or multi-rule artifact when such functions exist;
- negative unsupported program.

### 31.3 Honest claims

Do not publish only steady-state invocation numbers if compilation is expensive.

The useful decision metric is often:

```text
break-even invocations =
  additional compilation cost /
  per-invocation savings
```

Example only:

```text
cil compile: 0.8 ms
optimized-cil compile: 15 ms
cil invoke: 40 ns
optimized-cil invoke: 25 ns

break-even is approximately one million invocations.
```

No default-selection claim should be made without representative measurements.

## 32. Diagnostics

### 32.1 Diagnostic categories

Suggested categories:

- AIR verification;
- backend compatibility;
- type mapping;
- intrinsic lowering;
- managed member binding;
- effect contract;
- optimization restriction;
- CIL emission;
- assembly loading;
- artifact provenance;
- licensing/package availability.

### 32.2 Stable structure

A diagnostic should contain:

- stable code;
- severity;
- stage;
- backend id;
- message;
- related source/AIR location;
- related module/feature/intrinsic/member;
- remediation hint where safe;
- nested cause for logs, without exposing internal exception text as the stable contract.

### 32.3 Example

```text
UTC-FLAME-LOWERING-0042

Backend 'optimized-cil' cannot lower intrinsic 'managed.call'.
The target method uses an open generic parameter that was not resolved
before AIR generation.

Owning stage: managed member resolution
AIR block: call_7
Suggested action: register a closed method binding or select backend 'cil'.
```

## 33. Observability

Runtime metrics may include:

- compilation attempts by backend/profile;
- compatibility failures by reason;
- compile duration histogram;
- cache hits/misses/evictions;
- artifact load failures;
- live collectible contexts;
- unload success/failure observations;
- optimized instruction/block reduction;
- fallback attempts, which should normally be zero for explicit selection;
- parity test coverage in CI, not production.

Logs must not include source text, secrets, binding values, or private method arguments by default.

## 34. Licensing and distribution gate

### 34.1 Current issue

The inspected Flame repository declares `GPL-3.0-or-later`. UniversalToolchain is Apache-2.0.

A direct package or source dependency in a distributed combined product must be treated as a release blocker until reviewed and documented.

### 34.2 Preferred resolution

Request one of:

- dual licensing of relevant Flame libraries under MIT or Apache-2.0;
- an explicit permissive commercial/open-source linking grant;
- a clearly scoped permissive exception for library consumers.

The request should identify exactly which Flame projects are needed.

### 34.3 Experimental repository option

A separate clearly GPL experimental integration may be useful for technical validation, but it must not be published as part of the Apache-2.0 core distribution without a legal decision.

### 34.4 External worker option

An external compiler worker may create a clearer component boundary, but it is not automatically a legal solution. It also increases deployment complexity.

### 34.5 Clean-room fallback

If licensing cannot be resolved, retain the generic work from this proposal and implement a clean-room optimizing backend or integrate a permissively licensed alternative.

### 34.6 Required release evidence

Before distribution:

- documented license review result;
- exact Flame revision/package and transitive licenses;
- NOTICE/LICENSE updates;
- package metadata review;
- source distribution obligations review;
- CI license scan;
- no accidental Flame dependency in Apache-only packages.


[Back to the design dossier index](index.md)
