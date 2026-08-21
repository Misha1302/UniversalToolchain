---
title: UniversalToolchain Documentation
description: Choose the smallest supported documentation route for your role.
audience: all
status: current
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# UniversalToolchain documentation

UniversalToolchain is a modular .NET compiler/runtime framework with two public entry surfaces:

- **`UniversalToolchain.Wist`** for validating restricted formulas and compiling approved expressions into typed delegates;
- **External Language Authoring SDK** for composing independent non-Wist languages from typed packages, contributions, artifact routes and runtime components.

Wist is the reference language and compatibility proving ground. It is not the mandatory frontend or IR of every language built with the generic SDK.

## Choose your role

<div class="ut-paths">
  <a class="ut-path" href="./start/">
    <span class="ut-path-title">Wist application developer</span>
    <span class="ut-path-text">Install the package, validate formulas, compile typed delegates and integrate rule updates into a .NET host.</span>
  </a>
  <a class="ut-path" href="./language-authoring/">
    <span class="ut-path-title">External language author</span>
    <span class="ut-path-text">Build an independent non-Wist language package with typed artifacts, deterministic planning and exact backend routing.</span>
  </a>
  <a class="ut-path" href="./build-dsls/">
    <span class="ut-path-title">Wist dialect author</span>
    <span class="ut-path-text">Compose a smaller Wist language/runtime profile from shipped modules, optimizers and backends.</span>
  </a>
  <a class="ut-path" href="./write-modules/">
    <span class="ut-path-title">Wist compiler contributor</span>
    <span class="ut-path-text">Add syntax, AST translation, Bytecode/AIR behavior, intrinsics or backend support to the reference language.</span>
  </a>
  <a class="ut-path" href="./architecture/learning-path">
    <span class="ut-path-title">Framework contributor or learner</span>
    <span class="ut-path-text">Choose a 30-minute, two-hour or subsystem-specific route before opening implementation internals.</span>
  </a>
  <a class="ut-path" href="./SECURITY">
    <span class="ut-path-title">Security or platform reviewer</span>
    <span class="ut-path-text">Review trust boundaries, source retention, trace limitations, component lifecycle and process-isolation requirements.</span>
  </a>
  <a class="ut-path" href="./evidence/maintainer-guide">
    <span class="ut-path-title">Maintainer or evaluator</span>
    <span class="ut-path-text">Inspect the current verification baseline, package matrix, release status and known gaps.</span>
  </a>
</div>

## Wist first contact

```csharp
using UniversalToolchain.Wist;

using var engine = WistEngine.CreateRestrictedArithmetic();

const string source = "usage * 0.7 + reliability * 0.3 - incidents * 15.0";
var program = engine.Compile<Func<double, double, double, double>>(
    source,
    "usage",
    "reliability",
    "incidents");

double score = program.CompiledDelegate(100.0, 90.0, 1.0);
```

The formula returns data. The host owns authorization, persistence, approval, rollback, side effects and process-level resource isolation. Continue with [First Program](/start/first-program) or the [Production Integration guide](/start/production-integration).

## External language authoring model

```text
package registrations
  -> immutable package descriptor + component catalog
  -> LanguageDefinition
  -> deterministic LanguagePlan + schema-v5 lock snapshot
  -> exact runtime package assembly
  -> typed artifact route
  -> exact backend executor
```

The alpha SDK supports typed artifact contracts, contribution dependencies and conflicts, capability-provider selection, deterministic pass ordering, configurable entry artifacts and per-session lifecycle. It does not generate parsers, binders, type systems or IDE tooling. Continue with the [Language Authoring Quickstart](/language-authoring/quickstart).

## Documentation authority

Public current documentation lives under `docs/`. Maintainer policies, proposals, dated reviews, conference material and historical reports live under `internal-docs/` and are excluded from the VitePress source tree.

When documents conflict, use this order:

1. current implementation and executable tests;
2. current public reference and architecture pages;
3. evidence records tied to a named artifact;
4. internal proposals, reviews and archive material.
