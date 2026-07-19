# Twenty-five-minute talk outline

## 0:00-2:30 — The disappearing abstraction

Open with the question:

> What if a language could remain extensible while it is being built, but the extension machinery disappeared before execution?

Show the short pipeline:

```text
feature modules -> dialect -> Bytecode -> AIR -> typed operations -> CIL -> JIT code
```

State the second constraint immediately: the interpreter and compiler must still execute one language.

## 2:30-6:00 — Extensible programming, concretely

Explain only the minimum architecture:

- independent language feature modules;
- dialect composition as feature selection;
- deterministic selected runtime plan;
- Bytecode as the frontend composition boundary;
- AIR as the explicit backend/optimizer semantic boundary.

Avoid class diagrams and long interface lists.

## 6:00-12:00 — Live pricing demonstration

1. Show `price * 0.9 + fee`.
2. Run the hardcoded C# path.
3. Run the general Wist dialect.
4. Run the restricted pricing dialect.
5. Show the equal result.
6. Show the restricted dialect rejecting statement-style binding syntax.
7. Point to the selected runtime plan and the interpreter/CIL execution choices.

## 12:00-16:00 — Where the abstractions go

Trace one representative operation through the pipeline:

```text
module-owned syntax/semantics
-> Bytecode contribution
-> AIR operation
-> capability-gated typed lowering
-> DynamicMethod CIL
-> .NET JIT
```

Clarify that the module/plugin abstraction is primarily a construction-time mechanism; the hot compiled path invokes a prepared typed artifact.

Show the performance model and measurement boundary briefly. Prepared invocation, convenience evaluation, and compilation/setup cost must remain separate. Show numeric results only when a fresh reproducible run records the exact commit, environment, raw artifacts, and comparison contract.

## 16:00-21:30 — When one DSL became two languages

Present the preserved binding/local-variable reproducer.

Explain:

- the observable interpreter/compiler disagreement;
- why a local patch in one backend would be the wrong architecture;
- the explicit external-binding versus local-storage invariant;
- capability-gated lowering;
- parity tests across declaration order, unused bindings, nested scopes, and shadowing.

Use the current fixed test as evidence. Do not intentionally run a broken current build live.

## 21:30-24:00 — Reusable rules

Leave the audience with five rules:

1. Compose language features before backend activation.
2. Make binding and storage semantics explicit before optimization.
3. Let optimizers query capabilities instead of concrete backend names.
4. Treat the interpreter as a semantic oracle, not merely a slow fallback.
5. Test parity across dialect configurations and optimizer states.

## 24:00-25:00 — Closing

Close with:

> Extensible during construction. Specialized during execution. One semantics across supported runtimes.

## Demo fallback

Prepare three levels:

- **Primary:** live `run-demo.sh` with the pricing output and focused tests.
- **Fallback:** terminal recording of the same command.
- **Last resort:** screenshots plus the committed expected output and direct links to regression tests.

Never depend on conference Wi-Fi. Restore and build before the session, and use `--no-restore --no-build` for the live segment after the environment has been prepared.
