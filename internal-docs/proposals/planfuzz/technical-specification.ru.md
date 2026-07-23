# PlanFuzz

## Техническое задание на конфигурационно-ориентированное дифференциальное тестирование расширяемых языков

**Версия ТЗ:** 0.2  
**Дата:** 24 июля 2026 г.  
**Целевой репозиторий:** `Misha1302/Wist2`  
**GitHub baseline публикации:** `master@7f2b5819f712d03c39270349b6b39e914b79e008`  
**Инспектированный source bundle:** `Wist2-language-authoring-multi-user-documentation-2026-07-23.3(2).zip`  
**Целевая платформа:** .NET 10, C# 14, Linux x64; Windows — воспроизведение и CI  
**Статус:** proposal / implementation-ready specification; описанная система ещё не реализована

---

## 1. Назначение и позиционирование

PlanFuzz — исследовательский слой над UniversalToolchain, предназначенный для автоматического поиска дефектов, возникающих на пересечении:

- входной программы;
- выбранных возможностей языка;
- package/contribution composition;
- artifact routes;
- backend'ов;
- optimizer/IR routes;
- fallback policy;
- component lifetime;
- runtime lifecycle и execution schedule.

Обычный compiler fuzzer главным образом изменяет программу. PlanFuzz изменяет программу, исполняемый language plan и маршрут исполнения, после чего проверяет явно заданные дифференциальные и метаморфические отношения.

Каноническая модель testcase:

```text
TestCase = Program
         × LanguagePlanVariant
         × BackendOrOptimizationVariant
         × ExecutionSchedule
         × ApplicableOracleSet
```

PlanFuzz не предполагает, что все варианты должны завершаться успешно или возвращать одинаковое значение. Для каждой пары или группы вариантов заранее задаётся тип отношения:

- semantic equivalence;
- same normalized failure;
- deterministic composition;
- negative-surface preservation;
- extension noninterference;
- controlled fallback;
- state isolation;
- route conformance;
- expected fail-closed rejection.

Документ хранится в `internal-docs/proposals/planfuzz/` как следующая ступень развития, а не как описание текущей функциональности. До реализации authoritative sources — существующий код, тесты, runtime contracts и current-architecture документы.

---

## 2. Исследовательская гипотеза

> Конфигурационно-ориентированная генерация и проверка программ, language plans, execution routes и lifecycle schedules обнаруживает классы дефектов расширяемых компиляторов, которые не обнаруживаются program-only fuzzing и обычными handwritten tests при сопоставимом бюджете исполнения.

### 2.1. Исследовательские вопросы

**RQ1 — Defect discovery.** Находит ли PlanFuzz реальные ранее неизвестные дефекты взаимодействия composition, backend'ов, оптимизаций и lifecycle?

**RQ2 — Incremental value.** Какие дефекты становятся обнаружимыми только после добавления отдельных измерений:

1. program generation;
2. plan mutation;
3. backend/optimization variation;
4. lifecycle scheduling;
5. negative-surface oracle?

**RQ3 — Cost.** Как распределяется стоимость по generation, plan compilation, runtime creation, execution, oracle evaluation, confirmation и reduction?

**RQ4 — Reduction.** Насколько reducer уменьшает программу, plan delta и schedule, сохраняя один defect fingerprint?

**RQ5 — Generality.** Работает ли один generic core минимум для Wist и независимого `Acme.PricingLanguage`?

### 2.2. Условия научно сильного результата

Исследовательская гипотеза получает поддержку только если одновременно выполнено:

1. Найдены подтверждённые дефекты минимум двух root-cause classes.
2. Минимум один дефект требует изменения plan, route или lifecycle и не воспроизводится program-only baseline.
3. Каждый засчитанный дефект:
   - воспроизводится в fresh-process strict mode;
   - имеет минимизированный testcase;
   - имеет root-cause analysis;
   - закреплён regression test;
   - отделён от seeded faults.
4. Проведено равнобюджетное сравнение минимум с двумя baseline-режимами.
5. Сохранён воспроизводимый raw evidence package.

