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

## Generic Language SDK planning codes

| Family | Codes | Meaning |
|---|---|---|
| feature graph | `UTL1001`–`UTL1003`, `UTL1203` | missing feature, feature conflict/cycle, unsupported backend |
| Toolchain API | `UTL1501` | definition/package API incompatibility |
| contribution graph | `UTL2001`–`UTL2006`, `UTL2010`–`UTL2014` | missing/excluded contribution, dependency cycle, capability provider resolution, conflicts and ownership eligibility |
| slot ownership | `UTL2101`–`UTL2103` | multiple owners or invalid explicit replacement |
| typed routes and passes | `UTL2201`–`UTL2204` | no compatible route, pass cycle, backend owner ambiguity, unplaceable pass |
| runtime provider | `UTL2301`–`UTL2304` | missing/ambiguous provider, missing execution input, provider removed by override |

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
