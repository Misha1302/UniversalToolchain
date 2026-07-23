---
title: Composition and Plan Explainability
description: Current explain surfaces for Wist dialect composition and generic language plans.
---

# Composition and plan explainability

UniversalToolchain has two explainability surfaces.

## Wist dialect composition explanation

`UniversalToolchain.Dialects.Integration` exposes:

```text
DialectFrameworkCompositionResult
-> DialectCompositionExplanationProjector
-> DialectCompositionExplanation
-> DialectCompositionExplanationFormatter
```

The projection preserves module order, runtime selections, resolution state, diagnostics and directives without inventing resolved components for unknown selection types. It is an explanation snapshot, not a replacement source of truth.

See [Current explain-plan surface](/current-explain-plan-surface).

## Generic `LanguagePlan`

The external SDK exposes immutable planning output directly:

- resolved features and contributions;
- selected runtime-provider contribution and provider reference;
- backend-to-artifact routes;
- ordered route steps and total cost;
- definition/runtime policy;
- canonical plan summary and `PlanHash`;
- schema-v5 lock serialization through `LanguageLockFile`.

A useful plan report should show at least:

```text
language ID/version
entry artifact contract
selected package ID/version/manifest hash
selected features
selected contributions grouped by slot
runtime provider
one ordered route per backend
runtime policy
planning diagnostics
plan hash and lock schema version
```

## Authority boundary

- the package registry and compiler own planning truth;
- the immutable plan owns selected execution truth;
- formatted explanations are projections;
- runtime assembly revalidates package identity and selected component contracts;
- internal proposal graphs are not runtime truth.