Seeded faults проверяют адекватность инструмента и не считаются найденными реальными дефектами.

---

## 3. Baseline и архитектурные инварианты

Текущий repository baseline предоставляет необходимые предпосылки:

- typed language packages и contributions;
- immutable `LanguagePlan` и canonical plan hash;
- configurable entry artifacts и artifact routes;
- exact executor selection;
- component lifetimes;
- Wist interpreter/CIL paths;
- verifier-gated Wist `AIR -> SSA -> AIR` route;
- независимый `samples/Acme.PricingLanguage`;
- reusable testing contracts.

Числа из `VERIFICATION.md` инспектированного bundle являются recorded evidence, а не повторным clean run при публикации этого proposal.

Реализация обязана сохранять границы:

1. Generic PlanFuzz core не знает Wist-specific IDs, syntax, modules или backend types.
2. Wist-specific поведение находится только в Wist adapter.
3. Program generation и reduction работают с adapter-owned structured model, а не распознают raw source регулярными выражениями.
4. Existing planning/runtime contracts остаются authoritative; PlanFuzz не создаёт второй execution model.
5. Frontend не получает зависимости от backend implementation.
6. Unsupported behavior не превращается в silent success.
7. Plans, descriptors, cases и observations являются immutable snapshots на границах.
8. Глобальные mutable registries запрещены.
9. Reflection допускается только как bounded deterministic mechanism над выбранными components.
10. Существующие проверки не ослабляются ради generated cases.
11. Research tooling не расширяет public Wist package surface без отдельного non-Wist use case и compatibility review.

---

## 4. Область работ

### 4.1. MVP

В MVP входят:

- deterministic campaign coordinator;
- versioned testcase format;
- isolated worker process;
- generic language adapter contract;
- Acme adapter;
- Wist restricted-arithmetic adapter;
- plan variations и package-order permutations;
- backend parity;
- plan determinism;
- optimization/route parity для применимого Wist subset;
- controlled fallback;
- negative-surface checks;
- runtime/session isolation scenarios;
- typed value and failure normalization;
- replay, deduplication и reduction;
- seeded fault fixtures;
- CLI и machine-readable reports;
- reproducible research artifacts.

### 4.2. Расширенная версия

После MVP допускаются:

- Wist locals, scopes, conditions, loops и SafeMath calls;
- concurrent schedules;
- third independent adapter;
- coverage-guided scheduling;
- adaptive mutator weighting;
- distributed workers;
- cross-SDK и cross-OS campaigns.

### 4.3. Не входит

- новый parser generator;
- grammar inference;
- формальное доказательство корректности UniversalToolchain;
- hostile-extension sandbox;
- fuzzing произвольного CLR IL;
- security claim о безопасном исполнении hostile code;
- замена существующих unit/integration tests;
- автоматическое объявление любого расхождения compiler bug;
- Wist performance claims;
- blind AppDomain/filesystem plugin discovery.

---

## 5. Структура solution

Предлагаемые проекты:

```text
UniversalToolchain/
  UniversalToolchain.PlanFuzz.Core/
  UniversalToolchain.PlanFuzz.Adapter.Acme/
  UniversalToolchain.PlanFuzz.Adapter.Wist/
  UniversalToolchain.PlanFuzz.Cli/
  UniversalToolchain.PlanFuzz.Worker/
  UniversalToolchain.PlanFuzz.Tests/
  UniversalToolchain.PlanFuzz.IntegrationTests/
```

### 5.1. `UniversalToolchain.PlanFuzz.Core`

Содержит:

- campaign engine;
- deterministic PRNG;
- testcase schema/model;
- adapter abstractions;
- execution protocol DTOs;
- observation model;
- oracle engine;
- corpus/finding storage;
- reducer orchestration;
- report builders.

Core не ссылается на Wist facade, parser, AST classes или concrete backend projects.

### 5.2. Adapters

Adapter владеет:

