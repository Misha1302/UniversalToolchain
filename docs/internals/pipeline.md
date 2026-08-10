---
title: Pipeline
description: Explain source to execution pipeline.
---

# Pipeline

The canonical pipeline is the plan-owned lifecycle that turns a typed entry artifact into the backend artifact selected by `LanguagePlan`, then executes that artifact through `LanguageRuntime`.

There is deliberately no framework-level end-to-end BasicCore coordinator. `BasicCoreImpl<TCompilationOutput>` and `PreparedExecutionBuilder<TCompilationOutput>` are retired; reusable lexer/parser/binding/lowering mechanics remain narrow stage services.

## Why this page matters

Most architecture mistakes happen when a change is placed in the wrong stage or when a local helper becomes a second owner of planning/runtime selection.

Examples:

- parsing syntax in a bytecode visitor;
- fixing AST shape during bytecode post-processing;
- letting an optimizer provide required semantics;
- making a backend silently accept unsupported intrinsics;
- re-selecting components after `LanguagePlan` already fixed the route;
- recreating a lexer→parser→compiler→executor god object under a new name.

## Ownership before execution

The ownership chain is:

```text
LanguageDefinition
  -> LanguageCompiler
  -> LanguagePlan
  -> LanguageRuntime
  -> exact route selected for the requested backend
```

`LanguageCompiler` is the only planner. It resolves feature dependencies, contributions, explicit exclusions, capability providers, contribution order, runtime provider and artifact route.

`LanguageRuntime` verifies that immutable plan, validates exact provider identity/policy and owns the runtime/build-session lifecycle. It does not discover or re-plan an alternative route.

## Artifact route execution

For a Wist source request the concrete route is:

```text
Source/Text
  -> Wist frontend transformer
      -> text/module hooks
      -> lexer
      -> lexeme/module hooks
      -> parser
      -> AST/module hooks
      -> Binder
  -> Wist bytecode transformer
      -> AST visitor registration
      -> AST-to-bytecode translation
      -> bytecode/module hooks
  -> Wist AIR transformer
      -> bytecode-to-AIR translation
  -> selected optimizer / optional SSA transformers
  -> selected backend transformer/executor
```

`LanguageArtifactBuildPipeline` walks the route already stored in `LanguagePlan`. Every step must match the planned contribution and typed artifact contracts.

## Input normalization

Wist runtime and build requests both become `CompilationInput`, but they carry different binding information:

- runtime requests carry argument values;
- build requests carry declared binding types.

`WistHostBindingAdapter` uses `CompilationInputNormalizer` for this conversion. Compile-time binding shape and runtime argument values remain separate concerns.

## Frontend stage

The Wist frontend transformer creates the plan-selected frontend modules and invokes `CanonicalArtifactStages.ParseAndBind`.

The reusable stage mechanics are:

```text
modules.ProcessText
modules.InitLexer
lexer.Lexemize
modules.ProcessLexemes
modules.InitParser
parser.Parse
modules.ProcessAst
Binder.Bind
```

Text hooks are for explicit preprocessing, not hidden grammar. Parser extensions own syntax structure; binding happens before bytecode lowering.

## AST-to-bytecode stage

The bytecode transformer invokes `CanonicalArtifactStages.LowerToBytecode`:

```text
modules.InitAstTranslator
astTranslator.Translate
modules.ProcessBytecode
```

AST visitors must self-filter because multiple visitors may inspect the same node. `ProcessBytecode` is a bytecode-level hook and must not repair missing parser/AST semantics.

## Bytecode-to-AIR stage

The AIR transformer invokes `CanonicalArtifactStages.LowerToAir` through the selected `IAbstractMethodsTranslator`.

Bytecode and AIR are distinct semantic boundaries. AIR is the representation consumed by optimizers and backends; code must not infer a new runtime/component selection from AIR contents.

## Optimization and SSA routes

Optimizers are explicit planned route components. Backend intrinsic capability policy is supplied through typed capability context before a transformation runs.

Optional SSA is also represented as route-owned transformations. It is not an implicit global rewrite hidden inside a central orchestrator.

Optimizers must preserve base semantics. A program may not depend on an optimizer being enabled in order to become correct.

## Backend stage

The final planned transformer produces the backend artifact for the exact backend route. Interpreter and CIL use their own selected backend components, but both are selected by the same `LanguagePlan` authority.

Execution is owned by the runtime/build session associated with that exact route. A backend must reject unsupported intrinsics rather than silently accepting them.

## Verification and route observation

Module-contract verification observes canonical route boundaries through a read-only route observer. It can validate Bytecode, AIR, optimized AIR and backend facts, and it may reject an invalid transition according to policy.

The observer does **not** choose features, contributions, optimizers, runtime providers or backends. Verification is therefore attached to the canonical route rather than implemented as a second composition workflow.

## Lifecycle

`LanguageRuntime` owns operation lifetime and disposal. Build-capable runtimes expose:

- `Build(LanguageArtifactBuildRequest)`;
- `ExecuteBuilt(LanguageArtifactBuildResult)`;
- typed access to a built artifact value.

The runtime lifetime gate prevents disposal from racing in-flight operations. There is no `AsyncLocal` prepared-execution slot in the canonical core path.

## Observability and tracing

The supported release does not include the old `logs.txt` text-log pipeline. That format covered only an older partial frontend view and did not represent AIR, optimized AIR, SSA route stages, verifier diagnostics, backend ownership or failed partial traces.

Future tracing should observe the canonical route boundaries without changing semantics. See [Debug Trace v2](/architecture/debug-trace-v2) and [Debug Trace Schema](/reference/debug-trace-schema).

## Stage ownership summary

| Stage | Owns | Must not own |
|---|---|---|
| `LanguageCompiler` | dependency/contribution/route selection | runtime execution |
| `LanguageRuntime` | exact plan verification, lifecycle, route execution | re-planning |
| Frontend transformer | source→bound syntax/AST | backend selection |
| Lexer | token recognition | AST shape |
| Parser | AST structure | backend behavior |
| Binder | external binding attachment | local runtime mutation |
| AST visitors | bytecode emission | raw source parsing |
| Bytecode hooks | bytecode transformations | parser fixes |
| AIR translator | backend-facing IR | runtime-provider discovery |
| Optimizers/SSA | semantics-preserving planned IR transforms | required base semantics |
| Backend | backend artifact creation/execution | language syntax decisions |
| Route observer | verification/diagnostics | route or contribution selection |

## Architecture lock

Architecture tests and `eng/retired-surface.json` make the S12 cutover mechanical:

- retired BasicCore orchestrator paths/symbols may not reappear;
- BasicCore cannot recreate an end-to-end owner that combines lexer, parser, translators, compiler and executor;
- canonical runtime code cannot infer runtime providers from optimized AIR;
- Wist canonical runtime cannot revive raw legacy intrinsic routing.

## Next

Continue with [Lexer](/internals/lexer).
