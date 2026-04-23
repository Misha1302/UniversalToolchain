# UniversalToolchain — roadmap

## 0. Цель roadmap

Этот roadmap нужен не для «добавить побольше фич», а для перевода UniversalToolchain в состояние, где он:

- остается **конструктором языков**;
- становится **понятным и полезным для реального использования**;
- не теряет архитектурную чистоту;
- не расползается в «платформу для всего».

Главный стратегический переход:

## **не “pipeline из низкоуровневых интерфейсов”, а “язык как декларативно собираемая система фич”**

---

# 1. Главный целевой результат

К концу roadmap UniversalToolchain должен уметь следующее:

1. Позволять **собирать язык из semantic features**, а не только из raw modules.
2. Давать **готовый product-level API** для .NET-встраивания.
3. Поддерживать **детерминированную сборку language plan**.
4. Давать **ограниченные и безопасные language profiles**.
5. Иметь **как минимум 1-2 сильных прикладных профиля**, которые реально показывают пользу.
6. Сохранить и усилить идею **«язык как конструктор»**.

---

# 2. Главные продуктовые направления

Не нужно распыляться. Главный фокус:

## Основное направление
**Embedded DSL platform for .NET**
- rules DSL
- formula DSL
- pricing DSL
- policy DSL
- restricted expression DSL

## Вторичное направление
**Language constructor for platform engineers**
- сборка новых языков из feature graph
- кастомный синтаксис
- ограничения и capabilities
- backend-aware composition

## Не приоритет сейчас
- полноценный general-purpose language
- полноценный FP runtime
- полноценный новый типовой мир с closures / higher-order semantics
- широкий зоопарк backend’ов без продуктового кейса

---

# 3. Принципы приоритизации

Любая задача должна усиливать хотя бы один из этих столпов:

1. **Kernel stability**
2. **Language constructor model**
3. **Deterministic composition**
4. **Diagnostics and observability**
5. **Safety and restrictions**
6. **Product-level embedding**
7. **Reusable feature architecture**

Если задача не усиливает ни один из этих пунктов, она не приоритет.

---

# 4. Этапы roadmap

---

# Phase 0 — Зафиксировать направление проекта

## Цель
Прекратить архитектурную размытость и зафиксировать, что именно строится.

## Что нужно сделать
1. Зафиксировать основной positioning проекта:
   - UniversalToolchain — не “еще один язык”
   - UniversalToolchain — не “компилятор ради компилятора”
   - UniversalToolchain — **language constructor platform for .NET embedded DSLs**

2. Зафиксировать основные user personas:
   - integrator
   - language constructor
   - extension author
   - end-user DSL author

3. Зафиксировать уровни API:
   - product API
   - constructor API
   - extension API
   - end-user DSL layer

4. Зафиксировать, что **Wist — это reference language / proof language**, а не единственный смысл проекта.

## Deliverables
- отдельный vision / positioning document
- user personas document
- definition of success for project
- список явно неприоритетных направлений

## Definition of Done
- есть письменный документ с позицией проекта;
- все будущие задачи можно соотнести с этой позицией;
- прекращены архитектурные решения “на всякий случай”.

---

# Phase 1 — Укрепить и стабилизировать kernel

## Цель
Сделать ядро максимально надежным, детерминированным и предсказуемым.

## Почему это важно
Пока ядро не является железобетонным, нельзя строить уверенный language constructor.

## Что нужно сделать

### 1. Детерминизм
- зафиксировать детерминированный ordering всех composition stages;
- зафиксировать порядок feature/module resolution;
- зафиксировать deterministic diagnostics output;
- устранить все места, где composition зависит от случайного порядка discovery/registration.

### 2. Parity между backend’ами
- сделать обязательную стратегию semantic parity tests;
- ввести golden tests для reference scenarios;
- явно описать, какие features поддерживаются какими backend’ами;
- добавлять explicit diagnostics на unsupported semantics.

### 3. Изоляция состояния
- проверить и зачистить global/shared mutable state;
- убедиться, что repeated builds и parallel runs не текут друг в друга;
- усилить тесты на session/state isolation.