- structured program model;
- generator;
- renderer;
- semantic input generator;
- plan-variant generator;
- observation normalizer;
- relation applicability facts;
- program reducer;
- adapter-specific trace extraction.

Acme — первый vertical slice. Wist подключается после подтверждения generic path.

### 5.3. Worker

Worker — отдельный executable, который:

1. читает ровно один request из stdin или файла;
2. проверяет schema/version;
3. загружает только выбранный adapter;
4. исполняет variants;
5. возвращает bounded response;
6. завершается с определённым exit code.

Coordinator владеет timeout, kill-tree, confirmation и artifact preservation.

---

## 6. Testcase model

Минимальная логическая модель:

```csharp
public sealed record ResearchTestCase(
    string SchemaVersion,
    string CaseId,
    ulong Seed,
    AdapterIdentity Adapter,
    ProgramDocument Program,
    IReadOnlyList<ExecutionVariant> Variants,
    ExecutionSchedule Schedule,
    IReadOnlyList<OracleContract> Oracles,
    EnvironmentRequirements Environment,
    CaseProvenance Provenance);
```

### 6.1. Требования к формату

- canonical UTF-8 JSON;
- stable property order;
- invariant-culture numbers;
- no timestamps in semantic identity;
- explicit schema version;
- deterministic `CaseId = SHA256(canonical semantic payload)`;
- unknown required field/version fail closed;
- extensions хранятся в typed adapter-owned namespace;
- secrets и raw environment variables не сериализуются.

### 6.2. Variant

Variant фиксирует:

- language definition/package set;
- package/contribution order;
- entry artifact;
- artifact route;
- backend/executor;
- optimizer/SSA policy;
- fallback policy;
- component lifetime policy;
- input values;
- resource limits;
- expected relation role.

Нельзя восстанавливать semantic variant из display name или concrete type name.

### 6.3. Relation

Для изменения plan требуется тип отношения:

```text
Equivalent
EquivalentOnSharedDomain
ExpectedSameFailure
NegativeSurfacePreserving
ExtensionNoninterfering
DeterministicRebuild
ExpectedFailClosed
IntentionallyDifferent
```

Oracle запускается только при выполненных preconditions соответствующего relation.

---

## 7. Determinism

PlanFuzz должен использовать собственный versioned PRNG contract. Нельзя считать `System.Random` стабильным cross-version serialization contract.

Campaign identity включает:

- master seed;
- PRNG algorithm/version;
- adapter version;
- testcase schema version;
- mutator set version;
- oracle set version;
- repository commit;
- runtime/SDK identity.

Каждый case использует детерминированно выведенный seed, чтобы изменение количества соседних cases не меняло уже сохранённые testcases.

Повторный запуск одинакового campaign manifest обязан генерировать одинаковые canonical cases и variant order. Environment-specific runtime observations могут отличаться только в заранее классифицированных полях.

---

## 8. Program generation

### 8.1. Общие требования

Generator:

- строит well-typed structured programs;
- ограничивает глубину, node count и evaluation cost;
- избегает undefined/unspecified semantics;
- сохраняет renderer determinism;
- умеет порождать positive и intentional-negative cases отдельно;
- публикует coverage features каждого case.

### 8.2. Acme Level 0

Минимальная грамматика модели:

```text
DecimalExpression := Constant
                   | Parameter
                   | Add(Expression, Expression)
                   | Subtract(Expression, Expression)
                   | Multiply(Expression, Expression)
```

Обязательные значения:

- `0`, `1`, `-1`;
- дробные значения;
- большие и малые decimal;
- повторные параметры;
- вложенные операции;
- neutral/absorbing forms.

### 8.3. Wist Level 0

Restricted arithmetic subset:

- numeric constants;
- external parameters;
- unary minus, если canonical parser semantics подтверждена;
- `+`, `-`, `*`, `/`;
- parentheses;
- bounded exceptional cases, например division by zero, только с нормализованной failure relation.

Следующие уровни добавляют calls, conditions, locals/scopes и bounded loops по одному после стабилизации oracle model.

---

