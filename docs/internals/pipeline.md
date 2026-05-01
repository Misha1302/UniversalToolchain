---
title: Pipeline
description: Explain source to execution pipeline.
---

# Pipeline

The internal pipeline is the lifecycle that turns `CompilationInput` into a compiled artifact or a prepared execution session.

The canonical implementation entry point is `PreparedExecutionBuilder<TCompilationOutput>`. `BasicCoreImpl<TCompilationOutput>` delegates compilation and preparation to this builder.

## Why this page matters

Most architecture mistakes happen when a change is placed in the wrong stage.

Examples:

- parsing syntax in a bytecode visitor;
- fixing AST shape during bytecode post-processing;
- letting an optimizer provide required semantics;
- making a backend silently accept unsupported intrinsics;
- mixing runtime input values with local variable semantics.

This page defines the actual order of operations so each stage can keep a narrow responsibility.

## Public model vs. internal lifecycle

The public mental model is:

```text
source text
  -> lexer
  -> parser
  -> AST
  -> bytecode
  -> AIR
  -> backend
  -> result
```

The internal lifecycle is more precise:

```text
CompilationInput
  -> create per-build services
  -> modules.ProcessText
  -> modules.InitLexer
  -> lexer.Lexemize
  -> modules.ProcessLexemes
  -> modules.InitParser
  -> parser.Parse
  -> modules.ProcessAst
  -> Binder
  -> modules.InitAstTranslator
  -> astTranslator.Translate
  -> modules.ProcessBytecode
  -> optimizers.InitMethodsTranslator
  -> optimizers.InitIntrinsicCapabilityContext
  -> methodsTranslator.Translate(bytecode)
  -> optimizers.ProcessIr
  -> ExtractAllowedRuntimeProviderTypes
  -> middleEndModules.InitMethodsCompiler
  -> compiler.Compile
  -> middleEndModules.ProcessCompilation
  -> middleEndModules.InitExecutor
  -> compiled artifact or prepared session
```

Use the second model when documenting internals.

## Build setup

`PreparedExecutionBuilder` creates fresh pipeline services for each build:

```text
lexer
parser
AST translator
bytecode-to-AIR translator
backend compiler
executor
intrinsic capability set
optimizer capability context
```

This matters because pipeline services may carry request-local state. Module instances may be shared by DI, but lexer/parser/translator/compiler/executor instances are created through factories during the build.

## Input normalization

`BasicCoreImpl` accepts two common input shapes:

- runtime input: `Run(string code, Dictionary<string, object>? parameters)`;
- declared input: `Compile(string code, OrderedDictionary<string, Type>? parameters)`.

Both are normalized into `CompilationInput` with `ExternalBinding` entries.

Runtime input includes values. Declared input includes types. This distinction is important for compiled artifacts: compile-time parameter shape and runtime parameter values are not the same concern.

## Frontend text stage

First, every selected frontend module may transform source text:

```text
modules.ProcessText
```

This stage should be used sparingly. It is appropriate for explicit text-level preprocessing, not for hidden syntax recognition that should belong to lexer/parser/AST stages.

## Lexer stage

Modules register lexemes through:

```text
modules.InitLexer
```

Then the lexer turns the processed source text into lexeme values:

```text
lexer.Lexemize(targetCode)
```

After lexing, modules may transform the lexeme list:

```text
modules.ProcessLexemes
```

Lexeme post-processing should preserve the lexer/parser boundary. Do not use it to implement syntax that should be expressed through parser node creators.

## Parser stage

Modules register parser node creators through:

```text
modules.InitParser
```

Then the parser builds an AST:

```text
parser.Parse(targetLexemes)
```

Parser extensions are ordered and priority-sensitive. This stage owns syntax structure.

## AST processing and binding

After parsing, modules may transform the AST:

```text
modules.ProcessAst
```

Then external bindings are applied:

```text
new Binder(input.ExternalBindings).Bind(targetRoot)
```

Binding happens before AST-to-bytecode translation. This placement is important: visitors should receive a bound AST rather than rediscover external bindings independently.

## AST-to-bytecode translation

Modules register AST visitors through:

```text
modules.InitAstTranslator(astTranslator, modules)
```

Then the translator produces bytecode:

```text
astTranslator.Translate(boundRoot)
```

Visitors must self-filter because multiple visitors may inspect the same node. A visitor should emit only feature-owned semantics.

## Bytecode post-processing

After translation, modules may process bytecode:

```text
modules.ProcessBytecode
```

This is a bytecode-level hook. It should not be used to patch missing parser or AST behavior.

## Bytecode-to-AIR translation

Optimizers first initialize the bytecode-to-AIR translator:

```text
optimizers.InitMethodsTranslator(methodsTranslator)
```

Then they receive an intrinsic capability context:

```text
optimizers.InitIntrinsicCapabilityContext(optimizerCapabilityContext)
```

Then bytecode is translated into AIR:

```text
methodsTranslator.Translate(targetBytecode)
```

At this point, execution semantics are represented as backend-facing AIR instructions.

## IR optimization

Selected IR processing modules transform AIR:

```text
optimizers.ProcessIr(current, compiler)
```

The backend compiler is passed into the optimizer stage so optimizers can respect backend capabilities. This is a guardrail: backend-specific optimized instructions should not be emitted for backends that cannot consume them.

## Runtime provider extraction

The pipeline extracts allowed runtime provider types from AIR before compilation:

```text
ExtractAllowedRuntimeProviderTypes(targetIr)
```

Currently this scans AIR intrinsic calls for C# call descriptors that reference execution-scoped providers. The result becomes part of the compiled artifact.

This is implementation-specific and should not be described as a general-purpose security boundary.

## Backend compilation

Middle-end modules initialize the compiler:

```text
middleEndModules.InitMethodsCompiler(compiler)
```

Then the backend compiler compiles AIR:

```text
compiler.Compile(targetIr, input)
```

The output type depends on the selected backend. For example, the interpreter path can use AIR directly, while the CIL path compiles into a `DynamicMethod`.

## Compilation post-processing and executor setup

After backend compilation:

```text
middleEndModules.ProcessCompilation
middleEndModules.InitExecutor
```

This lets middle-end modules adapt backend output and executor wiring.

## Artifact/session output

`Compile(input)` returns a compiled artifact.

`Build(input)` returns a prepared execution object containing:

- source text;
- compiled artifact;
- execution session.

`BasicCoreImpl.PrepareToRun` stores the prepared execution in an `AsyncLocal`, and `RunPrepared` executes the stored session.

## Stage ownership summary

| Stage | Owns | Should not own |
|---|---|---|
| `ProcessText` | explicit text preprocessing | hidden grammar |
| Lexer | token recognition | AST shape |
| `ProcessLexemes` | token-list normalization | semantic lowering |
| Parser | AST structure | backend behavior |
| `ProcessAst` | AST normalization | bytecode execution semantics |
| Binder | external binding attachment | local variable runtime mutation |
| AST visitors | bytecode emission | raw source parsing |
| `ProcessBytecode` | bytecode-level transformations | parser fixes |
| AIR translator | backend-facing instruction stream | optimizer-only semantics |
| Optimizers | semantics-preserving IR transforms | required base semantics |
| Backend compiler | backend artifact creation | language syntax decisions |
| Executor | execution of compiled artifact | dialect composition |

## Next

Continue with [Lexer](/internals/lexer).