### 4. Pipeline introspection
- formalize dumps:
  - source
  - lexemes
  - AST
  - bytecode
  - IR
  - resolved language plan
- сделать стабильный формат debug/introspection outputs.

## Deliverables
- deterministic composition tests
- backend parity test suite
- state isolation test suite
- normalized diagnostics strategy
- unified introspection output model

## Definition of Done
- одинаковый input + одинаковый profile всегда дают одинаковый plan и одинаковые diagnostics;
- reference scenarios проходят на interpreter/compiler одинаково или диагностируются явно;
- внутреннее состояние не течет между независимыми сборками и сессиями.

---

# Phase 2 — Ввести first-class модель языка как конструктора

## Цель
Сделать так, чтобы язык описывался не низкоуровневыми модулями, а осмысленными feature-сущностями.

## Почему это ключевая фаза
Именно здесь проект реально превращается из pipeline framework в language constructor platform.

## Что нужно сделать

### 1. Ввести новые центральные сущности
Нужно спроектировать и ввести слой вроде:

- `LanguageFeature`
- `LanguageProfile`
- `LanguagePlan`
- `FeatureDependency`
- `FeatureCapability`
- `BindingsSchema`
- `RuntimePolicy`
- `BackendProfile`
- `LanguageDiagnostic`
- `FeatureManifest`

### 2. Разделить уровни описания
Нужно явно разделить:
- syntax contribution
- semantic contribution
- lowering contribution
- type contribution
- optimization contribution
- runtime/security contribution

### 3. Ввести dependency model
Каждая feature должна описывать:
- requires
- provides
- conflicts
- optional integrations
- backend capability requirements

### 4. Ввести capability model
Отдельно описывать:
- semantic capabilities
- backend capabilities
- safety capabilities
- host integration capabilities

### 5. Ввести итоговый immutable LanguagePlan
После разрешения языка система должна выдавать first-class artifact:

`LanguagePlan`

Он должен содержать:
- итоговый feature graph
- итоговый ordering
- итоговый набор syntax/semantic contributions
- итоговый набор enabled/disabled capabilities
- bindings schema
- runtime policy
- backend availability
- diagnostics

## Deliverables
- RFC / design document по language constructor model
- новая доменная модель language features
- resolver language features -> language plan
- тесты на dependency resolution and conflicts
- сериализуемый/отображаемый language plan

## Definition of Done
- язык можно описать без прямой работы с raw pipeline hooks;
- feature composition объяснима и тестируема;
- система умеет диагностировать missing requirements / conflicts / unsupported backend paths.

---

# Phase 3 — Перестроить composition API вокруг языка, а не вокруг модулей

## Цель
Сделать основной public API ориентированным на language construction, а не на внутреннюю механику pipeline.

## Что нужно сделать

### 1. Product-facing constructor API
Должен появиться API уровня:

- `LanguageBuilder`
- `LanguageProfileBuilder`
- `FeatureSetBuilder`
- `RuntimePolicyBuilder`

### 2. Declarative language definition
Нужно сделать так, чтобы язык можно было задавать:
- через builder API;
- через declarative DSL;
- через manifests / metadata.

### 3. Переосмыслить dialect DSL
Текущий dialect DSL нужно развить из:
- `use`
- `exclude`
- `backend`
- `enable`

в систему, которая может описывать:
- features
- dependencies
- syntax packs
- runtime restrictions
- binding schemas
- backend profiles
- docs/examples metadata
- versioning/deprecation rules

### 4. Развести internal и public API
Нужно жестко отделить:
- internal kernel contracts
- extension contracts
- public language construction API
- product embedding API

## Deliverables
- новый public constructor API
- новый/расширенный language-definition DSL
- clear boundary docs between internal and public layers
- migration guide from raw modules to feature model

## Definition of Done
- новый язык можно собрать на уровне feature composition;
- для большинства сценариев не нужно знать внутренние pipeline contracts;
- public API выглядит как language constructor, а не как compiler internals.

---

# Phase 4 — Сделать безопасность и ограничения first-class частью модели