## 9. Plan mutation

Mutator не должен произвольно портить JSON. Он работает через typed plan-definition/build APIs или через adapter-owned typed deltas.

Обязательные семейства:

1. physical registration/package order permutation;
2. independent contribution order permutation;
3. equivalent alias/preset selection;
4. backend variant selection;
5. optimizer/SSA policy variation;
6. classified fallback variation;
7. add unused independent extension;
8. exclude capability/feature;
9. unavailable/ambiguous contribution negative case;
10. component lifetime variation where contract allows;
11. lock/plan rebuild determinism;
12. resource-limit boundary variation.

Каждый mutator обязан вернуть:

- generated variant;
- declared relation to source variant;
- applicability facts;
- provenance and mutation ID;
- expected changed and unchanged dimensions.

---

## 10. Execution schedule

MVP schedules:

```text
SingleRun
RepeatedRun(count)
TwoSessionsInterleaved
DisposeOneSessionThenRunOther
SuccessfulPrepareThenFailedPrepareThenRun
RebuildSamePlan
```

Extended schedules:

- bounded parallel calls;
- cancellation;
- concurrent dispose;
- retry after failed activation;
- repeated runtime construction.

Schedule operations сериализуются явно. Worker не скрывает lifecycle действия внутри adapter.

---

## 11. Isolation protocol

### 11.1. Strict mode

Каждый testcase исполняется в fresh process. Coordinator обязан:

- установить wall-clock timeout;
- ограничить stdout/stderr;
- уничтожить process tree;
- различать normal response, timeout, crash, protocol error и infrastructure failure;
- сохранить request, response и bounded logs;
- повторить candidate finding 3/3 в новых processes.

### 11.2. Fast mode

Допускается reused worker только для exploration. Finding из fast mode не считается подтверждённым без strict replay.

### 11.3. Exit semantics

Рекомендуемые коды:

```text
0  valid response produced
10 testcase/schema rejected
20 adapter unavailable
30 controlled execution failure encoded in response
40 worker internal error
50 timeout enforced by coordinator
60 crash or protocol corruption
```

Program failure кодируется в response и не смешивается с worker failure.

---

## 12. Observation model

Observation содержит:

```csharp
public sealed record ExecutionObservation(
    ObservationStatus Status,
    ExecutionStage Stage,
    TypedValueSnapshot? Value,
    NormalizedFailure? Failure,
    PlanSnapshot Plan,
    RouteSnapshot Route,
    LifecycleTrace Lifecycle,
    ProcessSnapshot Process,
    TimingSnapshot Timing);
```

### 12.1. Typed values

Сравнение через `ToString()` запрещено. Snapshot хранит:

- canonical type identity;
- value kind;
- invariant canonical representation;
- exact bytes/bits where meaningful;
- null/type information.

Для float/double policy задаётся oracle contract: bit-exact, numeric with NaN/signed-zero rules или explicit tolerance. Нельзя неявно использовать tolerance для integer/decimal.

### 12.2. Failures

Normalized failure включает:

- category;
- pipeline stage;
- stable diagnostic code, если доступен;
- exception type category;
- bounded sanitized message fingerprint;
- timeout/crash distinction;
- unsupported-vs-internal classification.

Полный exception text не является semantic identity и может содержать volatile или sensitive data.

### 12.3. Traces

Trace должен быть bounded и versioned. Минимальные события:

- plan selected;
- route selected;
- contribution activated;
- backend/executor resolved;
- optimizer/SSA path selected;
- fallback decision;
- session created/disposed;
- provider/component activation.

Instrumentation предпочтительно добавлять через adapter/decorator. Новый production observer contract вводится только при доказанном independent non-Wist use case.

---

## 13. Oracle engine

Каждый oracle возвращает:

```text
Passed
Failed
NotApplicable
Inconclusive
InfrastructureFailure
```

`NotApplicable` не засчитывается как passed. `InfrastructureFailure` не считается compiler defect.

### O-001 Backend parity

