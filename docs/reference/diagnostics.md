---
title: Diagnostics Reference
description: Stable Wist facade codes and generic language-planning diagnostics.
audience: all-technical-users
status: current-reference
lastVerifiedAgainst: wist-release-state-2026-08-06
---

# Diagnostics reference

## Wist facade codes

| Code | Stage / meaning |
|---|---|
| `UTC-WIST-001` | source-length preflight limit |
| `UTC-WIST-002` | external-parameter-count preflight limit |
| `UTC-WIST-LEX-001` | lexer failure |
| `UTC-WIST-PARSE-001` | parser failure |
| `UTC-WIST-DIALECT-001` | dialect composition failure |
| `UTC-WIST-RESOLVE-001` | type/member resolution failure |
| `UTC-WIST-RESOLVE-002` | ambiguous resolution |
| `UTC-WIST-COMPILE-001` | backend compilation failure |
| `UTC-WIST-SSA-001` | experimental SSA route failure |
| `UTC-WIST-EXEC-001` | execution failure |
| `UTC-WIST-VALIDATE-001` | validation failure not classified above |
| `UTC-WIST-999` | unexpected internal failure |

`WistDiagnostic` exposes code, severity, stage, source span, message and hints. Expected formula failures should be consumed through `Validate` or `TryCompile`; do not parse exception text as an API.

### Reserved facade codes

Five constants and one test fixture code are declared in `ToolchainDiagnosticCodes` but no production site raises
them today. They are recorded here rather than deleted, because each names a failure the layer is expected to
distinguish and the constant is the only place that decision is written down:

| Code | Reservation |
|---|---|
| `UTC-FUNC-001` | unknown function; the resolver reports its own diagnostics today |
| `UTC-FUNC-002` | function unavailable; no caller distinguishes this from an unknown function yet |
| `UTC-FUNC-003` | function unsupported on backend; the backend gate reports through its own codes |
| `UTC-BIND-001` | unknown binding; the expression typing layer reports a context diagnostic instead |
| `UTC-BIND-002` | binding conflict; no two-binding conflict path exists yet |
| `UTC-TYPE-001` | type mismatch; used to exercise `ToolchainDiagnostic` in tests, not raised in production |

The diagnostic-code gate keeps this list honest: a reserved code that starts being raised fails the gate as a
stale reservation, and a dead code that is not reserved fails it as an undocumented phantom. Reserving a name is
a claim about the future, so it is a reviewable claim rather than an accident.

## Generic Language SDK planning codes

| Family | Codes | Meaning |
|---|---|---|
| feature graph | `UTL1001`–`UTL1003`, `UTL1203` | missing feature, feature conflict/cycle, unsupported backend |
| Toolchain API | `UTL1501` | definition/package API incompatibility |
| contribution graph | `UTL2001`–`UTL2006`, `UTL2010`–`UTL2014` | missing/excluded contribution, dependency cycle, capability provider resolution, conflicts and ownership eligibility |
| slot ownership | `UTL2101`–`UTL2103`, `UTL2110`–`UTL2112` | multiple owners or invalid explicit replacement; definition-level contribution order referencing an unselected contribution, crossing slots, or cyclic |
| typed routes and passes | `UTL2201`–`UTL2209` | no compatible route, pass cycle, backend owner ambiguity, unplaceable pass, violated definition-level order, violated descriptor order, ambiguous minimum-cost route, ambiguous ready pass at equal order, contribution not covered on an enabled backend |
| runtime provider | `UTL2301`–`UTL2304` | missing/ambiguous provider, missing execution input, provider removed by override |
| semantic judgments | `UTL2401`–`UTL2407` | obligation unknown/disproved/inconsistent, derivation depth bound, predicate arity, stale epoch evidence, trust precedence |

## Module-contract verification codes

These belong to `UniversalToolchain.ModuleContracts` and are raised while verifying that the selected
modules agree with each other: their contracts, ownership, pipeline effects, bytecode and AIR. They are
symbol-shaped rather than numeric because each one names the artifact it judges.

Every code is declared as a named constant in `ModuleContractDiagnosticCodes` and referenced by the
verifier that raises it. Codes are listed individually rather than as ranges on purpose: the numbering has
gaps (the contract family has no member numbered 004), so a range would advertise a code that does not
exist.

| Family | Codes | Meaning |
|---|---|---|
| contract table and selection | `UT-CONTRACT-001`–`UT-CONTRACT-003`, `UT-CONTRACT-005`–`UT-CONTRACT-009` | duplicate facet, duplicate contract id, identifier outside its owning namespace; schema downgrade, undeclared module, new or declared module missing a descriptor, facet kind without deterministic order |
| AST and lowerer ownership | `UT-MOD-OWNERSHIP-001`–`UT-MOD-OWNERSHIP-003` | AST node with no lowering owner, with multiple owners, or lowerer handling a node another module owns |
| compiler facts | `UT-COMPILER-FACT-001`–`UT-COMPILER-FACT-003` | a fact with several owners, referenced but not provided by any selected module, or produced by a module that does not own it |
| pipeline effects | `UT-PIPELINE-EFFECT-001`–`UT-PIPELINE-EFFECT-005` | effect that both produces and invalidates a fact, required fact missing, reverification not performed, missing pipeline order, module listed twice in one stage |
| bytecode | `UT-BYTECODE-TAG-001`, `UT-BYTECODE-TAG-002`, `UT-BYTECODE-SOURCE-001`, `UT-BYTECODE-PATTERN-001`, `UT-BYTECODE-STACK-001`, `UT-BYTECODE-METADATA-001` | unknown tag, undeclared producer, undeclared source node, unknown pattern, stack effect disagreeing with the declaration, contract metadata declared twice |
| AIR | `UT-AIR-SCHEMA-001`, `UT-AIR-STACK-001`, `UT-AIR-BRANCH-001`, `UT-AIR-BRANCH-002`, `UT-AIR-INTRINSIC-001`, `UT-AIR-INTRINSIC-002`, `UT-AIR-CAPABILITY-001` | invalid operand schema, stack discipline violated, missing branch target, duplicate label, intrinsic unsupported by the selected backend, intrinsic outside the universal interpreter surface, missing backend capability |
| backend capabilities | `UT-BACKEND-CAPABILITY-001`, `UT-BACKEND-CAPABILITY-002` | selected capability no contract declares, several capability facets competing |

`UT-AIR-INTRINSIC-001` is the earliest point an unsupported intrinsic is reported today, and
`UT-BACKEND-CAPABILITY-002` is a hard error rather than a warning: two facets offering the same capability
mean the pipeline cannot decide which one is in force.

## Handling pattern

```csharp
var result = compiler.Compile(definition);
foreach (var diagnostic in result.Diagnostics)
{
    Console.Error.WriteLine(
        $"[{diagnostic.Severity}] {diagnostic.Code} " +
        $"({diagnostic.Stage}): {diagnostic.Message}");
}
```

Codes identify a contract class. Messages and suggested actions may become more precise during alpha; avoid persisting full English message text as a compatibility key.
