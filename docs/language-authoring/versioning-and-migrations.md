---
title: Package Versioning and Migrations
description: Preserve artifact, contribution and runtime identities across independently shipped language packages.
audience: external-language-author
status: current-alpha-guidance
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# Package versioning and migrations

External language plans bind package identity, contribution identity, artifact routes and runtime-provider identity. Versioning is therefore part of runtime correctness, not only release labeling.

## Identities that form a contract

Treat the following identifiers as persistent public contracts once another package or stored definition depends on them:

- package ID and package version;
- feature IDs;
- contribution IDs;
- capability IDs;
- backend IDs;
- runtime-provider ID and version;
- artifact kind IDs and their CLR contract types;
- single-owner slot ownership;
- manifest and lock schema version.

Renaming one of these values is not a cosmetic refactor for external consumers.

## Compatible and incompatible changes

| Change | Typical classification | Required action |
|---|---|---|
| add an optional feature with new IDs | additive | publish a new package version; existing definitions remain valid |
| add a backend without selecting it by default | additive | add parity and route tests |
| change diagnostics without changing plan semantics | potentially compatible | preserve stable diagnostic codes where callers depend on them |
| change transformation cost or provider preference | plan-affecting | publish a new version and regenerate expected plans/locks |
| rename artifact or contribution ID | breaking | introduce a new ID and an explicit migration path |
| change CLR type associated with an artifact kind ID | breaking contract violation | use a new artifact kind ID |
| change runtime-provider implementation under the same manifest identity | invalid release practice | publish a new version and manifest hash |
| remove a selected contribution or capability | breaking | fail planning with a migration diagnostic; do not silently substitute |

## Do not mutate a stored plan in place

A `LanguagePlan` and schema-v5 lock snapshot describe the exact resolved graph used for runtime assembly. When packages change:

1. load the stored language definition or application-owned configuration;
2. resolve it against the new package registry;
3. inspect structured diagnostics and the new normalized plan;
4. compare selected package versions, routes, providers and policy;
5. run language-owned compatibility tests;
6. activate the new plan only after approval;
7. retain the previous package set and plan for rollback during the observation window.

Do not edit hashes, routes or provider identities inside a previously produced plan to make runtime assembly accept a different package graph.

## Artifact evolution

When an artifact representation changes incompatibly, introduce a new typed identity:

```csharp
var syntaxV1 = new LanguageArtifactKind<ExpressionV1>("contoso.rules.syntax.v1");
var syntaxV2 = new LanguageArtifactKind<ExpressionV2>("contoso.rules.syntax.v2");
```

A migration package can contribute an explicit transformer from `v1` to `v2`. This makes the conversion visible to planning, route hashing, tests and runtime policy.

Do not keep the old string ID while changing the CLR type. The generic runtime validates typed route contracts precisely so that this drift fails early.

## Contribution replacement

Single-owner slots require explicit replacement semantics. A new parser, binder, type system or backend owner must not win because of registration order.

For a replacement release:

- declare the replacement in the language definition or supported package contract;
- pin the expected previous owner when accidental replacement would be dangerous;
- add a test for both the accepted replacement and ambiguous/unexpected-owner rejection;
- document the semantic migration separately from the mechanical package upgrade.

## Runtime-provider upgrades

A runtime provider must match the provider identity and version selected by the plan. Upgrade it as an independently versioned contract:

- publish a new provider version;
- resolve a new plan;
- prove lifecycle, executor selection and policy behavior again;
- do not claim compatibility only because the public method signatures remained unchanged.

## Consumer migration checklist

Before shipping a package-family update:

1. build the full package matrix;
2. create a clean consumer using only produced packages;
3. create a clean `ut-language` template project;
4. compile representative stored definitions;
5. compare normalized plans and lock snapshots;
6. run backend parity and lifecycle tests;
7. verify old definitions fail with actionable diagnostics when intentionally unsupported;
8. update [Language Authoring evidence](/evidence/language-authoring-alpha) and the release note.

## Alpha boundary

The generic API is still alpha. Compatibility must be demonstrated per release; package version numbers alone do not constitute a long-term compatibility guarantee. Consumers should pin exact versions and retain the package set used to produce an active plan.