## Цель
Сделать restricted / safe languages одной из сильнейших особенностей платформы.

## Почему это важно
Именно restricted DSLs для embedded usage — один из самых реальных и ценных use case.

## Что нужно сделать

### 1. Security policy model
Нужно ввести first-class сущности:
- allowed features
- forbidden features
- allowed host surface
- forbidden interop
- allowed intrinsics
- backend-specific restrictions

### 2. Host interop boundary
Нужно четко разделить:
- pure language semantics
- external parameters
- host calls
- CLR interop
- unsafe extensions

### 3. Restricted profiles
Support safe profiles that restrict language-facing capabilities:
- no interop
- no user-visible reflection-like features
- no uncontrolled host access
- limited types only
- limited operators only

These restrictions target DSL/user capabilities. They do not imply that internal runtime infrastructure (for example, targeted exact activation of selected runtime components) must be removed.

### 4. Security-aware diagnostics
Ошибки ограничений должны быть:
- понятными;
- заранее диагностируемыми;
- детерминированными;
- объясняющими, что именно запрещено и почему.

## Deliverables
- runtime security policy model
- restricted language profiles
- safe host integration contracts
- security diagnostics strategy

## Definition of Done
- можно явно собрать “safe language profile”;
- нельзя случайно протащить запрещенную возможность;
- ограничения проверяются системно, а не ad-hoc.

---

# Phase 5 — Сделать сильный product-level embedding API

## Цель
Сделать UniversalToolchain удобным для .NET-интеграторов.

## Что нужно сделать

### 1. Ввести фасады верхнего уровня
Нужны понятные точки входа типа:
- `RulesEngine`
- `FormulaEngine`
- `PolicyEngine`
- `LanguageRuntime`

### 2. Typed bindings schema
Нужны first-class binding contracts:
- declared inputs
- runtime inputs
- validation
- coercion strategy
- host value shape

### 3. Artifact lifecycle
Нужна ясная модель:
- validate
- compile
- cache
- create session
- execute
- inspect diagnostics

### 4. Operational UX
Нужно сделать понятным:
- что делать при ошибке;
- как валидировать перед публикацией;
- как запускать безопасно;
- как логировать;
- как отлаживать.

## Deliverables
- minimal public embedding API
- documentation for real embedding scenarios
- compile/validate/run lifecycle model
- example integrations in ordinary .NET service

## Definition of Done
- обычный .NET-разработчик может встроить DSL без знания internal compiler layers;
- путь “описал bindings -> скомпилировал -> запустил -> получил diagnostics” естественный и короткий.

---

# Phase 6 — Построить 1-2 эталонных language profiles

## Цель
Показать реальную полезность платформы через сильные reference products.

## Почему это критично
Без готовых сильных профилей проект будет выглядеть как “набор инфраструктуры”.

## Какие профили выбрать

### Профиль 1 — Rules / Policy DSL
Лучший кандидат на первый основной продуктовый профиль.

Должен уметь:
- параметры
- арифметику
- булеву логику
- сравнения
- local bindings
- условные ветви
- ограниченный interop или его отсутствие
- хорошие diagnostics
- compiler/interpreter parity

### Профиль 2 — Pricing / Formula DSL
Очень понятный и прикладной кейс.

Должен уметь:
- expressions
- local values
- conditions
- numeric semantics
- limited domain types
- predictable execution

### Возможный профиль 3 — Restricted Educational Language
Не как главный продукт, а как showcase constructor power.

## Deliverables
- 1 production-style Rules DSL profile
- 1 production-style Pricing DSL profile
- examples, docs, diagnostics, tests, embedding examples

## Definition of Done
- можно показать продукт человеку вне проекта;
- профиль выглядит как самостоятельный полезный DSL, а не просто как “пример”.

---

# Phase 7 — Развить diagnostics, observability и tooling UX

## Цель
Сделать разработку языков и работу с DSL комфортной.

## Что нужно сделать

### 1. Diagnostics model
Нужны:
- stable error codes
- warnings
- spans / locations
- stage information
- category/severity model
- actionable messages

