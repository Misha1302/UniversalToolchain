---
title: Maintainer Promotion Kit
description: Canonical launch messages, claim boundaries and feedback metrics for Wist.
---

# Maintainer Promotion Kit

This page is for maintainers preparing a public Wist announcement. It keeps the product message aligned with the current alpha and avoids claims the repository does not prove.

## Canonical message

> Wist is a .NET package for validating restricted numeric rules and compiling approved formulas into typed delegates. Your application owns the inputs and side effects.

Use `Wist` and `UniversalToolchain.Wist` as the first-contact product names. Introduce `UniversalToolchain` as the underlying modular DSL/runtime framework only after the practical formula example is clear.

## Demonstration order

1. Show a useful formula with named host inputs.
2. Compile it into a typed delegate.
3. Show a broader statement-style rule being rejected.
4. Explain that the host owns side effects and authorization.
5. Link to the clean-room NuGet smoke command.
6. Only then discuss dialect composition, interpreter/CIL parity, AIR or experimental SSA.

## Claims that are supported

- A public `UniversalToolchain.Wist` facade exists for .NET applications.
- `CreateRestrictedArithmetic()` provides the recommended first-contact formula surface.
- `Validate`, `Evaluate<T>` and typed `Compile<TDelegate>` paths exist.
- The package has interpreter and CIL-oriented execution paths within the documented supported surface.
- SSA is optional, observable and experimental.

## Claims to avoid

- Do not call the restricted preset a hardened sandbox.
- Do not say Wist runs arbitrary untrusted code safely.
- Do not claim that every Wist program performs like handwritten C#.
- Do not present the experimental SSA route as an SSA-native backend or a general speed guarantee.
- Do not describe the current alpha as a finished business-rule platform or universal language workbench.

## Reddit or .NET community post

**Title**

> I built a .NET package that compiles restricted formulas into typed delegates

**Body**

> I am building Wist, a small first-contact facade over a modular .NET DSL/runtime framework. The practical use case is configurable numeric rules: the host application defines inputs, validates rule text, compiles an approved formula once and invokes a typed delegate on the hot path.
>
> The restricted preset intentionally rejects broader statement-style shapes, and I am not presenting it as a hardened sandbox. Side effects, authorization and persistence remain in the host application.
>
> The package is `UniversalToolchain.Wist` on NuGet. I would especially value feedback on the installation path, diagnostics and which real formula use cases are missing.

## Show HN post

**Title**

> Show HN: Wist – restricted formulas compiled to typed .NET delegates

**Body**

> Wist lets a .NET host validate small numeric rules, compile an approved formula into a typed delegate and keep side effects in ordinary application code. It is backed by a larger modular DSL/runtime framework with interpreter and CIL paths.
>
> The current alpha is intentionally narrow: it is not a hardened sandbox or a finished general-purpose language workbench. The repository includes a clean-room NuGet smoke check and copy-ready pricing, rollout and LMS examples.

## LinkedIn post

> I published the first alpha of `UniversalToolchain.Wist`, a .NET facade for restricted numeric rules.
>
> The host application controls the inputs, validates the rule, compiles an approved formula once and receives a typed delegate. The formula returns data; the application still owns authorization, persistence and side effects.
>
> The project also explores modular dialect composition, interpreter/CIL semantic parity and an observable experimental SSA route. The current release is deliberately a alpha, not a sandbox claim.
>
> I am looking for .NET developers who can try the clean-room quickstart and report where the first ten minutes are confusing.

## Russian Telegram post

> Выпустил первую alpha-версию `UniversalToolchain.Wist` — пакета для небольших управляемых формул внутри .NET-приложений.
>
> Приложение само задаёт входные данные, проверяет текст правила и компилирует одобренную формулу в типизированный delegate. Формула возвращает число, а авторизация, сохранение данных и реальные действия остаются в обычном C#-коде.
>
> Это не обещание безопасного запуска произвольного кода и не готовая универсальная rule-engine платформа. Сейчас мне особенно нужна обратная связь по установке, диагностике и реальным сценариям формул.

## Feedback request

Ask testers for observed behavior rather than general approval:

1. Could you install and run the package without opening the architecture docs?
2. At which exact command or API call did you hesitate?
3. What formula or rule would you try in a real application?
4. Which diagnostic or trust boundary was unclear?
5. What would stop you from trying Wist in a small internal tool?

## First launch metrics

Track:

- successful clean-room package runs;
- time from opening the repository to the first numeric result;
- independent issues or discussions from external users;
- real formulas attempted, not only stars or impressions;
- one external example or pull request.

Rework onboarding before increasing promotion if testers cannot reach the first result in ten minutes.
