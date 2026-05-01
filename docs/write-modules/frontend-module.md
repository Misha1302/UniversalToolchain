---
title: Frontend Module
description: Explain frontend module responsibilities.
---

# Frontend Module

A frontend module is the normal entry point for adding a language feature to Wist or to a UniversalToolchain-based DSL.

It should own the feature's public dialect name, lexemes, parser registration and AST-to-bytecode translator registration. It should not rely on generic framework code knowing the concrete feature name.

## When to read this page

Read this page when you are adding syntax or enabling a feature through a `.wistdialect` file.

## Goal

Understand what a frontend module is responsible for before going deeper into parser nodes, AST nodes or bytecode generation.

## Current interface shape

The core frontend module contract is `IFrontendCoreModule`. It has hooks for several stages of the pipeline:

```csharp
public interface IFrontendCoreModule
{
    void InitLexer(ILexer lexer) { }
    void InitParser(IParser parser) { }
    string ProcessText(string curCode) => curCode;
    List<LexemeValue> ProcessLexemes(List<LexemeValue> current) => current;
    AstNode ProcessAst(AstNode astRoot) => astRoot;
    Bytecode ProcessBytecode(Bytecode current) => current;
    void InitAstTranslator(IAstToBytecodeTranslator translator) { }
    void InitAstTranslator(IAstToBytecodeTranslator translator, IReadOnlyList<IFrontendCoreModule> selectedModules)
        => InitAstTranslator(translator);
}
```

Most feature modules use only a subset of these hooks. That is fine. A module should implement the hooks it actually owns.

## Typical module shape

A normal Wist feature module follows this shape:

```text
metadata attributes
  -> IFrontendCoreModule
  -> InitLexer
  -> InitParser
  -> InitAstTranslator
  -> dialect/runtime export metadata
  -> tests
```

For example, the arithmetic module exposes a dialect alias, registers arithmetic lexemes, registers parser node creators and registers AST visitors:

```csharp
[DialectModuleAlias("Arithmetic")]
[DialectCapabilityProvider(typeof(global::ArithmeticModule.ArithmeticCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "Arithmetic")]
[AutoRegisterService]
public class ArithmeticModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer) => lexer.AddLexemes(...);
    public void InitParser(IParser parser) => parser.AddNodeCreators(...);
    public void InitAstTranslator(IAstToBytecodeTranslator translator) =>
        translator.AddVisitors(...);
}
```

The exact implementation changes from module to module, but the ownership model should stay the same.

## Public dialect alias

The dialect alias is part of the user-facing API:

```text
use Arithmetic,Numbers,Scopes,Whitespaces
```

Choose aliases as stable feature names. Do not choose names that expose internal implementation details or temporary class names.

A dialect author should be able to understand what a module enables from the alias and the module documentation.

## Lexer registration

If the feature introduces new source syntax, the module should register its own lexemes in `InitLexer`.

Examples:

- `Arithmetic` registers operator lexemes such as addition and multiplication.
- `Variables` registers `let`.
- `Loops` registers `while` and `for`.

Rules:

- keep token names stable once parser code depends on them;
- keep regex/token definitions close to the owning module;
- avoid raw source scans for syntax that should be tokenized;
- prefer constants or centralized collections when token names are reused.

## Parser registration

If the feature creates AST structure, the module should register parser node creators in `InitParser`.

Parser registration is not just plumbing. Node creator priority affects grammar behavior and conflict resolution.

Rules:

- use explicit priorities;
- document why the priority belongs near existing grammar constructs;
- test conflicts with nearby syntax;
- do not copy a priority only because it makes one example pass.

## Translator registration

If the feature produces executable semantics, the module usually registers AST visitors in `InitAstTranslator`.

A visitor must self-filter. The translator may give multiple visitors a chance to see a node, so a visitor should return without emitting anything when the node is not owned by that visitor.

## Context-aware translator registration

`IFrontendCoreModule` also has this overload:

```csharp
void InitAstTranslator(
    IAstToBytecodeTranslator translator,
    IReadOnlyList<IFrontendCoreModule> selectedModules)
```

Use it only when translator setup genuinely depends on the selected module set. Keep this dependency explicit and tested. Do not use it to hide hardcoded composition shortcuts.

## What a frontend module must not do

A frontend module should not:

- make generic framework layers branch on its concrete type name;
- parse its syntax by scanning raw source after the parser;
- activate behavior only through hidden global state;
- silently depend on another module without documenting and testing that dependency;
- register backend-specific behavior as if it were general frontend behavior.

## Minimal checklist

Before accepting a frontend module, verify:

- it has a stable dialect alias;
- it registers only syntax it owns;
- parser priorities are intentional;
- AST visitors self-filter;
- the module can be selected and omitted through dialect files;
- disabled dialects reject the feature;
- interpreter/compiler behavior is tested when both backends support it.

## Next

Continue with [Parser Extension](/write-modules/parser-extension).