**Preconditions:** общий language subset, одинаковые inputs, два backends заявляют support, execution semantics сопоставимы.

**Check:** совпадают typed value либо normalized failure category/stage.

### O-002 Optimization/route parity

**Preconditions:** один source plan, route transformation заявлена semantics-preserving, unsupported fallback классифицирован.

**Check:** baseline и optimized/SSA route эквивалентны; internal pass failure не маскируется fallback.

### O-003 Plan determinism

**Preconditions:** различается только физический enumeration/registration order или другой declared-nonsemantic фактор.

**Check:** совпадают canonical plan, plan hash, selected routes, diagnostics и execution observation.

### O-004 Negative-surface preservation

**Preconditions:** feature/capability/contribution явно исключена.

**Check:** исключённый owner не активирован, intrinsic/provider не появился, fallback не расширил surface, syntax/program отклоняется на ожидаемой стадии.

### O-005 Extension noninterference

**Preconditions:** добавленная extension независима и не используется программой.

**Check:** поведение исходной программы и существующий route не меняются.

### O-006 Controlled fallback

**Preconditions:** policy допускает fallback только для конкретной unsupported classification.

**Check:** unsupported shape может перейти в допустимый fallback; verifier/pass/internal defect обязан завершиться failure и не превращаться в success.

### O-007 Session/runtime isolation

**Check:** две sessions одного artifact не разделяют mutable values; dispose одной session не ломает другую; `PerSession` components не переиспользуются; `SingletonStateless` не приобретает mutable execution state.

### O-008 Route conformance

**Check:** фактически выбранный executor, contracts и artifacts соответствуют `LanguagePlan`; нет wildcard/concrete-name substitution.

### O-009 Canonical lock consistency

**Check:** одинаковая semantic configuration даёт одинаковый lock; lock and plan hash согласованы; altered content/hash/version fail closed.

### O-010 Diagnostic determinism

**Check:** повторный invalid case сохраняет category, stage и stable code независимо от enumeration order; volatile text не участвует в fingerprint.

### O-011 Resource-limit consistency

**Check:** boundary inputs одинаково отклоняются до expensive/unsafe stages для сравниваемых routes.

### O-012 Worker robustness

**Check:** malformed request, timeout, crash и oversized output классифицируются как infrastructure/protocol outcome, а не semantic finding.

---

## 14. Finding lifecycle

Состояния:

```text
Candidate
Confirming
Confirmed
Reducing
Reduced
TriagedRealDefect
TriagedSeededFault
TriagedFalsePositive
TriagedFlaky
TriagedInfrastructure
Inconclusive
Fixed
RegressionProtected
```

### 14.1. Confirmation

Candidate становится confirmed только после 3/3 одинаковых strict fresh-process replays с одним semantic fingerprint.

### 14.2. Fingerprint

Fingerprint строится из:

- oracle ID/version;
- adapter ID/version;
- normalized status/stage/category pair;
- route/backend identities;
- stable diagnostic/trace discriminators;
- optional top owned frame or invariant ID.

Он не включает timestamps, temp paths, PID и полный message text.

### 14.3. Triage package

Для каждого confirmed finding сохраняются:

- original and reduced testcase;
- canonical JSON;
- campaign manifest;
- environment record;
- all variant observations;
- confirmation runs;
- reducer history;
- fingerprint;
- preliminary invariant/root-cause notes;
- fix/regression reference после исправления.

---

## 15. Reducer

Reducer минимизирует четыре измерения независимо и затем совместно:

1. program AST/model;
2. plan delta/package set;
3. variants/oracles;
4. execution schedule and inputs.

### 15.1. Predicate

Reduction step принимается только если:

- case schema remains valid;
- oracle remains applicable;
- same semantic fingerprint reproduces 3/3 strict runs;
- complexity metric strictly decreases.

### 15.2. Program reduction

Adapter предоставляет type-preserving operations:

- replace subtree with child;
- replace expression with typed constant/parameter;
- remove unused declaration/branch;
- reduce nesting;
- simplify input values.

