---
status: archive
last_verified: 2026-07-04
current_truth: ../../CURRENT_ARCHITECTURE_STATUS.md
---

# UniversalToolchain — основные мысли для дальнейшего roadmap

This document is archived historical thinking. It is not current runtime truth
or a release gate unless promoted by current-state documentation.

## 1. Базовая идея

UniversalToolchain должен развиваться не как «еще один язык» и не как абстрактная «платформа для всего», а как **конструктор языков**.

Ключевая формулировка:

> UniversalToolchain — это .NET-oriented language constructor platform: система, которая позволяет собирать, ограничивать, валидировать и исполнять предметные языки из декларативно-компонуемых semantic features поверх общего compilation/execution kernel.

Главная цель проекта:
- сохранить идею **«язык как конструктор»**;
- сделать систему **по-настоящему полезной для реальных пользователей**;
- не скатиться в набор низкоуровневых compiler hooks без понятного продуктового слоя.

---

## 2. Кто потенциально будет использовать проект

Нельзя мыслить «пользователя проекта» как одного человека. Есть минимум 4 разных класса пользователей.

### 2.1. Интегратор в приложении
Это .NET-разработчик, которому нужно встроить в продукт:
- формулы,
- правила,
- pricing logic,
- eligibility,
- routing,
- policy logic,
- сценарии.

Что ему нужно:
- простой API;
- typed bindings;
- компиляция и выполнение;
- хорошие ошибки;
- кэширование compiled artifacts;
- безопасные ограничения.

Что ему **не** нужно как primary UX:
- lexer,
- AST,
- IR,
- внутренние compiler contracts.

### 2.2. Конструктор предметного языка
Это platform engineer / tech lead / внутренний автор DSL.

Что ему нужно:
- собирать язык из feature-паков;
- включать/выключать возможности;
- ограничивать язык;
- управлять синтаксисом;
- управлять доступом к interop;
- понимать зависимости между фичами;
- получать детерминированную сборку языка.

Это **главный пользователь идеи «язык как конструктор»**.

### 2.3. Автор расширений языка
Это человек, который разрабатывает:
- новые operators,
- новые syntax packs,
- новые semantic features,
- lowering passes,
- optimizer packs,
- backend extensions,
- host integrations.

Что ему нужно:
- чистые контракты;
- явные зависимости и capabilities;
- предсказуемый ordering;
- тестируемость;
- backend parity.

### 2.4. Автор правил / скриптов
Это доменный пользователь:
- аналитик,
- инженер настройки,
- внутренний разработчик продукта,
- иногда business user.

Что ему нужно:
- понятный DSL;
- хорошие diagnostics;
- примеры;
- справка;
- валидатор;
- стабильное поведение.

---

## 3. Главный продуктовый вывод

UniversalToolchain не должен иметь один уровень взаимодействия.

У него должно быть **4 уровня доступа к одной и той же системе**:

1. **Product API** — для интегратора.
2. **Language constructor API / DSL** — для конструктора языка.
3. **Extension API** — для автора расширений.
4. **End-user DSL UX** — для автора правил.

---

## 4. Во что проект должен целиться в первую очередь

Самое сильное и реалистичное направление:

## **embedded DSL / rules / formula / policy platform for .NET**

То есть:
- не general-purpose language platform;
- не «маленький функциональный язык ради функциональности»;
- не бесконечная архитектура без продуктовой сборки;

а платформа, которая позволяет быстро собирать:
- rules DSL,
- formula DSL,
- pricing DSL,
- policy DSL,
- restricted expression DSL,
- workflow conditions DSL.

Это соответствует сильной стороне текущей архитектуры:
- staged pipeline;
- frontend/middle-end/IR hooks;
- compiler/interpreter;
- backend composition;
- параметризация;
- ограничения по intrinsics;
- диалекты и композиция.

---

## 5. Что такое «язык как конструктор» в правильном смысле

Настоящий конструктор языка — это не просто:
- много модулей,
- много интерфейсов,
- много мест, куда можно вклиниться.