### 2. Plan / pipeline explainability
Нужно уметь объяснить:
- какие features вошли в язык;
- почему они вошли;
- какие зависимости подтянулись;
- какие ограничения применились;
- почему что-то недоступно.

### 3. Tooling surface
Минимум:
- validator API
- explain plan API
- dump APIs
- maybe simple CLI utilities

### 4. Examples and cookbook
Нужны:
- “как собрать новый язык”
- “как добавить syntax pack”
- “как ограничить язык”
- “как встраивать в .NET service”
- “как диагностировать проблемы”

## Deliverables
- formal diagnostics model
- explain-plan model
- introspection tooling
- cookbook / example catalog

## Definition of Done
- язык можно не только собрать, но и понять;
- поведение системы объяснимо;
- developer UX не требует чтения всего ядра.

---

# Phase 8 — Упаковка и экосистема

## Цель
Сделать проект пригодным для долгосрочного роста.

## Что нужно сделать

### 1. Пакетирование
Разделить поставку на уровни:
- core kernel packages
- constructor model packages
- embedding packages
- reference language/profile packages
- extension sdk packages

### 2. Versioning strategy
Нужно определить:
- что считается stable API;
- как versioning работает для features;
- как versioning работает для language profiles;
- как отслеживаются breaking changes.

### 3. Extension SDK
Нужен понятный путь для внешнего автора расширения:
- contracts
- manifests
- tests
- version compatibility

### 4. Long-term maintainability
Нужно заранее предусмотреть:
- deprecation paths
- compatibility rules
- test matrix
- package boundaries

## Deliverables
- package map
- versioning policy
- extension authoring guide
- compatibility guarantees

## Definition of Done
- проект можно поддерживать как платформу, а не как растущий монолит;
- external extensions становятся реалистичными.

---

# 5. Порядок приоритетов

Если делать по жесткому приоритету, то порядок такой:

## Tier 1 — Без этого нельзя
1. Phase 0 — зафиксировать продуктовую роль
2. Phase 1 — стабилизировать kernel
3. Phase 2 — ввести language constructor model

## Tier 2 — Это делает проект реально usable
4. Phase 3 — новый composition API
5. Phase 4 — safety / restrictions as first-class
6. Phase 5 — embedding API

## Tier 3 — Это превращает проект в сильный продукт
7. Phase 6 — product profiles
8. Phase 7 — diagnostics/tooling UX
9. Phase 8 — packaging/ecosystem

---

# 6. Что делать не надо слишком рано

Нельзя раньше времени:
- строить полноценный FP-layer;
- тратить много времени на третий/четвертый backend без product pressure;
- расширять синтаксис ради синтаксиса;
- пытаться одновременно делать language research platform и массовый product API;
- размазывать effort на десятки reference languages;
- путать extension mechanism с end-user product UX.

---

# 7. Метрики успеха

Roadmap должен измеряться не количеством кода, а системными эффектами.

## Архитектурные метрики
- язык собирается детерминированно;
- feature dependencies разрешаются формально;
- backend support выражен явно;
- ограничения first-class;
- internal/public boundaries ясны.

## Product метрики
- новый restricted DSL можно собрать быстрее, чем писать его с нуля;
- embedding сценарий понятен обычному .NET-разработчику;
- есть минимум 1-2 реально убедительных profile demos;
- diagnostics понятны без чтения внутренностей ядра.

## DX метрики
- автор расширения не обязан знать весь runtime;
- автор языка работает с features/plans/policies, а не с сырой механикой;
- автор DSL-программы получает полезные ошибки и валидатор.

---

# 8. Самый краткий вариант roadmap в одной строке

## **Stabilize kernel -> formalize language features -> build language plans -> make safety first-class -> expose product APIs -> ship strong profiles**

---

# 9. Самая важная мысль

Если упростить до одной фразы:

## UniversalToolchain должен эволюционировать из «расширяемого компиляторного пайплайна» в «платформу, где язык является first-class сборкой semantic features».

Именно вокруг этого и должен строиться весь дальнейший roadmap.
