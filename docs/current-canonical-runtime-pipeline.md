# Current Canonical Runtime Pipeline

This document describes the currently supported Wist runtime composition flow in this repository.
It is intentionally limited to behavior that exists today.

## Canonical pipeline

1. **Dialect DSL compilation** — `DialectDslCompiler` parses `.wistdialect` text into a typed dialect slice.
2. **Language-definition translation** — `WistFacadeLanguageDefinitionFactory` translates requested module aliases, backend choice, security/intrinsic policy, ordering constraints and exclusions into `LanguageDefinition`.
3. **Canonical planning** — `LanguageCompiler` is the only owner that closes feature dependencies, resolves contribution/capability providers, applies exclusions and order constraints, selects the runtime provider, and builds backend artifact routes.
4. **Immutable plan** — the result is one `LanguagePlan` containing the exact resolved features, contributions, provider identity and backend routes.
5. **Runtime materialization** — `LanguageRuntime` binds only the exact runtime graph selected by that plan to exact package/component sources. Materialization does not make new semantic selection decisions.
6. **Execution or build** — source follows the planned route `Source -> Syntax/AST -> Bytecode -> AIR -> optimizers/optional SSA -> backend artifact -> execution`.

For the public Wist facade, `WistEngine.Create` performs steps 1–5 once and keeps the resulting `LanguagePlan` and `LanguageRuntime` for subsequent `Evaluate`, `Validate` and `Compile` calls.

## Canonical runtime constraints

- There is no second Wist build-plan or selected-runtime-plan owner after `LanguageCompiler`.
- Feature dependency closure belongs to `LanguageCompiler`; a dialect may request a high-level feature without manually repeating all of its required features.
- `exclude` directives become `LanguageDefinition.ExcludedContributions`. If dependency closure requires an excluded contribution, planning fails closed instead of silently reactivating it.
- Runtime component sources are provenance-checked against the exact package instances and manifests captured by `LanguagePlan`.
- Runtime materialization requires sources only for components that are actually part of the executable runtime graph; unrelated tooling-only planned contributions do not become runtime dependencies.
- Backend availability and route order come from `LanguagePlan`, not from service-registration or reflection enumeration order.
- Restricted host interop is enforced by the typed runtime policy and explicit allowed-assembly boundary.

## Ownership boundary

- `UniversalToolchain.Language.Abstractions` owns typed language, feature, contribution, backend and artifact contracts.
- `UniversalToolchain.FeatureSdk` owns package descriptors and exact package registration identity.
- `UniversalToolchain.LanguageSdk` owns deterministic planning and `LanguagePlan` construction.
- `UniversalToolchain.Runtime` owns exact plan verification, runtime materialization, lifecycle and route execution.
- `UniversalToolchain.Wist.LanguagePack` translates Wist configuration into generic language contracts and supplies Wist implementations for already-planned contributions.
- `UniversalToolchain.Wist` is the public facade over the canonical plan/runtime path.

`UniversalToolchain.Dialects.Wist` remains only as a non-packable compatibility project for a small set of compatibility data/contracts. It is not a planner, runtime selector or execution host, and canonical Wist production projects are guarded from depending on it.

## What is intentionally retired

The S11 architecture does not use `DialectBuildPlan`, `SelectedRuntimePlan`, `SelectedRuntimePlanResolver`, `ToolchainCompositionWorkflow`, `WistDialectExecutionWorkflow`, `WistDialectExecutionHost` or `WistDialectPlanFactory` as production owners. Permanent architecture tests reject reintroduction of these retired paths/symbols.
