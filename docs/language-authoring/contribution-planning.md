---
title: Contribution Planning and Diagnostics
description: Deterministic graph resolution, slot ownership, capabilities and planning failures.
---

# Contribution planning and diagnostics

`LanguageCompiler.Compile` converts a declarative `LanguageDefinition` and a package registry into an immutable `LanguagePlan`.

## Planning order

```text
validate Toolchain API
-> resolve selected features and feature dependencies
-> resolve contribution dependencies and capability closure
-> apply exclusions and explicit providers
-> validate contribution conflicts
-> resolve slot ownership and replacements
-> select runtime provider
-> build one typed artifact route per backend
-> insert same-artifact passes in deterministic order
-> compute canonical plan summary and hash
```

## Result handling

```csharp
LanguageBuildResult result = new LanguageCompiler(registry).Compile(definition);

if (!result.IsSuccess)
{
    foreach (var diagnostic in result.Diagnostics)
        Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
    return;
}

LanguagePlan plan = result.GetRequiredPlan();
```

A `LanguageDiagnostic` contains code, severity, stage, message, owner and suggested action. The current generic diagnostic families are listed in [Diagnostics](/reference/diagnostics).

## Determinism rules

- dependency traversal is normalized by stable IDs;
- a slot with more than one single owner fails instead of choosing enumeration order;
- ambiguous capability providers fail unless the definition selects one;
- route search uses typed contracts and explicit costs;
- same-artifact passes use deterministic topological ordering;
- ordering cycles fail;
- a selected pass that cannot be placed on a route fails rather than being ignored;
- the resulting plan has a canonical schema-v6 lock representation and plan hash.

## Ambiguity is not resolved by arbitrary ordering

Normalized IDs give stable traversal order, but they do not authorize a semantic choice. Ambiguous providers fail with `UTL2002`, multiple single-owner slot participants with `UTL2101`, feasible equal-cost routes with `UTL2207` and unresolved same-artifact pass order with `UTL2208`. Order cycles fail with `UTL2202`. Explicit preferences and ordering constraints belong to the language definition.

A successful plan certifies only that **declared structure** can be resolved. It does not verify whether passes preserve semantics or whether third-party implementations are trustworthy. Runtime traits are package attestations, not sandboxing.

## Planning-only definitions

A definition with features but no backend is valid. It can describe a formatter, parser, linter or IDE service. Such a plan has no runtime provider or executable routes; `LanguageRuntime.Create` rejects it.

Do not add a dummy backend to make an analysis-only package compile. Keep planning and execution distinct.

## Explicit entry artifact

Source text is only the compatibility default. `WithEntryArtifact` can start the plan from a host-prepared typed document, syntax tree, binary format or IR artifact. Route planning begins from that exact contract.
