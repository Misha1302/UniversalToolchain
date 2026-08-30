---
title: Handwritten pipeline vs planned composition
description: When UniversalToolchain is justified and when a direct pipeline is better.
audience: language authors, reviewers
status: proposed documentation-only hardening
---

# Handwritten pipeline vs planned composition

UniversalToolchain is not the right answer for every language implementation.

## Use a handwritten pipeline when everything is known

If the host application owns every component and every ordering decision, prefer a direct pipeline:

```csharp
language
    .UseParser(parser)
    .UseLowering(lowering)
    .UsePass(optimizer)
    .UseBackend(backend);
```

This is simpler, easier to debug and often faster to explain. There is no global composition problem if there are no independent contributors and no unresolved choices.

## Use planned composition when local declarations create global decisions

A planner becomes useful when packages are contributed independently and the host needs one reproducible whole-language result:

- which provider satisfies a required capability;
- which feature conflicts with another feature;
- which ordered pass set is selected;
- which artifact route reaches the requested backend;
- which runtime provider is allowed;
- which policy and provenance identify the selected configuration;
- which exact plan is materialized by runtime.

In that case, the planner does not erase complexity. It gives the complexity one explicit owner and emits an inspectable `LanguagePlan`.

## Decision rule

| Scenario | Prefer |
| --- | --- |
| One application, one known parser/lowering/pass/backend set | handwritten pipeline |
| Components are independently shipped and optional | planned composition |
| Runtime must not rediscover composition choices | planned composition |
| Need to explain selected provider/routes/provenance | planned composition |
| Need hardened untrusted-code sandboxing | neither; use separate process/security design |
| Need general dependency management | package manager / explicit compatibility layer, not current UT claim |
