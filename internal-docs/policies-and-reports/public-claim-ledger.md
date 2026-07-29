---
title: Public Claim Ledger
description: Evidence and safe wording for public UniversalToolchain claims.
---

# Public Claim Ledger

Use this ledger before README, talk, social or landing-page updates.

| Claim | Evidence location | Evidence class | Allowed wording | Forbidden wording |
|---|---|---|---|---|
| UniversalToolchain is a reusable .NET DSL/runtime framework | `readme.md`, `docs/start/what-is-universal-toolchain.md`, code projects under `UniversalToolchain.Dialects.*` | Observed code/docs | "experimental .NET framework for composing DSL runtimes" | "any language workbench" |
| Wist is the reference language | `AGENTS.md`, `internal-docs/policies-and-reports/ARCHITECTURE_RULES.md`, Wist projects | Observed project rule | "Wist is the reference language and proving ground" | "Wist assumptions are gone everywhere" |
| Runtime selection is manifest-backed | `SelectedRuntimePlanResolver`, runtime manifest tests | Observed tests/code | "manifest-backed selected runtime plans" | "all runtime behavior is fully plugin-independent" |
| Neutral runtime host exists | `ToolchainRuntimeHost`, `ToolchainRuntimeRunRequest`, runtime-profile tests | Observed code/tests | "neutral runtime host executes selected dialect runtime plans" | "public runtime API is fully extracted into a standalone package" |
| Runtime profiles exist | `RuntimeProfileDefinition`, `RuntimeProfileDefinitionBuilder`, `RuntimeProfileApplicator`, profile tests | Observed code/tests | "runtime profiles can apply source-level dialect defaults with diagnostics, provenance and a fluent builder API" | "runtime profiles are a complete package manager or deployment profile system" |
| Wist facade alpha exists | `UniversalToolchain.Wist`, package smoke tests, `docs/evidence/wist-stability-v0.1.0-alpha.3.md` | Observed code/tests/docs | "`UniversalToolchain.Wist` is a alpha first-contact facade for formula evaluation, validation and typed compiled invocation" | "stable 1.0 formula platform" |
| FunctionCalls and SafeMath MVP exist | `FunctionCallsModule`, `SafeMathFunctionsModule`, capability/source execution tests, `docs/CURRENT_ARCHITECTURE_STATUS.md` | Observed code/tests | "limited FunctionCalls/SafeMath MVP with covered provider-backed calls" | "complete function authoring/runtime system" |
| Directive syntax can be extended | `DialectDslExtensibilityTests`, `DialectDirectiveHandlerRegistryTests` | Observed tests | "custom dialect directive syntax and semantic handlers can be registered through extension contracts" | "all third-party dialect APIs are stable forever" |
| Structured trace exists | `wistc run --trace`, trace writer tests | Observed code/tests | "redacted `trace.json` v2 artifact phase is implemented" | "full pipeline viewer is done" |
| External contribution planning exists | `UniversalToolchain.LanguageSdk`, `UniversalToolchain.Runtime`, `ExternalLanguageAuthoringTests`, `docs/architecture/external-language-authoring-sdk.md` | Observed code/tests | "alpha contribution graph with explicit slots, capability resolution, deterministic artifact routes and a generic route runtime" | "finished arbitrary-language workbench" |
| SSA exists | `UniversalToolchain.Ssa.*`, `Tests/Ssa` | Observed code/tests | "alpha SSA route with verifier and round-trip tests for a subset" | "production SSA optimizer/backend layer" |
| Benchmarks cover separated measurement stories | benchmark project README, `FormulaHotPathBenchmarks`, `FormulaConvenienceBenchmarks`, `FormulaCompilationBenchmarks`, and retained raw artifacts when run | Benchmark evidence required | "the benchmark suite separates hot prepared invocation, convenience evaluation overhead, and cold compilation cost; claims require recorded artifacts" | "Wist is faster than C#" |
| Sandbox/security | `docs/SECURITY.md`, README caveats | Documentation/policy | "compiled execution is not a sandbox boundary" | "safe untrusted script sandbox" |

## Publication Rule

If a claim is not in this table, add evidence before using it publicly. If the evidence is only a roadmap or proposal,
word it as future work.
