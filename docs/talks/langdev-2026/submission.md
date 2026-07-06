# LangDev 2026 submission

## Title

**Build the Language, Then Make the Abstractions Disappear: Extensible Programming on .NET**

## Abstract

What fascinated me was not merely making a DSL extensible, but making its extension machinery disappear before execution.

UniversalToolchain explores this form of extensible programming on .NET. Language features are authored as independent modules, composed into dialects, and lowered through Bytecode and Abstract IR. Specialization extends beyond arithmetic: local variables, external bindings, control flow, calls, and typed operations become concrete runtime or CIL operations, removing language-construction machinery from prepared execution paths. The current benchmark suite is deliberately scoped to separate hot prepared invocation, convenience evaluation overhead, and cold compilation cost before making performance claims.

I will demonstrate this journey with a restricted pricing DSL: assemble the language, inspect its deterministic runtime plan and intermediate representations, then execute one formula through both an AIR interpreter and a DynamicMethod-based CIL backend.

Extensibility also creates a dangerous failure mode: two backends can quietly turn one DSL into two languages. I will walk through a real regression involving external bindings and local-variable shadowing, and show how explicit storage semantics, capability-gated lowering, deterministic composition, and cross-backend tests prevent that divergence.

The goal is simple: extensible languages during construction, specialized code during execution, and one semantics across supported runtimes.

## Demo outline

1. Assemble a restricted pricing language from independently selected feature modules.
2. Inspect the dialect's deterministic runtime plan and selected execution capabilities.
3. Trace language features, including external bindings, local variables, and arithmetic, from frontend semantics through Bytecode and AIR to concrete runtime or CIL operations.
4. Execute the same pricing formula through the AIR interpreter and the `DynamicMethod`-based CIL backend.
5. Show the restricted dialect rejecting a feature that was not selected.
6. Walk through a real semantic-parity regression involving external bindings and local-variable shadowing.
7. Show the explicit storage invariant, capability contracts, and cross-backend regression tests that prevent the same class of failure.

## Speaker bio

Mikhail Razakov is an open-source compiler and runtime developer and the creator of UniversalToolchain/Wist2, a modular .NET framework prototype for building and executing domain-specific languages. His interests include extensible programming, language composition, intermediate representations, runtime specialization, JIT compilation, and semantic consistency across interpreted and compiled execution.
