---
title: Showcase: controlled rules
description: Show how UniversalToolchain turns tiny user, admin or AI-suggested formulas into validated and compiled .NET decisions.
---

# Showcase: controlled rules

UniversalToolchain is easiest to understand as a controlled rule layer for .NET applications.

A product manager, admin UI, config file or LLM can suggest a small numeric rule:

```text
usage * 0.7 + reliability * 0.3 - incidents * 15.0
```

The application owns the inputs, validates the rule against a restricted Wist formula surface, compiles the accepted rule once, and decides what the returned score means.

## Full example

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateSafeFormulas();

var rolloutScore = rules.Compile<Func<double, double, double, double>>(
    "usage * 0.7 + reliability * 0.3 - incidents * 15.0",
    "usage",
    "reliability",
    "incidents");

double score = rolloutScore.CompiledDelegate(100.0, 90.0, 1.0);
bool enableNewDashboard = score >= 80.0;
```

The Wist formula does not enable the dashboard. It only computes a value. The host application owns the side effect.

## Validation before execution

The safe-formula preset intentionally starts narrow:

```csharp
using UniversalToolchain.Wist;

using var rules = WistEngine.CreateSafeFormulas();

var rejected = rules.Validate(
    """
    let score = usage * 0.7
    score
    """,
    new
    {
        usage = 100.0,
        reliability = 90.0,
        incidents = 1.0
    });

Console.WriteLine(rejected.IsValid); // false
Console.WriteLine(rejected.Message);
```

Statement-style bindings such as `let` are not enabled by the restricted safe-formula preset. This is the intended product model: the application controls which language features exist.

## Why not just JSON?

JSON is good for data. It becomes painful when the data structure is really a programming language:

```json
{
  "subtract": [
    {
      "add": [
        { "multiply": ["usage", 0.7] },
        { "multiply": ["reliability", 0.3] }
      ]
    },
    { "multiply": ["incidents", 15.0] }
  ]
}
```

The equivalent formula is easier to read, validate, review and explain:

```text
usage * 0.7 + reliability * 0.3 - incidents * 15.0
```

## Why not C# scripting?

C# scripting is powerful, but often broader than the surface you want to expose to an admin, config file or AI-generated suggestion.

UniversalToolchain is for the middle ground:

```text
not JSON-rule trees
not arbitrary C# scripting
but a small language surface your application owns
```

## Current preview boundary

This page demonstrates the current safe numeric/formula path. Full object-shaped business rules, string targeting rules and hardened sandboxing are not claimed as stable 1.0 features in this preview.

Restricted dialects reduce the available language/runtime surface. They are not process isolation or a hardened security boundary.
