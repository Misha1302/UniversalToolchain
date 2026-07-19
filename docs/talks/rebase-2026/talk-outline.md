# Thirty-minute REBASE talk outline

Status: proposal.

## 0:00-3:00 — The problem

Open with the pipeline:

```text
feature modules -> dialect -> Bytecode -> AIR -> typed operations -> CIL -> JIT code
```

Then state the two requirements:

- the language surface must be selected modularly and deterministically;
- interpreter and compiled execution must preserve the same observable semantics.

The question is not whether modules can be added. It is whether their dispatch can disappear from a prepared execution path without moving language semantics into a backend.

## 3:00-7:00 — How the language is assembled

Introduce only the boundaries needed for the rest of the talk:

- feature modules own syntax and semantic contributions;
- a dialect selects modules, optimizers, capabilities, and backends;
- the selected configuration becomes a deterministic runtime plan;
- Bytecode is the frontend composition boundary;
- AIR is the optimizer and backend boundary;
- optimizers query capabilities rather than concrete backend names.

## 7:00-13:00 — Restricted pricing DSL

Use `price * 0.9 + fee` as the running example.

1. Run the hardcoded C# implementation.
2. Run the general Wist dialect.
3. Run the restricted pricing dialect.
4. Compare the results.
5. Show the restricted dialect rejecting statement-style binding syntax that it did not select.
6. Inspect the deterministic runtime plan and the selected interpreter/CIL capabilities.

The scenario keeps the domain simple while making feature selection and execution-path differences visible.

## 13:00-19:00 — From a module to typed CIL

Trace one representative operation:

```text
module-owned syntax and semantics
-> Bytecode contribution
-> AIR operation
-> capability-gated typed lowering
-> DynamicMethod CIL
-> .NET JIT
```

Make the claim precise:

- modules and dialect selection are construction-time mechanisms;
- supported compiled operations become typed operations in a prepared artifact;
- the hot invocation path does not perform per-operation module dispatch;
- unsupported specialization keeps the portable representation;
- none of this guarantees universal JIT inlining or handwritten-C# performance.

Briefly show the performance model. Prepared invocation, convenience evaluation, and compilation/setup cost are separate measurements and must not be compared as one benchmark.

## 19:00-25:00 — The regression that exposed the boundary

Use the preserved scenario:

```text
let i = 0
i = i + 1
i = i + 1
i = i + 1
price + fee * i
```

Explain the failure in order:

1. External bindings and lexical locals acquired incompatible storage assumptions.
2. Interpreter and CIL execution could therefore disagree on the same program.
3. Patching one generated instruction would have hidden the ownership error.
4. Bindings and locals were given distinct semantic identities before backend storage allocation.
5. Backend contracts now own physical mapping, while parity tests cover declaration order, unused bindings, repeated access, nested scopes, and shadowing.

Show the current fixed regression tests as evidence.

## 25:00-28:00 — Three conclusions

1. **Define the language before activating a backend.** Compose features first, and keep semantic identities in backend-independent representations.
2. **Specialize by capability, not by implementation name.** Optimizers may change representation only when the selected backend proves support.
3. **Use the interpreter as a semantic reference.** Test parity across dialect configurations, storage layouts, scope shapes, and optimizer states rather than treating the interpreter as only a slow fallback.

The same boundary appears in expression evaluators, rule runtimes, query compilers, and template engines whenever interpreted and compiled paths share one language contract.

## 28:00-30:00 — Limits and closing

State the current limits directly:

- the project is an open-source alpha, not a production deployment report;
- restricted dialect composition is not hardened sandboxing;
- generic third-party DSL authoring is less mature than the Wist-first path;
- performance evidence is tied to recorded scenarios and environments.

Close with:

> Extensible during construction. Specialized during execution. One semantics across supported runtimes.
