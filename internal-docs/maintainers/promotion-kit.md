---
title: Maintainer Promotion Kit
description: Canonical launch messages, claim boundaries, distribution sequence and feedback metrics for Wist.
---

# Maintainer Promotion Kit

This page is for maintainers preparing a public Wist announcement. It keeps the product message aligned with the current alpha and avoids claims the repository does not prove.

## Canonical message

> Wist is a .NET package for validating restricted numeric rules and compiling approved formulas into typed delegates. Your application owns the inputs and side effects.

Use `Wist` and `UniversalToolchain.Wist` as the first-contact product names. Introduce `UniversalToolchain` as the underlying modular DSL/runtime framework only after the practical formula example is clear.

## Demonstration order

1. Show a useful formula with named host inputs.
2. Validate it before execution.
3. Compile it into a typed delegate.
4. Show a broader statement-style rule being rejected.
5. Explain that the host owns authorization, persistence, resource isolation, and side effects.
6. Link to the public NuGet quickstart or clean-room smoke command.
7. Only then discuss dialect composition, interpreter/CIL parity, AIR, or experimental SSA.

## Claims that are supported

- A public `UniversalToolchain.Wist` facade exists for .NET applications.
- `CreateRestrictedArithmetic()` provides the recommended first-contact formula surface.
- `Validate`, `Evaluate<T>`, and typed `Compile<TDelegate>` paths exist.
- The package has interpreter and CIL-oriented execution paths within the documented supported surface.
- SSA is optional, observable, and experimental.
- Source length and parameter count have host-owned preflight limits.

## Claims to avoid

- Do not call the restricted preset a hardened sandbox.
- Do not say Wist runs arbitrary untrusted code safely.
- Do not claim that every Wist program performs like handwritten C#.
- Do not present the experimental SSA route as an SSA-native backend or a general speed guarantee.
- Do not describe the current alpha as a finished business-rule platform or universal language workbench.
- Do not imply production adoption or external users unless there is direct evidence.

## Reddit or .NET community post

**Title**

> I built a .NET package that validates restricted formulas and compiles them into typed delegates

**Body**

> I am building Wist, a small public facade over a modular .NET DSL/runtime framework. The practical use case is configurable numeric logic: the host application defines inputs, validates rule text, compiles an approved formula once, and invokes a typed delegate on the repeated path.
>
> The restricted preset intentionally rejects broader statement-style shapes. I am not presenting it as a hardened sandbox: authorization, persistence, resource isolation, and side effects remain in the host application.
>
> The package is `UniversalToolchain.Wist` on NuGet. I would especially value concrete feedback on installation, diagnostics, and real formula use cases. Could you reach the first result without opening the architecture documentation?

Include the 1280×640 visual from `docs/assets/wist-social-preview.svg` and put the repository link after the practical explanation, not before it.

## Show HN post

**Title**

> Show HN: Wist – restricted formulas compiled to typed .NET delegates

**Body**

> Wist lets a .NET host validate small numeric rules, compile an approved formula into a typed delegate, and keep side effects in ordinary application code. It is backed by a modular DSL/runtime framework with interpreter and CIL paths.
>
> The current alpha is intentionally narrow: it is not a hardened sandbox or a finished general-purpose language workbench. The repository includes a public-package smoke check and runnable rollout, pricing, and LMS-style formula examples.
>
> I am looking for feedback from people who have embedded configurable formulas or small DSLs: where does the API become confusing, and which capability is missing for a real internal tool?

Publish Show HN only after an independent user has completed the package quickstart without maintainer help.

## LinkedIn post

> I published the first alpha of `UniversalToolchain.Wist`, a .NET facade for restricted numeric rules.
>
> A host application defines the inputs, validates the rule, compiles an approved formula once, and receives a typed delegate. The formula returns data; the application still owns authorization, persistence, resource isolation, and side effects.
>
> The project also explores modular dialect composition, interpreter/CIL semantic parity, and an observable experimental SSA route. The current release is deliberately an alpha, not a sandbox claim.
>
> I am looking for .NET developers who can try the quickstart and tell me the exact point where the first ten minutes become confusing.

## Russian Telegram post

> Выпустил первую alpha-версию `UniversalToolchain.Wist` — пакета для небольших управляемых формул внутри .NET-приложений.
>
> Приложение само задаёт входные данные, проверяет текст правила и компилирует одобренную формулу в типизированный delegate. Формула возвращает число, а авторизация, сохранение данных, ограничения ресурсов и реальные действия остаются в обычном C#-коде.
>
> Это не обещание безопасного запуска произвольного кода и не готовая универсальная rule-engine платформа. Сейчас мне особенно нужна конкретная обратная связь по установке, диагностике и реальным сценариям формул: получилось ли получить первый результат без чтения документации по внутренней архитектуре?

## Direct tester request

Send this to a small number of relevant .NET developers before the public launch:

> I am testing the first-contact experience of a small .NET formula package. Please open the repository or NuGet page and try to get one numeric result without asking me for instructions. Record the exact command, API call, or sentence where you hesitate. I need friction data more than general approval.

Do not ask testers to star the repository. Ask them to report observed behavior and a real formula they would try.

## Feedback questions

1. Could you install and run the package without opening the architecture docs?
2. How long did it take to obtain the first numeric result?
3. At which exact command or API call did you hesitate?
4. What formula or rule would you try in a real application?
5. Which diagnostic or trust boundary was unclear?
6. What would stop you from trying Wist in a small internal tool?
7. Would you choose one-off `Evaluate` or compile-once invocation correctly from the docs?

## Launch sequence

1. Validate the source demo and public-package smoke.
2. Run three to five independent first-ten-minutes tests.
3. Fix onboarding and diagnostics friction.
4. Publish LinkedIn and focused Telegram/community posts.
5. Publish the .NET community/Reddit post after incorporating early feedback.
6. Publish Show HN only after the quickstart is independently reproducible.
7. Submit to curated .NET lists after the entry path and maintenance expectations are stable.

## First launch metrics

Track:

- successful clean-room package runs;
- median time from opening the repository to the first numeric result;
- independent issues or discussions from external users;
- real formulas attempted, not only stars or impressions;
- one external example, integration, or pull request;
- repeated onboarding failures by step;
- stars as a secondary signal rather than the primary success criterion.

Rework onboarding before increasing promotion if testers cannot reach the first result in ten minutes.
