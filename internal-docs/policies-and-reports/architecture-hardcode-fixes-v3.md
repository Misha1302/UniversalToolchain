# Wist2 architecture hardcode fixes v3

> Superseded by `docs/architecture-hardcode-fixes-v4.md`. This file is retained as historical context for the v3 pass. Do not use its verification limits or status as current release truth.

Date: 2026-07-09

## Scope

This patch closes the highest-priority boundary and hardcode findings from the architecture review:

1. reusable language modules no longer depend on `UniversalToolchain.Wist.Contracts`;
2. runtime provider allowlists are owned by backend/runtime composition instead of being extracted from AIR;
3. production optimizers and runtime planning use a typed `CallCSharp` reader instead of comparing the legacy `"call C#"` display string;
4. architecture guardrail tests were strengthened to protect these boundaries.

## Design decisions

### Module-owned facts

`IdentifierModule` and `ScopesModule` now own their compiler fact IDs through:

- `IdentifierModule.Contracts.IdentifierFacts`;
- `ScopesModule.Contracts.ScopesFacts`.

`VariablesModule` depends on these module-owned contracts directly, because variable binding semantically depends on identifier and scope facts. `UniversalToolchain.Wist.Contracts` keeps obsolete compatibility aliases for Wist-facing callers, but generic modules do not reference those aliases.

### Composition-owned runtime provider policy

`PreparedExecutionBuilder` no longer scans optimized AIR to infer allowed runtime providers. Instead, it reads `IRuntimeProviderPolicyComponent` instances from selected backend pipeline components.

The Wist CIL and interpreter backend registrations explicitly allow:

- `ExternalRuntimeCallProvider`;
- `VariablesRuntimeCallProvider`.

This makes AIR a request surface, not the source of authorization.

### Typed CallCSharp access

`CSharpCallIntrinsicReader` centralizes decoding of both typed and legacy `CallCSharp` instructions. Production optimizers now use this reader instead of inspecting raw operands and display strings.

Legacy strings remain only in compatibility/decoder/registry surfaces that intentionally bridge old AIR encoding.

## Verification performed

All commands below were run from `UniversalToolchain/` using the sidecar .NET SDK 10.0.301 and `-p:Platform='Any CPU'` to neutralize ambient platform settings.

Passed builds:

- `BasicCore/BasicCore.csproj`
- `IdentifierModule/IdentifierModule.csproj`
- `ScopesModule/ScopesModule.csproj`
- `VariablesModule/VariablesModule.csproj`
- `NativeMathModule/NativeMathModule.csproj`
- `ConditionsModule/ConditionsModule.csproj`
- `BytecodeDynamicMethodsCompiler/BytecodeDynamicMethodsCompiler.csproj`
- `UniversalToolchain.Dialects.Wist/UniversalToolchain.Dialects.Wist.csproj` with `-p:EmitDialectRuntimeManifest=false`

Manual guardrail checks passed:

- no `UniversalToolchain.Wist.Contracts`, `WistIdentifierFacts`, or `WistScopesFacts` references remain in `IdentifierModule`, `ScopesModule`, or `VariablesModule`;
- no raw `"call C#"` comparison remains in `NativeMathModule`, `ConditionsModule/Optimizers`, `BytecodeDynamicMethodsCompiler/Compilers/CilExecutionRequirementAnalyzer.cs`, or `BasicCore/Core/PreparedExecutionBuilder.cs`;
- `PreparedExecutionBuilder` no longer contains `ExtractAllowedRuntimeProviderTypes`.


Clean-unpack verification after archive creation:

- the produced archive was extracted into a fresh directory;
- no `bin/`, `obj/`, `.git`, `.vs`, `.dll`, `.pdb`, `.exe`, or accidental `input.sha256` files were present in the archive;
- the same manual architecture guardrails passed from the clean unpack;
- `BasicCore/BasicCore.csproj` and `VariablesModule/VariablesModule.csproj` restored and built from the clean unpack with `-p:EmitDialectRuntimeManifest=false`.

## Known verification limits

The full `Tests/Tests.csproj` restore was not completed in the constrained offline environment. The architecture tests were updated and manually mirrored with grep-based checks, but a full test-suite run still needs to be performed in a normal developer environment.

Building `UniversalToolchain.Dialects.Wist` with dialect runtime manifest emission enabled timed out in the constrained environment while executing the manifest emitter across the graph. The same project compiled successfully with manifest emission disabled, so this result validates compile-time consistency but not the manifest emission path.
