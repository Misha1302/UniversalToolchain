---
title: Public Claim Ledger
description: Evidence and safe wording for public UniversalToolchain claims.
---

# Public Claim Ledger

Use this ledger before README, talk, social or landing-page updates.

| Claim | Evidence location | Evidence class | Allowed wording | Forbidden wording |
|---|---|---|---|---|
| UniversalToolchain is a reusable .NET DSL/runtime framework | `readme.md`, `docs/start/what-is-universal-toolchain.md`, generic `UniversalToolchain.Language.*` / `UniversalToolchain.Runtime` projects | Observed code/docs | "experimental .NET framework for composing DSL runtimes" | "any language workbench" |
| Wist is the reference language | `AGENTS.md`, `internal-docs/policies-and-reports/ARCHITECTURE_RULES.md`, Wist projects | Observed project rule | "Wist is the reference language and proving ground" | "Wist assumptions are gone everywhere" |
| Wist runtime selection is `LanguagePlan`-backed | `UniversalToolchain.LanguageSdk/LanguageCompiler.cs`, `UniversalToolchain.Runtime`, `UniversalToolchain.Wist.LanguagePack`, `Tests/Architecture/WistMigrationGateTests.cs`, `docs/current-canonical-runtime-pipeline.md` | Observed tests/code/docs | "Wist configuration is translated to `LanguageDefinition`, planned once by `LanguageCompiler`, and materialized/executed by `LanguageRuntime` from the resulting `LanguagePlan`" | "Wist runtime selection is manifest-backed"; "manifest-backed selected runtime plans are the current Wist execution model" |
| Generic dialect integration still contains a neutral runtime-host helper | `UniversalToolchain.Dialects.Integration/ToolchainRuntimeHost.cs` | Observed code | "the generic dialect integration layer retains a neutral runtime-host helper for its own compatibility/integration contracts" | "`ToolchainRuntimeHost` is the canonical Wist execution host"; "Wist public execution uses selected dialect runtime plans" |
| Generic runtime-profile definitions exist | `UniversalToolchain.Dialects.Integration/RuntimeProfileDefinition.cs`, `RuntimeProfileDefinitionBuilder.cs`, profile tests | Observed code/tests | "the generic dialect integration subsystem has runtime-profile data for source-level dialect defaults" | "runtime profiles are the current Wist semantic planner"; "Wist S11 execution depends on runtime-profile overlays" |
| Wist facade alpha exists | `UniversalToolchain.Wist`, package smoke tests, `docs/evidence/wist-stability-v0.1.0-alpha.3.md` | Observed code/tests/docs | "`UniversalToolchain.Wist` is an alpha first-contact facade for formula evaluation, validation and typed compiled invocation over the canonical LanguagePlan runtime" | "stable 1.0 formula platform" |
| FunctionCalls and SafeMath MVP exist | `FunctionCallsModule`, `SafeMathFunctionsModule`, capability/source execution tests, `docs/CURRENT_ARCHITECTURE_STATUS.md` | Observed code/tests | "limited FunctionCalls/SafeMath MVP with covered provider-backed calls" | "complete function authoring/runtime system" |
| Directive syntax can be extended | `DialectDslExtensibilityTests`, `DialectDirectiveHandlerRegistryTests` | Observed tests | "custom dialect directive syntax and semantic handlers can be registered through extension contracts" | "all third-party dialect APIs are stable forever" |
| Structured trace exists | `wistc run --trace`, trace writer tests | Observed code/tests | "redacted `trace.json` v2 artifact phase is implemented" | "full pipeline viewer is done" |
| External contribution planning exists | `UniversalToolchain.LanguageSdk`, `UniversalToolchain.Runtime`, `ExternalLanguageAuthoringTests`, `docs/architecture/external-language-authoring-sdk.md` | Observed code/tests | "alpha contribution graph with explicit slots, capability resolution, deterministic artifact routes and a generic route runtime" | "finished arbitrary-language workbench" |
| SSA exists | `UniversalToolchain.Ssa.*`, `Tests/Ssa` | Observed code/tests | "alpha SSA route with verifier and round-trip tests for a subset" | "production SSA optimizer/backend layer" |
| Benchmarks cover separated measurement stories | benchmark project README, `FormulaHotPathBenchmarks`, `FormulaConvenienceBenchmarks`, `FormulaCompilationBenchmarks`, and retained raw artifacts when run | Benchmark evidence required | "the benchmark suite separates hot prepared invocation, convenience evaluation overhead, and cold compilation cost; claims require recorded artifacts" | "Wist is faster than C#" |
| Sandbox/security | `docs/SECURITY.md`, README caveats | Documentation/policy | "compiled execution is not a sandbox boundary" | "safe untrusted script sandbox" |

## S11 architecture wording rule

For current Wist architecture, public material must preserve this ownership chain:

```text
LanguageDefinition -> LanguageCompiler -> LanguagePlan -> LanguageRuntime
```

Runtime manifests, generic `ToolchainRuntimeHost`, runtime-profile types and compatibility dialect infrastructure may still exist elsewhere in the repository, but they must not be described as a second current Wist planner, selector or execution host.

## Publication Rule

If a claim is not in this table, add current evidence before using it publicly. If the evidence is only a roadmap, dated review or proposal, word it as future/history rather than current implementation.