### 15.3. Plan reduction

- remove irrelevant package/contribution;
- remove nonessential option;
- collapse order delta;
- reduce route to smallest differentiating pair;
- remove unused capabilities.

### 15.4. Schedule reduction

- remove operations;
- reduce repetitions;
- shrink interleaving;
- remove unrelated sessions/dispose actions.

Reduction history должна быть replayable.

---

## 16. Corpus и artifact layout

Corpus types:

- hand-authored seeds;
- generated non-findings with new semantic coverage;
- confirmed reduced findings;
- regression corpus;
- seeded-fault corpus;
- invalid/protocol corpus.

Предлагаемый layout:

```text
artifacts/planfuzz/<campaign-id>/
  campaign.json
  environment.json
  cases/
  observations/
  findings/
  corpus/
  metrics/
  logs/
  report.md
  report.json
  MANIFEST.sha256
```

Campaign artifacts не коммитятся в обычный source tree, кроме малого curated regression corpus. Raw publication artifacts поставляются отдельно и имеют recursive manifest.

---

## 17. CLI

Предлагаемые команды:

```text
planfuzz campaign
planfuzz replay
planfuzz reduce
planfuzz triage
planfuzz list-adapters
planfuzz validate-case
planfuzz summarize
```

Пример после реализации:

```bash
dotnet run --project UniversalToolchain/UniversalToolchain.PlanFuzz.Cli -- \
  campaign --adapter acme --seed 1 --cases 10000 --mode strict

dotnet run --project UniversalToolchain/UniversalToolchain.PlanFuzz.Cli -- \
  campaign --adapter wist --profile restricted-arithmetic \
  --seed 1 --cases 5000 --mode strict
```

CLI всегда печатает campaign ID, artifact path, generated/executed counts, oracle outcomes, candidates, confirmed findings, infrastructure failures и exit status.

Exit code `0` означает успешное выполнение campaign, а не отсутствие findings. Отдельная policy определяет, когда confirmed finding делает CI job failed.

---

## 18. Seeded fault suite

Минимальные seeded faults:

1. wrong arithmetic in one backend;
2. order-dependent plan hash;
3. wrong route contribution selected;
4. excluded contribution activated;
5. internal optimizer failure silently falls back;
6. shared mutable `PerSession` state;
7. mutable/disposable stateless singleton misuse;
8. typed/untyped wildcard route accepted;
9. wrong executor identity;
10. component claims deterministic but varies;
11. stale Wist prepared program after failed prepare;
12. optimization miscompile.

Каждый fault обязан:

- активироваться только test configuration;
- обнаруживаться предназначенным oracle;
- иметь negative control;
- подтверждаться и сокращаться;
- не учитываться как real discovered defect.

Если mandatory fault не обнаруживается, соответствующий oracle не считается готовым.

---

## 19. Testing strategy

### 19.1. Unit tests

- PRNG vectors;
- canonical serialization/hash;
- relation applicability;
- typed value normalization;
- failure classification;
- fingerprint stability;
- reducer metrics;
- bounded log handling.

### 19.2. Property tests

- serialize/deserialize/canonicalize idempotence;
- same seed -> same cases;
- nonsemantic order permutations -> same plan;
- reducer never increases complexity;
- accepted reduction preserves fingerprint;
- inapplicable oracle never reports passed/failed.

### 19.3. Integration tests

- Acme interpreter/compiled parity;
- Wist interpreter/CIL parity;
- applicable SSA policy matrix;
- worker timeout/crash/protocol paths;
- session isolation;
- controlled fallback;
- canonical lock consistency;
- finding replay across processes.

### 19.4. Architecture tests

- Core has no Wist references;
- adapters do not leak into production package closure;
- no raw syntax recognition in Core;
- no concrete backend-name branching in generic layer;
- no weakening of existing parity/verifier tests.

Existing relevant tests remain mandatory. PlanFuzz не заменяет их и не может объявляться green только по собственному suite.

---

## 20. CI strategy

### Pull request gate

