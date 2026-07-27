---
title: External Language Authoring SDK
description: Typed authoring, cross-package composition, deterministic pass planning and runtime-policy contracts for independent languages.
---

# External Language Authoring SDK

This is the deep architectural reference. For a task-oriented path, start with [Quickstart](/language-authoring/quickstart), [Package Model](/language-authoring/package-model), [Artifact Routing](/language-authoring/artifact-routing) and [Runtime Lifecycle](/language-authoring/runtime-lifecycle).

The external authoring alpha models a language as an explicit graph of independently owned contributions rather than as one hardcoded Wist runtime profile:

```text
LanguageDefinition
-> explicit extension-package registry
-> feature and contribution resolution
-> slot, dependency, capability and conflict validation
-> typed conversion-route and same-artifact pass planning per backend
-> immutable LanguagePlan + schema-v5 lock snapshot
-> exact package-manifest binding
-> runtime assembly from all selected package component catalogs
-> runtime-policy validation
-> transformer/pass route + exact backend executor
```

## High-level authoring path

`UniversalToolchain.LanguageAuthoring` is the preferred API for a new language. The author registers each implementation once; the SDK derives the matching package descriptor and immutable runtime component-registration catalog from the same typed objects:

```csharp
var syntax = new LanguageArtifactKind<MySyntaxTree>("acme.syntax", "acme.syntax/v1");
var backend = new BackendId("interpreter");

var package = LanguagePackageBuilder.Create("Acme.Language", "1.0.0")
    .AddFeature("acme.core", feature => feature
        .AddTransformer(
            "acme.parse",
            LanguageSlots.FrontendParser,
            StandardLanguageArtifactKinds.SourceText,
            syntax,
            static (source, _) => Parse(source),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
        .AddPass(
            "acme.normalize",
            LanguageSlots.Optimizers,
            syntax,
            static (tree, _) => Normalize(tree),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
        .AddBackend(
            backend,
            new LanguageContributionId("acme.interpreter"),
            syntax,
            static (tree, _) => Execute(tree),
            LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
    .UseRouteRuntime("acme.runtime", "1.0.0")
    .Build();
```

The descriptor cannot claim `source.text<string> -> acme.syntax<MySyntaxTree>` while the runtime implementation accepts another CLR type: both are generated from the same `LanguageArtifactKind<T>` instances.

Package-level `AddTransformer`, `AddPass` and `AddBackend` registrations support backend-only, optimizer-only and infrastructure packages. An executable language may be composed entirely from package-level contributions and therefore does not require a synthetic feature solely to seed the graph.

The lower-level descriptor API remains available for package generators and manifest readers.

## Typed artifact contracts

An artifact contract has:

- a stable protocol ID chosen by the language author;
- an optional stable contract identity;
- a local CLR type used for runtime type checking.

```csharp
var syntax = new LanguageArtifactKind<MySyntaxTree>(
    "acme.syntax",
    "com.acme.syntax-tree/v1");
```

The explicit contract identity is the recommended public-package form because it does not depend on runtime or assembly versions. The default CLR-derived identity recursively excludes assembly versions, cultures and public-key tokens.

Route planning connects stages only when their artifact IDs and available type identities are compatible. A same-ID `object`/`string` mismatch is rejected with `UTL2201` during planning instead of failing inside `Run()`.

Planning-only untyped descriptors remain available as an explicit non-executable surface, but schema-v5 manifests, canonical executable plans and the generic route runtime require explicit type identities. Typed and untyped contracts never connect through wildcard semantics. New packages should use:

- `LanguageArtifactKind<T>`;
- `ArtifactTransformationDescriptor.Create<TSource,TTarget>`;
- `ILanguageArtifactTransformer<TSource,TTarget>`;
- `ILanguageArtifactExecutor<TInput,TResult>`.

## Conversions and same-artifact passes

Conversions and compiler passes are separate planning concepts:

- conversions change an artifact contract and form the minimum-cost route;
- passes preserve the contract, use `ContributionMergePolicy.Decorate`, and are inserted at the matching route stage;
- `Before` and `After` constraints produce a deterministic topological order;
- an ordering cycle fails with `UTL2202`;
- a selected pass whose artifact never occurs on the backend route fails with `UTL2204` instead of being silently ignored.

This directly models common pipelines such as:

```text
source.text -> syntax
syntax -> syntax       normalization
syntax -> AIR
AIR -> AIR             constant folding
AIR -> AIR             DCE
AIR -> executable
```

Authors do not need artificial `air.raw`, `air.optimized1` and `air.final` IDs merely to represent optimizer order.

## Cross-package runtime composition

Planning and execution use the same selected package graph. Every authored package exports:

```text
immutable LanguagePackageDescriptor
immutable LanguageRouteComponentCatalog
```

`LanguageRouteRuntimeAssembler` gathers implementations from all package component sources selected by the plan. It validates:

1. exact package ID and version;
2. Toolchain API compatibility;
3. the SHA-256 of the complete package manifest captured during planning;
4. presence and contracts of every selected transformer/pass;
5. presence of the exact backend contribution, backend ID and input contract;
6. presence of the package that owns the selected runtime-provider contribution.

A package with the same ID/version but different descriptor content cannot impersonate the package used to build the plan. A runtime provider cannot privately execute only its own package while ignoring frontend or optimization components from other packages.

Multiple executors may share a contribution ID only when their backend or input contract differs. Runtime selection still resolves the exact `(contribution, backend, input contract)` chosen by the plan.

## Runtime component ownership

Component catalogs store registrations and factories, not mutable transformer or executor instances. The safe default lifetime is `PerSession`: every `LanguageRuntime` session receives fresh components and owns their disposal. `SingletonStateless` is an explicit opt-in reserved for immutable, thread-safe components implementing `IStatelessLanguageRuntimeComponent`; disposable components cannot use that lifetime.

Disposal is coordinated with in-flight operations:

- new runs are rejected once disposal starts;
- disposal waits for active runs to leave the session;
- synchronous and asynchronous components are released in reverse construction order;
- repeated `Dispose` / `DisposeAsync` calls are idempotent;
- execution after disposal throws `ObjectDisposedException`.

A `PerSession` factory that returns an instance already used by another session is rejected instead of silently sharing mutable state.

## Configurable pipeline entry

Text is the compatibility default, not a framework invariant. `WithEntryArtifact(...)` allows a plan to begin from any typed artifact:

```csharp
var document = new LanguageArtifactKind<MyDocument>("acme.document", "acme.document/v1");

var definition = LanguageDefinitionBuilder.Create("Acme.Language", "1.0.0")
    .WithEntryArtifact(document)
    .UseFeature("acme.core")
    .EnableBackend("interpreter")
    .Build();

runtime.Run(LanguageExecutionRequest.FromArtifact(
    document,
    preparedDocument,
    new BackendId("interpreter")));
```

This supports pre-tokenized input, parsed documents, incremental syntax trees, binary formats and host-prepared IR without pretending they are strings. Empty or whitespace source text reaches the language frontend; the generic runtime does not impose a grammar rule.

## Planning-only languages

A definition with selected features but no backend is valid. It can represent a formatter, parser, linter, IDE service or analysis package. Such a plan has no runtime provider or routes, and `LanguageRuntime.Create` rejects attempts to execute it.

A runtime provider may be selected only for an executable definition. Runtime limits reject negative values at definition construction.

## Slots, capabilities and feature ownership

A feature is a user-visible language capability. A contribution is one implementation participant. Contributions declare:

- a slot such as frontend parser, lowering, optimizer, backend, tooling or runtime provider;
- single-owner or multi-owner multiplicity;
- contribution and capability requirements;
- contribution and capability conflicts;
- backend scope and deterministic order;
- optional `Before` / `After` pass constraints;
- an optional typed artifact transformation;
- for backend contributions, the typed executor input contract.

