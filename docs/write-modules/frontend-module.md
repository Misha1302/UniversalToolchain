---
title: Frontend Module
description: Explain Wist syntax-module responsibilities and the phase-owned migration boundary.
---

# Frontend Module

A frontend/syntax contribution is the normal entry point for adding concrete syntax to Wist or to a UniversalToolchain-based DSL. It owns source preprocessing, lexemes, parser registration and syntax-tree processing. It does **not** own bytecode/AIR lowering merely because the historical Wist implementation class also implements lowering hooks.

## When to read this page

Read this page when you are adding syntax or enabling a feature through a `.wistdialect` file.

## Goal

Understand the real phase ownership boundary before going deeper into parser nodes, semantic binding or bytecode generation.

## Current implementation shape versus architecture ownership

Many existing Wist assemblies still implement the historical `IFrontendCoreModule` interface. That interface predates the current phase split and contains hooks for several phases:

```csharp
public interface IFrontendCoreModule
{
    void InitLexer(ILexer lexer) { }
    void InitParser(IParser parser) { }
    string ProcessText(string curCode) => curCode;
    List<LexemeValue> ProcessLexemes(List<LexemeValue> current) => current;
    AstNode ProcessAst(AstNode astRoot) => astRoot;
    IReadOnlyList<IAstBindingRule> GetAstBindingRules() => [];
    Bytecode ProcessBytecode(Bytecode current) => current;
    void InitAstTranslator(IAstToBytecodeTranslator translator) { }
    void InitAstTranslator(
        IAstToBytecodeTranslator translator,
        IReadOnlyList<IFrontendCoreModule> selectedModules)
        => InitAstTranslator(translator);
}
```

This combined interface is a **legacy implementation shape**, not the composition model. `WistLanguageFeaturePackage` declares separate plan contributions for the responsibilities a feature actually has, and the runtime materializes them independently:

```text
source
  -> syntax contribution
  -> semantic contribution
  -> lowering contribution
  -> optimizer contribution
  -> backend/runtime contribution
```

A feature may own only some of those roles. TextualAddition, for example, contributes concrete syntax but does not acquire a fake module lowerer; its syntax converges to canonical semantic `Add`, whose explicit semantic/lowering contributions own executable meaning. Variables contributes syntax, binding rules and lowering, so those are represented as separate planned roles even though the existing implementation class supplies their hooks.

The runtime never decides that a syntax contribution is also a lowerer by inspecting `IFrontendCoreModule` at execution time. Phase ownership is captured in `LanguagePlan`.

## Typical syntax contribution

For an existing Wist module, the syntax-facing portion often looks like:

```csharp
[DialectCapabilityProvider(typeof(global::ArithmeticModule.ArithmeticCapabilityProvider))]
[DialectComponentContract("FrontendModule", "Arithmetic")]
[AutoRegisterService]
public class ArithmeticModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer) => lexer.AddLexemes(...);
    public void InitParser(IParser parser) => parser.AddNodeCreators(...);

    // Historical implementation method. Its ownership is represented by a
    // separate lowering contribution; the syntax stage never invokes it.
    public void InitAstTranslator(IAstToBytecodeTranslator translator) =>
        translator.AddVisitors(...);
}
```

The class may still contain both hooks during migration, but syntax-stage activation and lowering-stage activation use independent stage-local instances selected from different contribution slots.

## Public dialect alias

The dialect alias is part of the user-facing API:

```text
use Arithmetic,Numbers,Scopes,Whitespaces
```

Choose aliases as stable feature names. Do not choose names that expose internal implementation details or temporary class names.

A dialect author should be able to understand what a module enables from the alias and the module documentation. Alias translation happens before planning; aliases are not runtime selection keys.

## Lexer registration

If the feature introduces new source syntax, the syntax contribution should register its own lexemes in `InitLexer`.

Examples:

- `Arithmetic` registers operator lexemes such as addition and multiplication.
- `Variables` registers `let`.
- `Loops` registers `while` and `for`.

Rules:

- keep token names stable once parser code depends on them;
- keep regex/token definitions close to the owning feature;
- avoid raw source scans for syntax that should be tokenized;
- prefer constants or centralized collections when token names are reused.

## Parser registration

If the feature creates concrete syntax structure, its syntax role should register parser node creators in `InitParser`.

Parser registration is not just plumbing. Node creator priority affects grammar behavior and conflict resolution.

Rules:

- use explicit priorities;
- document why the priority belongs near existing grammar constructs;
- test conflicts with nearby syntax;
- do not copy a priority only because it makes one example pass.

## Semantic binding

Binding rules are semantic ownership, not syntax ownership. Existing Wist modules may expose them through `GetAstBindingRules()`, but the syntax stage does not call that method. The semantic stage independently materializes the semantic contributions selected by `LanguagePlan`, runs `Binder`, and snapshots the result into `WistSemanticProgram`.

Do not move binding back into parser/frontend code to make an implementation convenient.

## Lowering registration

Bytecode visitors and `ProcessBytecode` behavior are lowering ownership. Existing module classes may expose those hooks through `InitAstTranslator`/`ProcessBytecode`, but they are invoked only on instances materialized from explicit lowering contributions.

A lowerer must self-filter. The translator may give multiple visitors a chance to see a semantic projection, so a visitor should return without emitting anything when the semantic node is not owned by that visitor.

Where multiple concrete syntax forms have the same meaning, lowering should consume the shared semantic identity rather than concrete spelling or parser-plugin identity. Symbolic `+` and textual `plus` are the canonical Wist example: both lower through semantic `Add`.

## Historical context-aware translator overload

`IFrontendCoreModule` still has this overload:

```csharp
void InitAstTranslator(
    IAstToBytecodeTranslator translator,
    IReadOnlyList<IFrontendCoreModule> selectedModules)
```

It receives the **lowering-stage** module set when invoked by the canonical Wist route. Do not use it to inspect syntax-only modules or to rediscover feature selection. New code should prefer explicit language-neutral contracts when a real reusable cross-component dependency exists; do not add a generic UT API merely to make Wist composition shortcuts easier.

## Ordering

Dialect `before`, `after` and `requires` directives are translated into `LanguageDefinition` contribution constraints before planning. When both features own a semantic or lowering role, the corresponding phase-owned constraints are propagated to those roles. Runtime code must not independently recompute module order.

## What a syntax/frontend contribution must not do

A syntax/frontend contribution should not:

- own bytecode/AIR lowering merely because the legacy class has lowering methods;
- run semantic binding inside parsing;
- make generic framework layers branch on its concrete type name;
- parse its syntax by scanning raw source after the parser;
- activate behavior only through hidden global state;
- silently depend on another module without typed feature/contribution constraints;
- select a backend, optimizer or lowering implementation at runtime;
- use concrete syntax spelling as a lowering discriminator when a semantic identity exists.

## Minimal checklist

Before accepting a feature module, verify:

- it has a stable dialect alias;
- syntax, semantic and lowering responsibilities are declared as separate plan contributions where they exist;
- syntax-stage code registers only syntax it owns;
- semantic binding runs after syntax and before bytecode lowering;
- parser priorities are intentional;
- lowerers self-filter on semantic ownership rather than syntax spelling/plugin identity;
- the feature can be selected and omitted through dialect files;
- disabled dialects reject the feature;
- interpreter/compiler behavior is tested when both backends support it;
- stage-local instances are not reused across sessions/phases unless an explicit lifetime contract permits that.

## Next

Continue with [Parser Extension](/write-modules/parser-extension). For the current end-to-end phase boundary, see [Wist phase ownership](/architecture/wist-phase-ownership).
