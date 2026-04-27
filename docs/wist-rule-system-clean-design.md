# Wist rule system clean design

This document describes the target design for the Wist rule system after removing raw-source syntax shortcuts.

Status (April 26, 2026): the temporary raw-source rule parser and RuleSet public surface were removed during cleanup.
`RuleDeclarationsModule` is also removed from runtime-visible composition until parser-owned declarations exist.
Future implementation must use Wist parser/AST-owned structure only.

The goal is intentionally modest: make the rule feature correct, parser-owned, and maintainable without introducing an oversized language-workbench architecture.

## Problem

The rule system must not recognize Wist syntax by scanning raw source text.

Production code must not use regular expressions, line splitting, substring checks, or one-off scanners to rediscover language constructs such as `rule`, parameters, bodies, or `let` bindings.

Those facts must come from the owning syntax pipeline.

## Design principles

1. Parser owns syntax.
2. Extractors consume parser output, not raw source text.
3. Validators consume declaration models, not source strings.
4. Resolvers consume symbols and descriptors, not profile-specific names.
5. The facade only orchestrates existing services.
6. Runtime execution uses the existing compiled-artifact/session path.
7. The first clean version should stay small and direct.

## Target pipeline

```text
raw Wist source
-> Wist lexer/parser
-> Wist AST
-> rule declaration extractor from AST
-> rule declaration model
-> rule validation
-> rule compilation through the normal Wist compiler path
-> compiled rule set
-> runtime argument binding
-> compiled artifact session execution
```

There must be no parallel path like this:

```text
raw Wist source
-> regex / Split / IndexOf / manual brace scanner
-> rule model
```

## Minimal model additions

The rule system only needs a small structured model.

```csharp
public sealed record RuleDeclarationModel(
    string Name,
    IReadOnlyList<RuleParameterModel> Parameters,
    RuleTypeDescriptor ReturnType,
    RuleBodyModel Body,
    SourceSpan Span);

public sealed record RuleBodyModel(
    AstNode Root,
    IReadOnlyList<LocalBindingDeclarationModel> LocalBindings);

public sealed record LocalBindingDeclarationModel(
    string Name,
    int DeclarationOrder,
    ScopeId ScopeId,
    SourceSpan Span);
```

The exact type names may follow existing project conventions, but the responsibilities should stay the same.

## Parser-owned syntax

Rule declarations should be represented in the Wist syntax layer.

The parser or a parser-owned module should produce structured nodes for:

- rule declaration name;
- parameter list;
- return type;
- body root;
- local binding declarations discovered by the normal Wist syntax pipeline.

A rule extractor may exist, but it must be parser-backed. It should walk AST nodes or a parser-produced declaration tree.

It must not read `string source` and search for keywords manually.

## Validation

Validation should be split into small validators over structured models.

Suggested validators:

- `WistRuleNameValidator` for duplicate rule names.
- `WistRuleParameterValidator` for duplicate parameters and unsupported parameter types.
- `WistRuleLocalBindingValidator` for duplicate locals and parameter shadowing.
- `WistRuleReturnTypeValidator` for return type checks.

Validators should receive `RuleDeclarationModel` or `RuleSetDeclarationModel`.

They must not receive raw body text.

## Compilation

Rule compilation should reuse the normal Wist compilation path.

For each validated rule:

1. Create declared bindings from rule parameters.
2. Compile the rule body through the existing Wist artifact compiler.
3. Store the resulting artifact with a rule descriptor.

The rule compiler should not know product profile names such as pricing, validation, or policy profiles.

## Runtime argument binding

Runtime argument validation should be isolated from compiled rule execution.

Suggested component:

```csharp
public interface IRuleArgumentBinder
{
    RuleArgumentBindingResult Bind(
        CompiledRuleDescriptor descriptor,
        IReadOnlyDictionary<string, object?> arguments);
}
```

The binder should produce diagnostics for:

- unknown argument;
- missing argument;
- null argument when not allowed;
- type mismatch;
- failed runtime value conversion.

`CompiledWistRule` should only create a session, set bound arguments, and run the artifact.

## Facade shape

`WistRuntimeFacade` should stay thin.

It may expose convenience methods such as:

```csharp
public RuleSetCompileResult CompileRuleSet(string source, string mode = "compiler");
```

But the method should delegate to an injected or composed `IWistRuleSetCompiler`.

The facade should not instantiate extractors, validators, or runtime-specific rule services directly.

## Implementation phases

### Phase 1: Stop the architectural violation

- Remove regex and line-splitting rule body validation.
- Add parser-backed local binding extraction or leave local-binding validation incomplete with an explicit limitation.
- Add guardrail tests proving production validators do not parse raw source text.

This phase is allowed to be conservative. Incomplete validation is better than a second parser.

### Phase 2: Add the minimal structured model

- Represent rule declarations and local bindings as structured parser output.
- Add AST-backed rule extraction.
- Move duplicate-local and parameter-shadowing checks onto the structured model.

### Phase 3: Thin facade and service split

- Introduce `IWistRuleSetCompiler`.
- Move extraction, validation, compilation, and descriptor creation out of `WistRuntimeFacade`.
- Keep public facade methods as convenience wrappers.

### Phase 4: Strengthen tests

Add tests for:

- comments containing `rule` or `let`;
- strings containing `rule` or `let`;
- multiline local declarations;
- duplicate locals in the same scope;
- same local name in independent rule scopes;
- parameter shadowing;
- interpreter/compiler parity for compiled rule sets;
- unknown, missing, null, and wrong-type arguments.

## Non-goals

This design does not require:

- a full type-checker rewrite;
- a new runtime;
- a new backend;
- a separate rule language;
- a large control-flow or data-flow framework for the first rule cleanup;
- product-specific behavior for pricing, validation, or policy profiles.

## Acceptance criteria

A clean implementation is acceptable when:

1. Rule declarations are extracted from parser-owned structure.
2. Local binding validation does not scan raw body text.
3. The facade delegates rule compilation instead of becoming a rule engine.
4. Runtime arguments are bound through a dedicated binder.
5. Interpreter and compiler backends produce the same observable rule results.
6. Architecture tests prevent reintroducing raw-source syntax recognition outside syntax owners.

## Summary

The clean rule system should be small but strict.

The parser owns syntax, extractors expose structure, validators check structure, compilers reuse the normal Wist pipeline, and the facade stays a convenience layer.

This removes the main architectural defect without turning the rule feature into an over-engineered subsystem.
