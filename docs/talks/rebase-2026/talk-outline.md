# Thirty-minute REBASE talk outline

## 0:00-3:00 — The disappearing abstraction

Open with the practical question:

> Can a language remain extensible while it is being assembled, but execute as a prepared concrete artifact without letting each backend redefine its semantics?

Show the short pipeline:

```text
feature modules -> dialect -> Bytecode -> AIR -> typed operations -> CIL -> JIT code
```

State both constraints immediately:

- feature selection must remain modular and deterministic;
- the interpreter and compiler must still execute one language.

## 3:00-7:00 — Extensible programming, concretely

Explain only the architecture required for the rest of the talk:

- independent language feature modules;
- dialect composition as explicit feature selection;
- deterministic runtime-plan construction;
- Bytecode as the frontend composition boundary;
- AIR as the optimizer/backend execution boundary;
- backend capability queries instead of concrete backend-name checks.

Avoid class diagrams and long interface inventories.

## 7:00-13:00 — Restricted pricing DSL demonstration

1. Show `price * 0.9 + fee` as the smallest useful formula.
2. Run the hardcoded C# path.
3. Run the general Wist dialect.
4. Run the restricted pricing dialect.
5. Show equal results through the prepared paths.
6. Show the restricted dialect rejecting statement-style binding syntax that was not selected.
7. Inspect the deterministic runtime plan and selected interpreter/CIL capabilities.

The point is not pricing itself. The scenario makes language-surface selection and execution-path differences visible without requiring domain-specific background.

## 13:00-19:00 — Where the abstractions go

Trace one representative operation through the pipeline:

```text
module-owned syntax and semantics
-> Bytecode contribution
-> AIR operation
-> capability-gated typed lowering
-> DynamicMethod CIL
-> .NET JIT
```

Clarify the exact claim:

- language modules and dialect selection are construction-time mechanisms;
- supported compiled operations become typed operations in a prepared artifact;
- the hot invocation path does not perform per-operation module dispatch;
- this does not promise universal JIT inlining or handwritten-C# performance.

Present the performance **model and measurement boundary**, not an unqualified benchmark table. Prepared invocation, convenience evaluation, and compilation/setup cost must be measured separately. Show numeric results only when a fresh run preserves the exact commit, environment, raw BenchmarkDotNet artifacts, and comparison contract.

## 19:00-25:00 — When specialization threatened semantics

Present the preserved external-binding/local-variable regression:

```text
let i = 0
i = i + 1
i = i + 1
i = i + 1
price + fee * i
```

Explain:

- how interpreter and CIL storage assumptions could diverge;
- why patching one generated instruction would hide the ownership error;
- why external bindings and lexical locals need distinct semantic identities;
- how late backend-specific storage mapping preserves the language contract;
- how parity tests cover declaration order, unused bindings, nested scopes, repeated access, and shadowing.

Use the current fixed regression tests as evidence. Do not break the current runtime live on stage.

## 25:00-28:00 — Reusable engineering rules

Leave the audience with five rules:

1. Compose language features before backend activation.
2. Put semantic identities in backend-independent representations.
3. Let optimizers query capabilities instead of concrete backend names.
4. Treat the interpreter as a semantic reference implementation, not merely a slow fallback.
5. Test parity across dialect configurations, storage shapes, and optimizer states.

Briefly connect the rules to expression engines, rule runtimes, query compilers, template engines, and tiered language implementations.

## 28:00-30:00 — Limits and closing

State the current boundary explicitly:

- open-source alpha, not a production deployment report;
- restricted dialect composition is not hardened sandboxing;
- generic third-party DSL authoring remains less mature than the Wist-first path;
- performance evidence is scenario- and environment-bounded.

Close with:

> Extensible during construction. Specialized during execution. One semantics across supported runtimes.

## Demonstration fallback

Prepare three levels:

- **Primary:** run the shared demo script after restoring and building before the session.
- **Fallback:** use a terminal recording of the same command and results.
- **Last resort:** use screenshots, the committed expected output, and direct links to the focused regression tests.

Never depend on conference Wi-Fi. Restore and build before the session; use `--no-restore --no-build` for live commands after the environment is prepared.