Только bounded deterministic smoke:

- unit tests;
- seeded-fault subset;
- 50–200 Acme cases;
- fixed seed;
- strict replay одного fixture;
- artifact upload только при failure.

### Scheduled campaign

- larger case budget;
- multiple fixed seeds;
- Acme and Wist adapters;
- metrics/artifact upload;
- controlled timeout;
- no automatic issue creation before confirmation/deduplication.

### Release gate

PlanFuzz не становится обязательным release gate, пока campaign не стабилен, false-positive rate не измерен и runtime budget не ограничен.

---

## 21. Experiment design

Сравниваются режимы:

**B0 — Existing tests.** Текущий handwritten suite и architecture checks.

**B1 — Program-only.** Тот же structured generator, но один fixed plan/backend matrix без plan/lifecycle mutation.

**B2 — Pairwise plan enumeration.** Pairwise combinations configuration dimensions с одинаковым program budget.

**PF — Full PlanFuzz.** Program + plan + route/backend + lifecycle + oracle-aware generation.

### 21.1. Equal budget

Основное сравнение использует одинаковый wall-clock или execution-attempt budget. Дополнительно публикуются cases/sec и phase costs.

### 21.2. Ablations

Отключаются по одному:

- plan mutation;
- route variation;
- lifecycle scheduling;
- negative-surface oracle;
- reduction;
- applicability filtering.

### 21.3. Метрики

- unique confirmed real defects;
- time to first defect;
- defects per execution-hour;
- root-cause classes;
- candidate-to-real ratio;
- false-positive and flaky rates;
- semantic dimension coverage;
- reduction ratio/time;
- worker/process overhead;
- replay success rate.

Реальный defect считается один раз по root cause, даже если найден множеством cases/oracles.

---

## 22. Этапы реализации

### Phase 0 — Baseline lock and skeleton

Deliverables:

- project skeleton;
- architecture decision record;
- testcase/worker schemas;
- deterministic PRNG;
- artifact layout;
- adapter contract;
- no-op worker roundtrip.

Acceptance:

- Core has no Wist dependencies;
- same seed produces byte-identical cases;
- request/response roundtrip works in fresh process;
- docs checks remain green.

### Phase 1 — Acme vertical slice

Deliverables:

- structured Acme generator/renderer;
- order mutation;
- interpreter/compiled variants;
- typed decimal snapshots;
- backend parity;
- plan determinism;
- replay;
- one wrong-arithmetic seeded fault.

Acceptance:

- complete generate-to-finding path;
- seeded fault detected and replayed 3/3;
- clean Acme campaign has no unexplained finding;
- 10,000 fixed-seed pilot completes within bounded budget.

### Phase 2 — Wist arithmetic and route matrix

Deliverables:

- restricted arithmetic adapter;
- interpreter/CIL variants;
- applicable SSA policies;
- normalized diagnostics;
- optimization parity and controlled fallback.

Acceptance:

- shared subset declared explicitly;
- unsupported shapes classified;
- internal seeded pass defect never falls back to success;
- 5,000-case Wist pilot completes with triaged outcomes.

### Phase 3 — Lifecycle and negative surface

Deliverables:

- session schedules;
- lifecycle trace;
- excluded capability/contribution variants;
- state isolation and negative-surface oracles.

Acceptance:

- seeded state leak detected;
- unused/excluded component activation detectable;
- disposed-session behavior classified;
- traces bounded and deterministic enough for fingerprints.

### Phase 4 — Reducer and corpus

Deliverables:

- program/plan/schedule reduction;
- corpus storage;
- deduplication;
- reduction history.

Acceptance:

- every mandatory seeded fault reduces;
- reduced case reproduces 3/3;
- complexity monotonically decreases;
- history is replayable.

### Phase 5 — Research campaigns

Deliverables:

- B1/B2/PF modes;
- ablations;
- semantic coverage metrics;
- raw reports;
- triage workflow.

Acceptance:

- equal-budget comparison complete;
- real/seeded/flaky/infrastructure outcomes separate;
- all confirmed findings classified;
- raw artifact manifest preserved.

