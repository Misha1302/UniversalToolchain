---
title: Current Explain-Plan Surface
description: Explain planning diagnostics and immutable LanguagePlan output.
navigation: hidden
status: Internal contributor reference; linked from architecture explainability docs.
---

# Current explain-plan surface

The current explainability source is the same immutable object that owns execution selection: `LanguagePlan`.

`LanguageCompiler.Compile(...)` returns a `LanguageBuildResult` containing planning diagnostics and, on success, the exact `LanguagePlan`. The plan exposes its `Summary`, selected features/contributions, runtime provider, backend routes, runtime policy and `PlanHash`; lock serialization provides a stable machine-readable projection.

## Authority boundary

- `LanguageCompiler` owns planning decisions and diagnostics.
- `LanguagePlan` owns the selected immutable graph.
- `LanguageRuntime` verifies/materializes that exact graph and does not re-plan it.
- Any human-readable report is a projection of these typed values, never a second composition model.

The former `UniversalToolchain.Dialects.Integration` composition-explanation/runtime-selection model was retired in S13 together with its second runtime topology.
