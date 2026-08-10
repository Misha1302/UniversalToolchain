---
title: Lowering and Route Walkthrough
description: Follow one Wist formula and one generic language artifact route through the implementation.
---

# Lowering and route walkthrough

UniversalToolchain currently contains two related but distinct pipelines.

## Wist compiler pipeline

For a Wist formula such as:

```wist
usage * weight
```

the conceptual path is:

```text
source text
-> lexer tokens
-> parser-owned AST nodes
-> frontend visitors emit module-oriented Bytecode
-> BytecodeToAbstractIrConverterImpl creates AIR
-> backend-neutral optimizers inspect capabilities
-> optional Wist AIR -> SSA -> AIR route
-> interpreter or CIL backend
-> compiled artifact/session or typed Wist delegate
```

Representative implementation owners:

| Stage | Source location |
|---|---|
| public formula orchestration | `UniversalToolchain/UniversalToolchain.Wist/WistEngine.cs` |
| arithmetic AST lowering | `UniversalToolchain/ArithmeticModule/Visitors/ArithmeticAstVisitor.cs` |
| Bytecode to AIR | `UniversalToolchain/AbstractIrConverters/BytecodeToAbstractIrConverterImpl.cs` |
| native arithmetic specialization | `UniversalToolchain/NativeMathModule/NativeCILOptimizerModule.cs` |
| CIL intrinsic lowering | `UniversalToolchain/BytecodeDynamicMethodsCompiler/Compilers/CilIntrinsicRegistry.cs` |
| generic artifact/session boundary | `UniversalToolchain/UniversalToolchain.Runtime` |

Bytecode and AIR are separate semantic boundaries. Frontend modules do not emit `DynamicMethod` or backend-specific instructions directly.

## Generic language-authoring route

The Acme sample uses a shorter independent route:

```text
source.text<string>
  -- acme.pricing.parse --> acme.pricing.syntax<PricingExpression>

interpreter backend:
  acme.pricing.syntax<PricingExpression>
  -- exact interpreter executor --> decimal

compiled backend:
  acme.pricing.syntax<PricingExpression>
  -- acme.pricing.compile --> acme.pricing.executable<Func<decimal>>
  -- exact compiled executor --> decimal
```

The planner produces both routes before `LanguageRuntime` is created. Runtime execution follows only the selected route and validates every intermediate contract.

## What the generic SDK does not assume

The generic route does not require AST, Bytecode, AIR or SSA. Those are artifact protocols a language may choose. Wist uses its mature compiler pipeline; a small external language may route directly from text to a typed syntax object and executor.

This distinction prevents Wist implementation details from becoming mandatory framework truth.
