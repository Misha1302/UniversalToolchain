---
title: Restricted DSL Security
description: Explain the boundary between dialect restriction and real sandboxing.
---

# Restricted DSL Security

A restricted dialect is a composition boundary. It is useful, but it is not a complete security sandbox.

## When to read this page

Read this before exposing Wist or a UniversalToolchain-based DSL to user-authored formulas, customer configuration, pricing rules or workflow scripts.

## Goal

Understand what restricted dialects can honestly guarantee, what they cannot guarantee and what must be handled by the host application.

## What a restricted dialect does

A restricted dialect can limit the selected runtime surface:

- selected frontend modules;
- selected optimizers;
- selected backend modes;
- selected intrinsic policies;
- declared capabilities;
- trusted or restricted security intent.

This is valuable because omitted modules should make their syntax unavailable. For example, a pricing dialect can omit C# interop, loops or labels.

## What a restricted dialect does not do

A restricted dialect does not automatically provide:

- process isolation;
- CPU or memory limits;
- timeout enforcement;
- protection from all host API exposure;
- protection from bugs in selected modules or backends;
- validation that business rules are safe or fair.

Do not document a dialect as safe for arbitrary untrusted third-party code only because it has `security restricted`.

## Recommended host model

Use layered protection:

```text
narrow dialect
  -> explicit input bindings
  -> no trusted interop unless intended
  -> backend availability checks
  -> timeouts and resource limits outside the DSL runtime
  -> process/container isolation for untrusted third-party code
  -> business-level validation of accepted formulas
```

The dialect controls the language surface. The host controls the execution environment.

## Interop risk

`CSharpInterop` belongs to trusted profiles. Do not include it in end-user formula dialects unless the host intentionally exposes a trusted interop surface.

A restricted pricing DSL should normally reject expressions that call arbitrary .NET methods.

## Loop and resource risk

Loops may be valid for workflow-style DSLs, but they change the resource profile. A formula DSL often does not need loops. If loops are selected, the host should consider timeouts or step limits outside the documented language surface.

## Testing restricted profiles

A restricted dialect should include negative tests for syntax that must remain unavailable:

- C# interop;
- loops when not intended;
- labels when not intended;
- broad function calls when not intended;
- backend modes not exposed by the dialect;
- optimizer or intrinsic paths not supported by the selected backend.

Negative tests are not optional. They prove the restriction is real.

## Documentation wording

Good wording:

```text
This dialect restricts the available Wist modules and omits C# interop.
```

Bad wording:

```text
This dialect safely sandboxes arbitrary untrusted code.
```

Use the first wording unless the host also provides and documents process-level isolation and resource controls.

## Next

Continue with [Testing a DSL](/build-dsls/testing-dsl) and [Backend Contracts](/reference/backend-contracts).