A contribution owned by a feature is eligible only when at least one owning feature is selected. Capability resolution cannot pull a contribution from an unselected or conflicting feature into the plan. Package-level infrastructure contributions have no feature owner and are eligible through explicit dependency/capability closure.

Single-owner conflicts fail closed. A language must make replacement explicit and may pin the expected old owner:

```csharp
.ReplaceSlot(
    LanguageSlots.FrontendParser,
    alternativeFrontend,
    expectedCurrentOwner: defaultFrontend)
```

Capability requirements may select their only eligible provider automatically. Ambiguous providers require `PreferCapabilityProvider`.

## Runtime policy is enforced

`RequireDeterminism` and `AllowHostInterop` are runtime gates, not decorative lock-file fields.

A provider must implement `ILanguageRuntimePolicyValidator`. The generic route provider validates the traits of all selected transformers, passes and executors before session creation:

- a deterministic plan rejects unknown or non-deterministic components;
- a plan that forbids host interop rejects components not explicitly declared host-interop-free;
- allowed host assemblies are rejected when host interop is disabled;
- every public runtime-provider entry point applies the same fail-closed rule.

The authoring API requires an explicit `LanguageRuntimeComponentTraits` argument for every transformer and executor. These traits are package attestations; hostile extensions still require process isolation and a trust model.

## Wist isolation and runtime boundary

The generic projects do not reference Wist:

- `UniversalToolchain.Language.Abstractions`;
- `UniversalToolchain.FeatureSdk`;
- `UniversalToolchain.LanguageSdk`;
- `UniversalToolchain.Runtime`;
- `UniversalToolchain.LanguageAuthoring`;
- `UniversalToolchain.Testing`.

`UniversalToolchain.Wist.LanguagePack` owns the typed Wist package and runtime provider. The provider validates that a plan contains the exact canonical Wist entry, frontend, lowering and backend route, converts the selected typed contributions to a deterministic `DialectDefinitionSlice`, and delegates execution to the canonical Wist dialect runtime. Unsupported custom routes are rejected instead of being silently ignored.

Wist package descriptor, runtime provider and NuGet package versions are derived from one assembly metadata source.

`samples/Acme.PricingLanguage` is an independent language: it owns its syntax model, parser, passes, interpreter backend and compiled backend and contains no Wist project reference.

## Template

`dotnet new ut-language` creates a standalone non-Wist language using `UniversalToolchain.LanguageAuthoring`. Template replacement controls package, feature, contribution, runtime and artifact identifiers; no `Acme.Pricing` or Wist identifier is embedded in generated projects.

## Manifest and lock contract

Schema v5 includes typed transformation endpoints, backend input contracts, pass ordering, runtime inputs, route steps and the plan entry artifact. It also defines `universaltoolchain-json-v1` canonicalization and SHA-256 over compact UTF-8 bytes without a BOM or platform-dependent line ending. Pretty JSON always uses LF but is not itself the package-identity input.

The reader accepts schema v5 only. Older manifest schemas must be migrated before loading; they are not normalized implicitly. Executable transformation endpoints, backend inputs and runtime inputs require explicit contract identities. Plan hashes include the entry artifact, selected contributions and typed route contracts, so changing an executable contract changes the reproducible plan identity.

## Current boundary

The SDK cleanly supports independent typed pipelines, cross-package execution, same-artifact passes and executable or planning-only languages. It does not yet provide a declarative high-level workbench for:

- grammar productions and precedence conflict analysis;
- symbol tables, binding and type-system authoring;
- operation definitions that automatically derive verifier/backend contracts;
- incremental parsing and IDE protocols;
- hardened hostile-extension sandboxing;
- stable 1.0 package compatibility.

Those are future authoring layers over the current contribution, pass, route and runtime foundation, not responsibilities that should be hardcoded into Wist or the generic runtime.
