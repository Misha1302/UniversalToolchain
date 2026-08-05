---
title: Use-case Recipes
description: Copy-ready Wist examples for pricing, rollout scoring and LMS scoring.
audience: wist-application-developer
status: current
lastVerifiedAgainst: wist-release-state-2026-08-06
---

# Use-case Recipes

These examples use the public `UniversalToolchain.Wist` facade and the restricted arithmetic preset. They deliberately keep side effects in the .NET host:

```text
rule text -> validate or compile -> numeric result -> host application decides the action
```

<!-- wist-published-install:begin -->
Install the package currently exercised by the clean-room NuGet.org smoke:

```bash ci-run=false
dotnet add package UniversalToolchain.Wist \
  --version 0.1.0-alpha.1 \
  --source https://api.nuget.org/v3/index.json
```
<!-- wist-published-install:end -->

The repository may contain a newer unpublished source candidate. Recipes do not silently switch package feeds or versions.

## Pricing and commission

Compile a reviewed commission formula once and invoke the typed delegate for each order:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateRestrictedArithmetic();

var commission = rules.Compile<Func<double, double, double, double>>(
    "gross * rate - fixedFee",
    "gross",
    "rate",
    "fixedFee");
double payout = commission.CompiledDelegate(5_000.0, 0.05, 25.0);
Console.WriteLine(payout); // 225
```

The rule returns a number. The host still owns payment creation, authorization, persistence and audit logging.

## Rollout score

Keep rollout policy configurable without letting the formula enable a feature directly:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateRestrictedArithmetic();

var rolloutScore = rules.Compile<Func<double, double, double, double>>(
    "usage * 0.7 + reliability * 0.3 - incidents * 15.0",
    "usage",
    "reliability",
    "incidents");
double score = rolloutScore.CompiledDelegate(100.0, 90.0, 1.0);
bool enableNewDashboard = score >= 80.0;
```

The threshold and feature switch remain ordinary reviewed .NET code.

## LMS or autograding score

Use a small rule for a score that changes more often than the surrounding application:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateRestrictedArithmetic();

var finalScore = rules.Compile<Func<double, double, double, double, double>>(
    "correct * pointsPerTask - penalties * penaltyPoints",
    "correct",
    "pointsPerTask",
    "penalties",
    "penaltyPoints");
double score = finalScore.CompiledDelegate(18.0, 5.0, 2.0, 3.0);
Console.WriteLine(score); // 84
```

Enrollment state, grade publication and permissions stay outside the rule.

## Reject a broader language shape

Validate before storing an admin-, config- or LLM-suggested formula:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateRestrictedArithmetic();

var validation = rules.Validate(
    "let score = correct * pointsPerTask\nscore",
    new
    {
        correct = 18.0,
        pointsPerTask = 5.0
    });
Console.WriteLine(validation.IsValid); // false
Console.WriteLine(string.Join("; ", validation.Diagnostics.Select(diagnostic => diagnostic.Message)));
```

The restricted arithmetic surface intentionally rejects statement-style bindings such as `let`.

## Integration checklist

Before using a configurable rule in an application:

1. Define the exact numeric inputs owned by the host.
2. Validate the text before storing or activating it.
3. Compile once after approval; do not compile inside the hot loop.
4. Keep side effects, authorization and persistence in ordinary .NET code.
5. Store the last-known-good rule so an invalid update does not replace working behavior.
6. Treat restricted syntax as a language-surface limit, not as process isolation or a hardened sandbox.

## When not to use this alpha

Do not use the restricted arithmetic preset when you need arbitrary C# execution, process isolation, execution timeouts, memory quotas or a stable full business-rule language.

## Next

Read the [Wist `0.1.0-alpha.6` source-candidate stability record](/evidence/wist-stability-v0.1.0-alpha.6), the pinned [verification snapshot](/evidence/current-verification), the [Performance Model](/reference/performance-model) and [Security](/SECURITY) before moving beyond the currently published package.
