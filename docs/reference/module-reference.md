---
title: Module Reference
description: List built-in modules and their responsibilities.
---

# Module Reference

This page lists the currently documented Wist frontend module aliases and their broad responsibilities.

For module authoring guidance, read [Writing Modules](/write-modules/). This page is a reference index, not a tutorial.

## Alias source

Module aliases are declared through `DialectModuleAliasAttribute`.

The attribute accepts one or more aliases:

```csharp
[DialectModuleAlias("Arithmetic")]
```

Most Wist frontend modules also expose themselves through:

```csharp
[DialectRuntimeExport("FrontendModule", "...")]
```

Runtime export metadata is what lets dialect/runtime composition select concrete runtime components.

## Common frontend modules

The following aliases are used by the current Wist module coverage tests and module implementations.

| Alias | Broad responsibility | Typical owner evidence |
|---|---|---|
| `Whitespaces` | whitespace lexeme handling | whitespace module |
| `SemicolonAsNewLine` | semicolon/newline normalization | semicolon module |
| `Comments` | comment lexemes | comments module |
| `Numbers` | numeric literal lexemes and number AST translation | `NumbersModuleImpl` |
| `Identifier` | identifier lexemes and identifier handling | identifier module |
| `Arithmetic` | arithmetic operator lexemes, parser creators and visitors | `ArithmeticModuleImpl` |
| `Equality` | equality operations | equality module |
| `Conditions` | `if` / `elif` / `else` conditional syntax | `ConditionsModuleImpl` |
| `ComparisonConditions` | comparison operations such as `<`, `<=`, `>`, `>=` | comparison condition module |
| `BooleanConditions` | boolean operations such as and/or/not | boolean condition module |
| `Loops` | `while` and `for` syntax | `LoopsModuleImpl` |
| `Variables` | `let`, assignment and local variable behavior | `VariablesModuleImpl` |
| `Scopes` | grouped scopes and parenthesized bodies | scopes module |
| `Labels` | label-related control constructs | labels module |
| `InternalPreprocessorLexemes` | internal preprocessing lexeme support | internal preprocessor module |
| `CSharpInterop` | trusted C# interop surface | C# interop module |

## Native and specialized modules

Additional modules exist for specialized profiles:

| Alias | Broad responsibility |
|---|---|
| `NativeTypes` | native type-oriented execution/profile support |
| `SafeMathFunctions` | safe math function surface |
| `FunctionCalls` | function call syntax/runtime support |
| `ParametersSetter` | parameter-setting syntax/runtime support |

These should be used only when the dialect actually needs the corresponding surface.

## Optimizer aliases

Optimizers are separate from frontend modules and use `DialectOptimizerAliasAttribute`.

Examples visible in current code include:

| Alias | Broad responsibility |
|---|---|
| `ArithmeticOptimization` | arithmetic intrinsic lowering and peephole simplification |
| `NativeCilOptimization` | typed native CIL-oriented constant loading |
| `NativeTypesOptimization` | native type optimization support |
| `EGraphOptimization` | e-graph-inspired arithmetic/profile optimization support |
| `BooleanOptimization` | boolean intrinsic optimization support |
| `ComparisonIntrinsicOptimization` | comparison intrinsic optimization support |

Optimizer aliases are enabled through dialect optimizer directives, not through `use` module directives.

## Module responsibilities

A frontend module may contribute:

- lexeme registrations;
- parser node creators;
- AST visitors;
- bytecode post-processing;
- capability providers;
- runtime export metadata.

A module should own only the syntax and semantics it introduces.

## Dependencies are not automatic magic

Some modules are useful only with related modules.

Examples:

- `Variables` usually requires `Identifier`.
- `Loops` usually requires variables and comparisons to be useful.
- `Conditions` often requires equality or comparison modules depending on the condition expression.
- `CSharpInterop` belongs to trusted/full profiles, not restricted user-facing formula DSLs.

Dialect authors are responsible for selecting coherent module sets and testing omitted syntax.

## Stability note

This page is a curated reference, not an automatically generated catalog. When adding a new module, update this page only after confirming the module's alias, runtime export and intended responsibility from code.

## Related pages

- [Writing Modules](/write-modules/)
- [Dialect Reference](/reference/dialect-reference)
- [Testing a DSL](/build-dsls/testing-dsl)
