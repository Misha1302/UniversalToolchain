---
title: Wist 0.1.0-alpha.7 Candidate Stability
description: Source-candidate contract for Wist architecture and production hardening over the canonical LanguagePlan runtime.
audience: wist-application-developer
status: source-candidate
lastVerifiedAgainst: wist-release-readiness-2026-08-18
---

# Wist 0.1.0-alpha.7 candidate stability

`UniversalToolchain.Wist` `0.1.0-alpha.7` is the current source/release candidate for integration review. It is **not published on NuGet.org** and is not a stable 1.0 compatibility contract. The package currently exercised from NuGet.org remains `0.1.0-alpha.1`.

## Reviewed hardening surface

The canonical architecture remains:

```text
LanguageDefinition
    -> LanguageCompiler
    -> immutable LanguagePlan
    -> LanguageRuntime
    -> exact planned implementations
```

`LanguageCompiler.Compile(...)` remains the only public planner. `LanguageRuntime` materializes the immutable plan and does not select semantics, and `WistEngine` does not perform a second planning pass.

The alpha.7 hardening candidate adds or tightens:

- a typed `LanguageBuildRuntime` capability so execution-only runtimes do not advertise artifact-build operations they cannot perform;
- construction rollback that preserves the primary exception and its stack while retaining cleanup failures separately;
- a Wist failure taxonomy that distinguishes expected user/policy/unsupported failures from infrastructure/internal faults and fails fast for the latter;
- explicit same-instance Wist concurrency behavior: overlapping operations are rejected instead of being serialized by an undocumented lock;
- source-retention modes `Full`, `HashAndIdentity` and `None`, plus separate developer versus safe consumer diagnostic exposure;
- deterministic CI required-workflow ownership and fail-closed aggregate behavior;
- an evidence-backed physical runtime-closure classification without introducing a second package/runtime topology.

## Package candidate matrix

The source tree defines these unpublished candidate identities:

| Package | Source candidate |
|---|---|
| `UniversalToolchain.Language.Abstractions` | `0.3.0-alpha.4` |
| `UniversalToolchain.FeatureSdk` | `0.3.0-alpha.4` |
| `UniversalToolchain.LanguageSdk` | `0.3.0-alpha.5` |
| `UniversalToolchain.Runtime` | `0.3.0-alpha.5` |
| `UniversalToolchain.LanguageAuthoring` | `0.3.0-alpha.5` |
| `UniversalToolchain.Testing` | `0.3.0-alpha.5` |
| `UniversalToolchain.Templates` | `0.3.0-alpha.5` |
| `UniversalToolchain.Wist.LanguagePack` | `0.3.0-alpha.6` |
| `UniversalToolchain.Wist` | `0.1.0-alpha.7` |

These version changes reserve new identities for changed package payloads, including generated dependency metadata and the `ut-language` template's package references. They do **not** publish or promote any package.

## Public compatibility notes

The Wist facade adds typed failure, diagnostic-exposure and source-retention information. The reviewed Wist public API delta contains 23 intentional additions and no removals relative to the previous facade snapshot.

Generic runtime artifact-build callers should retain the result of the component-source factory as `LanguageBuildRuntime` (or `var`) when they need `Build`, `ExecuteBuilt` or `GetBuiltArtifactValue`. Execution-only code continues to use `LanguageRuntime`.

Low-level variable binding continues to expose the reviewed top-level `InvalidOperationException` family. A typed inner binding marker lets the Wist facade classify the same expected authoring failure as `UserInput` without treating arbitrary `InvalidOperationException` instances as invalid formulas.

## Privacy and concurrency boundary

`HashAndIdentity` stores a deterministic source hash and length instead of raw source. `None` stores neither raw source nor its hash in `WistProgramMetadata`. These policies are retention choices, **not secure secret scrubbing guarantees**: source may still exist transiently in caller/runtime memory or other application telemetry.

One `WistEngine` instance is not advertised as concurrently reentrant. Overlapping facade operations on the same instance fail fast. Use separate engine instances for independent concurrent operation streams. This is separate from disposal/lifetime coordination.

## Verification boundary

The source tree declares an exact repository test manifest of 1,323 tests across the canonical suites and keeps the existing benchmark smoke, PlanFuzz regression, architecture guards/mutants, package-surface and clean-consumer gates. The baseline-bearing package gate is required because package payload identities changed.

This document does **not** claim those gates are green for the final revision until the exact final commit has completed them. The pinned [verification snapshot](/evidence/current-verification) and the integration-review report are authoritative for the exact revision that was actually checked.

## Not promised

- publication of `0.1.0-alpha.7` or any related package;
- automatic merge or release promotion;
- OS/process sandboxing;
- secure wiping of source text from managed process memory;
- concurrent reentrancy of a single `WistEngine` instance;
- compatibility with every future alpha;
- universal performance superiority over handwritten C#;
- removal of semantic-only assemblies from the current monolithic Wist package closure.

## Promotion rule

Do not describe this candidate as the published package until:

1. the exact candidate revision passes the full baseline-bearing package gate and required CI suite;
2. an explicit release/publish decision is made separately;
3. the exact package is actually published;
4. the clean-room NuGet.org smoke succeeds for the published identity;
5. `eng/documentation-release-state.json` promotes `publishedVersion`;
6. public installation commands and evidence links pass the documentation release-state checks and mutants.