Настоящий конструктор языка — это когда язык собирается **из осмысленных semantic features**, а pipeline является механизмом реализации.

То есть пользователь должен собирать не:
- lexer + parser hook + translator hack + optimizer;

а:
- arithmetic,
- boolean logic,
- local bindings,
- conditional branching,
- host parameters,
- restricted interop,
- money type,
- rule declarations,
- diagnostics policy,
- backend profile.

---

## 6. Целевая архитектура

У проекта должна быть трехслойная целевая архитектура.

## 6.1. Kernel
Низкоуровневое стабильное ядро:
- lexer;
- parser;
- AST;
- bytecode;
- IR;
- optimizers;
- compiler backends;
- interpreter backend;
- execution session;
- prepared execution;
- low-level runtime composition.

Это должно оставаться:
- стабильным;
- детерминированным;
- тестируемым;
- относительно low-level.

## 6.2. Constructor Model
Это главный недостающий слой.

Именно он должен стать **основным публичным интеллектом проекта**.

Здесь должны жить сущности вроде:
- `LanguageFeature`
- `LanguageProfile`
- `LanguagePlan`
- `FeatureDependency`
- `FeatureCapability`
- `SyntaxContribution`
- `SemanticContribution`
- `TypeContribution`
- `LoweringContribution`
- `IntrinsicContribution`
- `SecurityContribution`
- `BackendProfile`
- `BindingsSchema`
- `RuntimePolicy`
- `LanguageDiagnostic`

Именно этот слой должен отвечать на вопрос:
> «Что такое язык в рамках UniversalToolchain?»

## 6.3. Product Profiles
Готовые сборки поверх конструктора:
- Rules Core
- Formula Core
- Pricing DSL
- Policy DSL
- Restricted Expression DSL
- Educational Mini Language
- Demo Language Profiles

Это нужно, чтобы у проекта были реальные точки входа для пользователей.

---

## 7. Что должно быть главным публичным способом описания языка

Главным способом описания языка должен стать не raw pipeline API, а **декларативная композиция language features**.

То есть вместо модели:
- «я подключил модули и надеюсь, что они соберутся»

должна появиться модель:
- «я описал язык как набор фич, зависимостей, ограничений и профилей исполнения»

Это можно делать:
- через C# builder API;
- через declarative language DSL;
- через manifests/metadata;
- через language package descriptors.

Важный принцип:
- pipeline — внутренний механизм исполнения языка;
- language plan — внешняя модель языка.

---

## 8. Что нужно улучшить в расширяемости

### 8.1. Явная dependency model
Каждая feature должна уметь объявлять:
- что она требует;
- что она предоставляет;
- что запрещает;
- на какие backend capabilities опирается.

Например:
- `Conditions` требует boolean semantics;
- `Loops` требует labels/scopes;
- `Comparison` требует comparable values;
- `Interop` требует отдельную security capability.

### 8.2. Явная capability model
Нужно разделять:
- language semantics;
- backend capabilities;
- runtime safety capabilities;
- host integration capabilities.

### 8.3. Разделение syntax и semantics
Одна и та же semantic feature должна поддерживать несколько syntax surfaces.

Пример:
- semantic feature: conditional branching
- syntax surface A: `if ... then ... else`
- syntax surface B: `when ... ->`
- syntax surface C: другой sugar

Это даст настоящую гибкость.

### 8.4. Разделение semantics и host interop
Нужно четко различать:
- внутреннюю семантику языка;
- внешние bindings;
- host method calls;
- CLR interop;
- sandbox/security boundary.

### 8.5. Уровни расширения
Чтобы расширение не требовало знания всего runtime, нужно ввести уровни:

- syntax-only extension
- syntax + lowering extension
- semantic feature extension
- type system extension
- backend extension
- tooling/diagnostics extension

---

## 9. Что должно стать архитектурными инвариантами

Это критически важно для доверия к системе.

## 9.1. Детерминизм
Язык должен собираться детерминированно:
- порядок модулей;
- порядок passes;
- итоговый language plan;
- diagnostics;
- разрешение конфликтов.

