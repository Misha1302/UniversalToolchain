# Module Authoring Guide

This guide documents the expected pattern for adding language features to UniversalToolchain and Wist.

It describes current project contracts. Do not treat nearby modules as sufficient documentation when adding new behavior.

## Goal of a module

A module should own one coherent language or runtime capability.

A frontend feature module usually owns:

- its public dialect alias;
- its capability declarations;
- its lexemes;
- its parser node creators;
- its AST translation visitors;
- its runtime exports and manifest metadata;
- its feature tests.

A module must not make generic framework layers aware of concrete Wist feature names.

## Standard frontend module shape

A typical frontend module follows this shape:

```text
attributes
-> IFrontendCoreModule
-> InitLexer
-> InitParser
-> InitAstTranslator
-> tests
-> explicit component contract + typed package registration
```

Use this as the default feature recipe unless the feature is intentionally not a frontend feature.

## Required metadata

A module that participates in dialect/runtime composition should provide explicit metadata instead of relying on type names.

Common metadata includes:

- dialect module alias;
- capability provider metadata;
- runtime export metadata;
- automatic service registration metadata when applicable.

The alias is part of the dialect authoring surface. Choose it as a stable public name, not as an implementation detail.

## Lexer ownership

A module that introduces syntax must register its own lexemes in its lexer initialization path.

Rules:

- Do not recognize module syntax through raw source scans outside the lexer/parser/AST pipeline.
- Do not add framework-level string checks for module syntax.
- Keep lexeme names stable once other parser or visitor code depends on them.
- Prefer centralized constants for token names when a token is shared across files.

## Parser ownership

A module that introduces AST forms must register parser node creators in its parser initialization path.

Rules:

- Parser priorities are semantic. Treat them as part of the grammar contract.
- Do not choose a priority by copying a nearby value without explaining why the feature belongs there.
- Avoid changing existing priorities unless the intended grammar change is covered by tests.
- Add conflict tests when a new node creator can overlap with existing syntax.

## AST translation ownership

A module that lowers AST nodes must register visitors in its AST translator initialization path.

The current translator pattern can call multiple visitors for the same node. Therefore every visitor must be defensive:

- return without emitting when the node is not owned by that visitor;
- avoid double-emitting for nodes already handled by another feature;
- keep visitor state per compilation unless the state is immutable;
- document any intentional cooperation with another visitor.

## Shared state between visitors

Shared state is allowed only when it is explicit and scoped.

Good shared state:

- is created per module setup or per compilation as appropriate;
- has one narrow responsibility;
- is passed explicitly to cooperating visitors;
- has tests proving no leakage between separate compilations.

Bad shared state:

- global mutable dictionaries;
- state hidden behind static fields;
- state that depends on previous compilations;
- state shared only because it was convenient to access.

## Runtime and IR participation

Some modules may participate beyond frontend translation, for example as IR-processing modules or optimizer providers.

If a module participates in more than one pipeline layer, document that explicitly:

- frontend syntax responsibility;
- bytecode/AIR responsibility;
- optimizer responsibility;
- runtime/backend dependency;
- backend capability requirements.

Do not rely on the manifest kind alone as the full architecture boundary. A selected component may implement multiple contracts; that must be intentional and tested.

## Required tests for a new module

A non-trivial feature module should include tests for:

- lexeme recognition;
- parser ownership and precedence;
- AST translation output;
- dialect selection or rejection;
- interpreter behavior when supported;
- CIL behavior when supported;
- interpreter/CIL semantic parity when both are supported;
- negative cases for disabled dialect profiles;
- deterministic composition when ordering matters.

When the feature touches optimizers or intrinsics, add backend-capability tests and semantic-preservation tests.

## Checklist before merging a module

- The module has a stable dialect alias.
- The module does not require generic framework code to branch on its concrete name.
- Lexeme names are documented or centralized.
- Parser priorities are intentional and tested.
- Visitors self-filter and do not double emit.
- Any shared state is explicit and scoped.
- The module can be excluded by dialect composition.
- Restricted dialects reject the feature when it is not selected.
- Interpreter and CIL behavior is tested when both backends support the feature.
- The module follows `internal-docs/policies-and-reports/PROJECT_RULES.md` and `internal-docs/policies-and-reports/ARCHITECTURE_RULES.md` (repository-only policies).
