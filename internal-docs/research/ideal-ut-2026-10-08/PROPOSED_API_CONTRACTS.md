# Proposed API sketches — NOT current UniversalToolchain public types

**DESIGN ONLY, NOT COMPILE-CHECKED AGAINST CURRENT UT.** Derived from the source ZIP's `04_TARGET_ARCHITECTURE_AND_APIS.md`; shown to preserve interface shape and design intent while guarding against accidental implementation claims. A future PR must discover exact shipped types/overloads before selecting namespaces and binary contracts.

## 1. Existing config owner; optional ergonomic language authoring facade

```csharp
// PROPOSED convenience facade (RuleLanguage, UsingGrammar, RulesBinder):
var rules = RuleLanguage.Create("Acme.Policy", version: "1.0")
    .Syntax(s => s.UsingGrammar("rule NAME when EXPR then EXPR"))
    .Semantics(s => s.Register(new RulesBinder()))
    .Diagnostics(d => d.RequireNoUnboundNames())
    .Backend(Backends.Interpreter)
    .BuildPackage();

// EXISTING architectural chain remains:
// LanguagePackageRegistry -> LanguageDefinitionBuilder ->
// LanguageCompiler.Compile(definition) -> immutable LanguagePlan.
// No facade may construct a second parser, planner or runtime registry.
```

## 2. Versioned semantic provider and exact world scope

```csharp
// PROPOSED illustrative contracts, not installed APIs.
public interface ISemanticProvider<TSubject, TValue>
{
    SemanticProperty<TSubject, TValue> Property { get; }
    ValueTask<SemanticEvidence<TValue>> QueryAsync(
        SemanticSubject<TSubject> subject,
        SemanticQueryContext context,
        CancellationToken cancellationToken);
}
```

`SemanticQueryContext` must identify frozen plan, actual selected executable package/implementation digests, program snapshot/version, IR and phase, subject/program-point/path, backend target, assumptions/observation model, trust policy, budget, cancellation and provenance. Stable schema IDs require independent compatible-version/migration rules; string equality of trait names is insufficient. This is a *local selected-world query*, not another package selector.

## 3. Judgments, obligations and explicit decline

```csharp
// PROPOSED conceptual call; no concrete method/type is currently promised.
var interval = await semantics.QueryAsync(StandardProperties.Range, index, world, ct);
var length   = await semantics.QueryAsync(StandardProperties.ArrayLength, array, world, ct);
var legality = obligations.Check(
    BoundsSafety.At(index, array, programPoint),
    interval, length, world);

if (legality.Status != ObligationStatus.Proven)
    return TransformResult.NotApplicable(legality.Diagnostics);
```

Expected 4-state evidence algebra: `+ only → Proven`, `- only → Disproven`, `none → Unknown`, `both → Contradiction`. **Proven is under a named trust policy**, not an unconditional theorem. Missing/expired/untrusted evidence must never authorize a destructive rewrite. Asserted predicates are not independently checked by declaration alone.

## 4. Transformation effects, invalidation and mapping

```csharp
// PROPOSED protocol; current module contracts may already cover subsets.
public interface IGuardedProgramTransform
{
    string Id { get; }
    IReadOnlyList<ObligationSchema> Preconditions { get; }
    ValueTask<ProgramTransformResult> TryApplyAsync(
        ProgramSnapshot snapshot,
        SemanticContext context,
        CancellationToken cancellationToken);
}

public sealed record ProgramTransformResult(
    ProgramSnapshot NewSnapshot,
    IReadOnlyList<AnchorMappingWitness> Mappings,
    IReadOnlySet<SemanticSchemaId> ClaimedPreservedProperties,
    IReadOnlySet<SemanticSchemaId> InvalidatedProperties,
    EvidenceCertificate? PreservationCertificate);
```

A producer's `ClaimedPreservedProperties` is not itself a certificate. Consumers need independent trusted verification or recomputation after effects, exception/FP/overflow/alias changes. For one-to-many AST↔AIR remapping, every transferred fact needs a precise transport rule and valid witness; otherwise return Unknown.

## 5. Runtime, IDE and AI layering

- **Current** selected `LanguagePlan` + `LanguageRuntime` owns runtime materialization and declared exact backend execution.
- **Proposed** `UT.SemanticContracts` only after successful independent N+1 test; optional `UT.SemanticQueries` only after a second consumer; neither installed by this research PR.
- **Proposed** `UT.LanguageAuthoring.Rules`, `UT.Tooling.Lsp`, `UT.Tooling.Dap`, `UT.AI` remain optional adapters—do not create package names/classes just because a diagram suggests them.
- AI may `QuerySymbols`, `ExplainDiagnostic`, `PreviewEdit`, `ValidatePatch` and propose a versioned structured diff; applying edits requires policy/human authorization and a matching document revision. It cannot self-assert `Proven`, select a different package than `LanguagePlan` or silently execute code.

## Compatibility gates before ANY implementation

Identify owner and existing typed contracts; design smallest reusable surface; try a direct C# interface implementation first; check serialization/version evolution, cancellation and deterministic diagnostics; test wrong schema, stale SHA, conflicting fact, negative fact, incomplete dependencies, generated facade hidden state and bad transformation. Publish stable APIs **only** after UT-bound external consumer, CI and reverse dependency audit.