### Phase 6 — External validation

Recommended:

- third adapter;
- independent clean-machine replay;
- artifact evaluation package;
- external maintainer confirmation where possible.

---

## 23. Definition of Done

PlanFuzz MVP реализован только если одновременно:

1. Core не содержит Wist-specific hardcode.
2. Generation deterministic and versioned.
3. Strict out-of-process execution работает с timeout/kill-tree.
4. Acme и Wist adapters используют один core path.
5. Реализованы минимум семь обязательных oracles:
   - backend parity;
   - optimization parity;
   - plan determinism;
   - negative surface;
   - controlled fallback;
   - state isolation;
   - route conformance.
6. Mandatory seeded-fault suite проходит.
7. Replay and reduction воспроизводимы.
8. Clean baseline campaigns не содержат необъяснённых false positives.
9. Existing relevant suite remains green.
10. CLI examples and documentation smoke pass.
11. Campaign artifact имеет recursive manifest.
12. Reports раздельно учитывают real defects, seeded faults, flaky outcomes, infrastructure failures и inconclusive cases.

Реализация MVP сама по себе не доказывает исследовательскую гипотезу. Publication-ready claim требует реальных confirmed findings и controlled baseline comparison.

---

## 24. Риски и replan triggers

### R1. Нет реальных дефектов

**Trigger:** Acme + Wist pilots находят только seeded faults и known regressions.

**Action:** усилить plan/lifecycle mutations, расширить Wist surface, добавить third adapter или сузить claim до negative-surface/route-conformance testing. Не утверждать превосходство без данных.

### R2. Высокий false-positive rate

**Trigger:** более 10% candidates после triage — ошибки oracle/model.

**Action:** остановить большой campaign, усилить applicability facts и typed snapshots, разделить positive/negative profiles.

### R3. Process startup доминирует

**Trigger:** более 70% strict campaign time уходит на startup.

**Action:** оставить fresh-process confirmation, добавить bounded batch workers только для exploration.

### R4. Generator становится вторым parser

**Trigger:** Core или reducer анализирует raw source regex'ами.

**Action:** остановить развитие этой ветки и вернуть adapter-owned structured model + renderer.

### R5. Research tool загрязняет packages

**Trigger:** public Wist API/package closure изменяется без необходимости.

**Action:** вынести instrumentation в decorators/adapters; новый production contract требует независимого use case.

### R6. Novelty boundary слаба

**Trigger:** literature review обнаруживает близкий configuration-aware compiler fuzzer.

**Action:** сравнить точные dimensions/oracles; сфокусировать вклад на executable language plans, negative surface, lifecycle или route conformance; не выдумывать algorithmic novelty.

---

## 25. Первый mergeable milestone

Кодирование должно начинаться с узкого vertical slice:

```text
Acme structured generator
+ registry-order mutation
+ interpreter/compiled variants
+ typed decimal observation
+ backend parity
+ plan determinism
+ fresh worker per case
+ finding replay
+ wrong-arithmetic seeded fault
```

Сначала доказывается полный путь:

```text
generate
-> serialize
-> isolated execute
-> observe
-> compare
-> confirm
-> preserve evidence
```

Нельзя одновременно начинать с Wist loops, concurrency, reducer и coverage guidance. Каждое новое измерение добавляется после того, как предыдущий путь детерминирован, replayable и защищён seeded fault.

---

## 26. Итоговый комплект поставки

```text
Source code
CLI and worker executables
Acme adapter
Wist adapter
Versioned testcase and protocol schemas
Seeded fault fixtures
Unit/integration/architecture tests
Deterministic seed corpus
Finding replay and reducer
Campaign artifact writer
Research protocol
Raw CSV/JSON results
Markdown report
Reproduction scripts
Recursive SHA-256 manifest
```

Точные project paths и команды должны быть синхронизированы с фактической solution/package matrix при реализации. Proposal не является основанием добавлять несуществующие команды в public user documentation.