## 9.2. Явная проверка конфликтов
Система должна уметь заранее диагностировать:
- противоречащие directives;
- циклы зависимостей;
- несовместимые features;
- отсутствующие capabilities;
- недоступные backend paths.

## 9.3. Semantic parity между backend’ами
Если feature заявлена как поддерживаемая на нескольких backend’ах, ее семантика должна быть согласованной.

Нужно считать обязательными:
- parity tests;
- golden tests;
- explicit unsupported diagnostics;
- capability-aware fallback.

## 9.4. Наблюдаемость
Нужны first-class механизмы introspection:
- AST dump;
- bytecode dump;
- IR dump;
- plan dump;
- diagnostics trace;
- feature resolution trace.

## 9.5. Безопасность как часть модели
Ограничения должны быть встроены в архитектуру, а не навешиваться потом.

Нужны:
- security policies;
- safe language profiles;
- capability whitelists;
- restricted host surface;
- controlled interop.

---

## 10. Как должен выглядеть продукт для каждого класса пользователей

## 10.1. Для интегратора
Нужен очень простой API.

Пример направления:
- `RulesEngine`
- `PolicyEngine`
- `FormulaEngine`

Типовой сценарий:
1. создать engine из готового profile;
2. скомпилировать текст;
3. передать typed bindings;
4. выполнить;
5. получить diagnostics;
6. кешировать artifact.

## 10.2. Для конструктора языка
Нужен builder или declarative DSL, где язык описывается через:
- feature-packs;
- bindings schema;
- runtime policy;
- syntax choices;
- enabled backends;
- safety rules;
- diagnostics policy.

## 10.3. Для автора расширений
Нужны:
- manifest-like contracts;
- feature metadata;
- capability declaration;
- dependency declaration;
- backend compatibility declaration;
- test expectations.

## 10.4. Для автора правил
Нужны:
- готовый DSL;
- примеры;
- справка;
- friendly diagnostics;
- validator;
- возможно редактор/подсказки.

---

## 11. Что нельзя делать, если хочется сохранить фокус

Нельзя:
- пытаться сейчас стать полноценной general-purpose language platform;
- уводить проект в сторону «маленького FP-языка ради FP-языка»;
- делать главным API низкоуровневые compiler hooks;
- продавать проект как «у нас можно сделать любой язык» без конкретного кейса;
- оставлять язык описанным только списками строковых модулей;
- смешивать internal kernel API и product API.

---

## 12. В каком направлении проект будет по-настоящему полезным

UniversalToolchain станет реально полезным, если позволит дешевле, безопаснее и быстрее решать задачу:

> встроить свой ограниченный предметный язык в .NET-приложение

Полезность должна измеряться не количеством модулей и не “красотой архитектуры”, а тем, насколько система уменьшает:
- цену внедрения DSL;
- цену изменения логики;
- цену валидации;
- цену диагностики;
- цену безопасности;
- цену поддержки нескольких backend’ов;
- цену разработки нового языкового профиля.

---

## 13. Самый важный стратегический вывод

Главный переход, который должен произойти:

## **не “пайплайн из интерфейсов” → а “язык из фич”**

Это и есть центральная идея, вокруг которой потом можно строить roadmap.

---

## 14. Опорная формула для roadmap

При построении roadmap каждая задача должна проверяться вопросом:

### Эта задача усиливает:
- kernel stability?
- constructor model?
- feature composition?
- diagnostics/observability?
- safety/restrictions?
- product profiles?
- ease of embedding into .NET?

Если нет — скорее всего, это не приоритет.

---

## 15. Краткая целевая формулировка

### Что такое UniversalToolchain в идеале
UniversalToolchain — это платформа, в которой:
- ядро компиляции и исполнения стабильно;
- язык собирается как декларативный граф feature-паков;
- ограничения и capabilities first-class;
- backend’ы подключаются как профили исполнения;
- поверх конструктора существуют готовые прикладные language profiles для .NET.

### Что должно быть главным ощущением от системы
Пользователь должен чувствовать не:
- «я лезу в компилятор»

а:
- **«я собираю язык»**
