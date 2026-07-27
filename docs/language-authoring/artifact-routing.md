---
title: Typed Artifact Routing
description: Conversion routes, same-artifact passes and exact backend executors.
---

# Typed artifact routing

An executable language plan contains one route for every enabled backend. A route starts at the definition entry artifact, applies selected transformers and passes, and ends at the exact artifact contract required by the selected backend executor.

## Typed contract

```csharp
var syntax = new LanguageArtifactKind<MySyntaxTree>(
    "acme.syntax",
    "com.acme.syntax-tree/v1");
```

A contract has:

- stable artifact kind ID;
- optional stable public contract identity;
- local CLR type identity for runtime checking.

For independently versioned packages, use an explicit stable contract identity. The CLR-derived identity intentionally excludes assembly version, culture and public-key token, but it still describes a CLR-shaped local contract rather than a cross-ecosystem protocol.

Typed and planning-only untyped contracts do not connect through wildcard semantics. The generic route runtime requires typed executable contracts.

## Conversions versus passes

A conversion changes the contract:

```text
source.text<string> -> acme.syntax<MySyntaxTree>
acme.syntax<MySyntaxTree> -> ir.air<MyAir>
```

A pass preserves the contract and decorates a route stage:

```text
ir.air<MyAir> -> ir.air<MyAir>
```

The planner finds a minimum-cost conversion route, then inserts applicable passes. `Before` and `After` constraints determine pass order. Passes do not need fake artifact names such as `air.optimized1` merely to express sequencing.

## Backend selection

A backend contribution declares:

- backend ID;
- contribution ID;
- exact input artifact contract;
- executor implementation/factory;
- runtime component traits.

Runtime assembly resolves the exact tuple `(contribution, backend, input contract)`. Reusing a contribution ID for multiple executors is allowed only when backend or input contract differs; ambiguous exact matches fail.

## Non-text entry

```csharp
var document = new LanguageArtifactKind<MyDocument>(
    "acme.document",
    "com.acme.document/v1");

var definition = LanguageDefinitionBuilder.Create("Acme.Language", "1.0.0")
    .WithEntryArtifact(document)
    .UseFeature("acme.core")
    .EnableBackend("interpreter")
    .UseRuntimeProvider("acme.runtime", "1.0.0")
    .Build();

var request = LanguageExecutionRequest.FromArtifact(
    document,
    preparedDocument,
    new BackendId("interpreter"));
```

The runtime validates the request contract against the plan entry contract before invoking any transformer.
