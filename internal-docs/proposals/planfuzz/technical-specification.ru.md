# PlanFuzz

## Техническое задание на систему конфигурационно-ориентированного дифференциального тестирования расширяемых языков

**Версия ТЗ:** 0.3  
**Дата:** 24 июля 2026 г.  
**Целевой репозиторий:** `Misha1302/Wist2`  
**GitHub baseline:** `master@7f2b5819f712d03c39270349b6b39e914b79e008`; реализация развивается поверх PlanFuzz proposal branch  
**Целевая платформа:** .NET 10, C# 14, Linux x64; Windows поддерживается на уровне воспроизведения и CI  
**Статус документа:** living implementation and experiment specification; Phase 0 и первый Acme vertical slice реализованы, последующие этапы остаются gated  

---

> **Статус реализации.** Этот документ задаёт целевую систему целиком. Фактически реализованный subset и проверенные observables перечислены в [implementation-status.md](implementation-status.md). Наличие требования ниже не означает, что соответствующий oracle, adapter, reducer или campaign stage уже реализован.

# 1. Назначение

PlanFuzz — исследовательский инструмент для автоматического обнаружения дефектов, возникающих на пересечении:

- входной программы;
- выбранных языковых возможностей;
- package/contribution composition;
- artifact routes;
- backend’ов;
- optimizer/IR routes;
- fallback policy;
- runtime lifecycle и execution schedule.

Обычный compiler fuzzer преимущественно изменяет программу. PlanFuzz должен изменять и программу, и исполняемый языковой план, и маршрут исполнения, после чего проверять заданные метаморфические отношения и сквозные архитектурные инварианты.

Каноническая модель testcase:

```text
TestCase = Program
         × LanguagePlanVariant
         × Backend/OptimizationVariant
         × ExecutionSchedule
         × OracleSet
```

PlanFuzz не должен предполагать, что все варианты обязаны завершаться успешно или возвращать одинаковое значение. Каждый вариант обязан сопровождаться явным типом ожидаемого отношения: эквивалентность, одинаковая категория отказа, отсутствие новой возможности, детерминизм композиции, изоляция состояния либо ожидаемое fail-closed отклонение.

---

# 2. Целевой исследовательский результат

## 2.1. Центральная гипотеза

> Конфигурационно-ориентированная генерация и проверка программ, language plans, execution routes и lifecycle schedules обнаруживает классы дефектов расширяемых компиляторов, которые не обнаруживаются program-only fuzzing и обычными handwritten tests при сопоставимом бюджете исполнения.

## 2.2. Основные исследовательские вопросы

### RQ1. Defect discovery

Обнаруживает ли PlanFuzz реальные ранее неизвестные дефекты, связанные с взаимодействием plan composition, backend’ов, оптимизаций и lifecycle?

### RQ2. Incremental value

Какие дефекты обнаруживаются только при добавлении следующих измерений:

1. program generation;
2. plan mutation;
3. optimization/backend variation;
4. lifecycle scheduling;
5. negative-surface oracle?

### RQ3. Cost

Какова стоимость PlanFuzz по фазам:

- generation;
- plan compilation;
- runtime creation;
- execution;
- oracle evaluation;
- replay;
- reduction?

### RQ4. Reduction quality

Насколько reducer уменьшает программу, plan delta и schedule, сохраняя тот же defect fingerprint?

### RQ5. Generality

Работает ли общий механизм минимум для двух независимо устроенных языков:

- Wist как reference language;
- `Acme.PricingLanguage` как non-Wist external language?

## 2.3. Условия научно сильного результата

Работа считается исследовательски успешной, если выполнены все условия:

1. Найдены подтверждённые дефекты минимум двух различных root-cause classes.
2. Минимум один дефект требует изменения language plan, route или lifecycle и не воспроизводится program-only baseline.
3. Каждый засчитанный дефект:
   - воспроизводится в strict isolated mode;
   - имеет минимизированный testcase;
   - имеет root-cause analysis;
   - получил regression test;
   - отделён от seeded mutants.
4. Проведено сравнение с минимум двумя baseline-режимами.
5. Сохранён воспроизводимый raw evidence package.

Seeded/fault-injected defects используются только для проверки адекватности инструмента и не засчитываются как реальные найденные дефекты.

---

# 3. Baseline и доказательная граница

## 3.1. Фактически инспектированный baseline

В baseline присутствуют:

- `UniversalToolchain.Language.Abstractions`;
- `UniversalToolchain.FeatureSdk`;
- `UniversalToolchain.LanguageSdk`;
- `UniversalToolchain.LanguageAuthoring`;
- `UniversalToolchain.Runtime`;
- `UniversalToolchain.Testing`;
- `UniversalToolchain.Wist`;
- Wist interpreter/CIL paths;
- verifier-gated Wist `AIR -> SSA -> AIR` route;
- `samples/Acme.PricingLanguage` с interpreter и compiled backend;
- schema-v5 language lock serialization;
- deterministic `LanguagePlan.PlanHash`;
- exact runtime provider/contribution/backend/contract checks;
- component lifetimes `PerSession` и `SingletonStateless`;
- disposal and in-flight-operation coordination.

Рекурсивный `MANIFEST.sha256` распакованного baseline проверен: 1552 записи прошли проверку.

`VERIFICATION.md` baseline фиксирует 1411 успешных тестов, 85/85 построенных проектов и девять проверенных packages. В рамках составления этого ТЗ clean build и повторный запуск 1411 тестов не выполнялись; эти числа считаются записанным baseline evidence, а не новым воспроизведением.

## 3.2. Архитектурные ограничения baseline

PlanFuzz обязан сохранять следующие границы:

- generic core не знает Wist-specific IDs, modules, syntax или backend types;
- Wist-specific поведение находится только в Wist adapter;
- PlanFuzz core не распознаёт синтаксис из raw source;
- program generation и reduction используют adapter-owned structured model;
- frontend не получает зависимость от backend implementation;
- runtime исполняет именно route, зафиксированный в `LanguagePlan`;
- unsupported behavior не превращается в silent success;
- `LanguagePlan`, descriptors и observations рассматриваются как immutable snapshots;
- отсутствуют глобальные mutable registries;
- reflection, если используется, ограничена явно выбранными packages/components и не выполняется в hot execution path;
- существующие проверки не ослабляются ради пропуска generated cases.

---

# 4. Область работ

## 4.1. Входит в MVP

1. Детерминированный campaign coordinator.
2. Версионированный формат testcase.
3. Изолированный worker process.
4. Generic adapter contract.
5. Adapter для `Acme.PricingLanguage`.
6. Adapter для Wist restricted arithmetic.
7. Плановые вариации и package-order permutations.
8. Backend parity.
9. Plan determinism.
10. Optimization/route parity для применимого Wist subset.
11. Controlled fallback checks.
12. Negative-surface checks.
13. Runtime isolation/lifecycle scenarios.
14. Typed value and failure normalization.
15. Finding replay, deduplication и reduction.
16. Seeded fault fixtures.
17. CLI и machine-readable reports.
18. Raw research artifact output.

## 4.2. Входит в расширенную версию

- Wist conditions, locals, scopes, loops и SafeMath calls;
- concurrent schedules;
- richer negative program generation;
- external third-party language adapter;
- coverage-guided scheduling;
- distributed workers;
- optional code coverage;
- adaptive mutator weighting;
- cross-SDK/cross-OS differential campaigns.

## 4.3. Не входит

- создание нового parser generator;
- общий grammar inference;
- формальное доказательство корректности UniversalToolchain;
- hostile-extension sandbox;
- fuzzing произвольного CLR IL;
- security claim о безопасном исполнении недоверенного кода;
- замена всех существующих unit/integration tests;
- автоматическое принятие найденного расхождения за compiler bug;
- performance benchmark самого Wist языка;
- скрытая загрузка произвольных plugins через AppDomain/filesystem scan.

---

# 5. Термины

**Campaign** — одно детерминированное исследовательское выполнение с фиксированными options, seed, adapter versions и environment record.

**Case** — полностью сериализуемый набор входов, variants, schedule и oracle contracts.

**Variant** — одна конкретная конфигурация plan/backend/optimization/runtime.

**Relation** — ожидаемая связь между observations двух или нескольких variants.

**Observation** — нормализованный результат одной execution attempt, включая stage, value, failure, trace и process status.

**Finding** — подтверждённое нарушение oracle contract.

**Defect fingerprint** — стабильная семантическая сигнатура finding для deduplication и reduction.

**Strict mode** — fresh worker process для каждого testcase с timeout и kill-tree.

**Fast mode** — опциональный reused worker для предварительного исследования; finding не считается подтверждённым без strict replay.

**Seeded fault** — намеренно добавленный test-only defect, проверяющий, что соответствующий oracle способен сработать.

**Negative surface** — набор features, contributions, capabilities, providers, intrinsics и execution paths, которые language plan обязуется не допускать.

---

# 6. Архитектура решения

## 6.1. Проекты

MVP должен состоять из следующих проектов:

```text
UniversalToolchain/
  UniversalToolchain.PlanFuzz.Core/
  UniversalToolchain.PlanFuzz.Adapter.Acme/
  UniversalToolchain.PlanFuzz.Adapter.Wist/
  UniversalToolchain.PlanFuzz.Cli/
  UniversalToolchain.PlanFuzz.Tests/
  UniversalToolchain.PlanFuzz.IntegrationTests/
```

### `UniversalToolchain.PlanFuzz.Core`

Содержит:

- campaign engine;
- deterministic PRNG;
- testcase schema/model;
- execution protocol;
- observation model;
- oracle engine;
- corpus and finding storage;
- reducer orchestration;
- report builders;
- generic adapter contracts.

Разрешённые зависимости:

- `UniversalToolchain.Language.Abstractions`;
- `UniversalToolchain.LanguageSdk` только через стабильные plan/lock DTO или выделенный abstraction package;
- стандартная библиотека .NET.

Запрещённые зависимости:

- `UniversalToolchain.Wist`;
- Wist modules;
- Wist AST types;
- `BasicCilCompiler`;
- `BasicInterpreter`;
- конкретные backend types;
- test projects.

### `UniversalToolchain.PlanFuzz.Adapter.Acme`

Содержит structured model, generator, renderer, plan variants, execution adapter и reducer для Acme.

### `UniversalToolchain.PlanFuzz.Adapter.Wist`

Содержит Wist-specific structured models, renderer, plan/profile variants, backend mapping, SSA policy mapping и diagnostic normalization.

### `UniversalToolchain.PlanFuzz.Cli`

Содержит:

- command parsing;
- campaign orchestration;
- worker launch;
- replay;
- reduction command;
- inspect/report commands.

### Tests

- `UniversalToolchain.PlanFuzz.Tests` — unit and property-like tests.
- `UniversalToolchain.PlanFuzz.IntegrationTests` — out-of-process, package/order/backend/lifecycle tests.

## 6.2. Направление зависимостей

```text
PlanFuzz.Cli
  -> PlanFuzz.Core
  -> adapter registry
       -> Adapter.Acme
       -> Adapter.Wist

Adapter.Acme
  -> Language SDK / Runtime / Acme package factory

Adapter.Wist
  -> Wist facade or Wist language-pack adapter
  -> Wist-specific testing helpers

PlanFuzz.Core
  -X-> Wist
  -X-> Acme
  -X-> concrete backend implementation
```

## 6.3. Extension model

Для MVP adapters регистрируются явно в CLI composition root. Blind runtime scanning запрещён.

В будущем допускается статический generated registry или explicit package list. Dynamic plugin loading не входит в MVP.

---

# 7. Принципы реализации

1. **Correctness before throughput.** Сначала strict deterministic path, затем fast exploration.
2. **One semantic owner.** PlanFuzz не дублирует parser, planner или runtime semantics.
3. **No raw string contracts.** IDs и categories typed/versioned.
4. **Fail closed.** Неизвестная schema, adapter, oracle или version прекращает выполнение case.
5. **Relation-aware generation.** Mutator обязан объявить ожидаемое отношение.
6. **Typed observations.** Value сравнивается по type/value contract, не по `ToString()`.
7. **No silent fallback.** Fallback является observable event.
8. **No publication by implementation.** Наличие инструмента не доказывает исследовательский claim.
9. **Reproducibility first.** Case и evidence должны воспроизводиться без исходного campaign process.
10. **Bounded everything.** Размеры, время, attempts, logs, source, trace и reducer budgets ограничены.

---

# 8. Determinism model

## 8.1. Требования к PRNG

Использовать собственную стабильную реализацию PRNG с фиксированным algorithm ID, например:

```text
xoshiro256** / planfuzz-prng-v1
```

Запрещено использовать `System.Random` как cross-version reproducibility contract.

PRNG API:

```csharp
public interface IPlanFuzzRandom
{
    ulong NextUInt64();
    int NextInt32(int exclusiveUpperBound);
    bool NextBoolean();
    T Pick<T>(IReadOnlyList<T> items);
    IPlanFuzzRandom Fork(string domain);
}
```

`Fork(domain)` обязан создавать независимый stream через hash master state + UTF-8 domain, а не потреблять случайное число из родителя.

Домены:

```text
case/<index>
program
inputs
plan
backend
schedule
reduction/<step>
```

## 8.2. Campaign identity

Campaign manifest включает:

- master seed;
- PRNG algorithm/version;
- adapter IDs/versions;
- testcase schema version;
- mutator-set version;
- oracle-set version;
- repository commit/archive hash;
- .NET SDK/runtime identity;
- OS/architecture;
- relevant environment options.

## 8.3. Golden tests

Обязательны tests:

- первые N значений PRNG для фиксированного seed;
- одинаковый fork domain — одинаковый stream;
- разные domains — разные streams;
- generated case JSON идентичен byte-for-byte;
- изменение числа соседних cases не меняет case с тем же index;
- canonical case ID стабилен.

---

# 9. Testcase schema

## 9.1. Формат

Файл `case.json`:

```json
{
  "schemaVersion": 1,
  "caseId": "sha256:...",
  "campaign": {
    "masterSeed": "1844674407370955161",
    "caseIndex": 42,
    "caseSeed": "...",
    "prng": "planfuzz-prng-v1"
  },
  "adapter": {
    "id": "acme-pricing",
    "version": "1"
  },
  "program": {
    "modelKind": "acme-decimal-expression-v1",
    "model": {},
    "renderedSource": "unitPrice * quantity - discount",
    "classification": "ValidDeterministic"
  },
  "variants": [],
  "schedule": {},
  "oracles": [],
  "environmentRequirements": {},
  "provenance": {}
}
```

## 9.2. Case identity

```text
CaseId = SHA256(CanonicalJson(SemanticCaseBody))
```

Не включать в semantic identity:

- timestamps;
- output path;
- process ID;
- machine-specific temp paths;
- absolute repository path;
- elapsed time;
- stdout/stderr ordering.

Включать:

- seed/index;
- adapter/version;
- structured program;
- variants;
- schedule;
- oracle contracts;
- semantic limits;
- mutation and seeded-fault IDs.

## 9.3. Compatibility

- Unknown major schema version — reject.
- Unknown required enum/field — reject.
- Unknown optional extension namespace — preserve or explicitly reject according to declared policy.
- No implicit default for semantic fields.
- Migration tool required before changing stored corpus schema.

## 9.4. Immutability

DTO constructors copy mutable collections. JSON deserialization produces fully validated immutable object graph.

---

# 10. Core domain model

Минимальные типы:

```csharp
public sealed record PlanFuzzCase(
    CaseIdentity Identity,
    AdapterIdentity Adapter,
    ProgramDocument Program,
    IReadOnlyList<ExecutionVariant> Variants,
    ExecutionSchedule Schedule,
    IReadOnlyList<OracleContract> Oracles,
    EnvironmentRequirements Environment,
    CaseProvenance Provenance);

public sealed record ExecutionVariant(
    VariantId Id,
    LanguagePlanDocument Plan,
    BackendRouteSelection Route,
    RuntimePolicyDocument RuntimePolicy,
    InputDocument Inputs,
    ExpectedRelationRole Role);

public sealed record OracleContract(
    OracleId Id,
    IReadOnlyList<VariantId> Subjects,
    OraclePreconditions Preconditions,
    ComparisonPolicy Policy);
```

Каждый ID должен быть стабильной value-semantic сущностью, а не `Type.Name`.

---

# 11. Adapter contract

## 11.1. Generic interface

```csharp
public interface IPlanFuzzLanguageAdapter
{
    AdapterIdentity Identity { get; }
    AdapterCapabilities Capabilities { get; }

    GeneratedProgram GenerateProgram(
        IPlanFuzzRandom random,
        GenerationProfile profile);

    IReadOnlyList<PlanVariant> GenerateVariants(
        GeneratedProgram program,
        IPlanFuzzRandom random,
        VariantProfile profile);

    WorkerExecutionRequest CreateExecutionRequest(
        PlanFuzzCase testCase,
        ExecutionVariant variant);

    NormalizedObservation Normalize(
        WorkerExecutionResponse response,
        ExecutionVariant variant);

    OracleApplicability DescribeApplicability(
        PlanFuzzCase testCase,
        OracleContract oracle);

    IEnumerable<CaseReduction> ProposeReductions(
        PlanFuzzCase testCase,
        FindingFingerprint fingerprint);
}
```

Это логический контракт. Фактическая сигнатура может быть разбита на generator/executor/reducer interfaces, если это уменьшает зависимости.

## 11.2. Обязанности adapter

Adapter владеет:

- structured syntax model;
- program renderer;
- input generation;
- plan variants;
- relation preconditions;
- value normalization;
- adapter-specific failure mapping;
- adapter-specific reduction;
- optional trace extraction.

Core владеет:

- deterministic scheduling;
- process isolation;
- generic testcase storage;
- oracle invocation;
- finding confirmation;
- deduplication;
- reducer orchestration;
- campaign reporting.

## 11.3. Versioning

Adapter version меняется при изменении:

- generated semantics;
- renderer;
- normalization;
- plan variant semantics;
- reduction behavior;
- relation applicability.

Cosmetic log changes version не требуют.

---

# 12. Program models

## 12.1. Общие требования

Generator обязан:

- строить well-typed program model;
- ограничивать depth/node count/evaluation cost;
- избегать undefined or intentionally unspecified semantics;
- разделять valid и intentional-negative profiles;
- обеспечивать deterministic rendering;
- сохранять structured model в testcase;
- публиковать semantic features для coverage.

## 12.2. Acme Level 0

Structured model:

```text
DecimalExpression :=
    Constant(decimal)
  | Parameter(unitPrice | quantity | discount)
  | Add(left, right)
  | Subtract(left, right)
  | Multiply(left, right)
```

Параметры:

```text
unitPrice: decimal
quantity: decimal
conversionRate: decimal
fixedFee: decimal
```

Минимальная shipped Acme syntax может быть уже существующей. Adapter обязан рендерить только поддерживаемый subset и не подменять production parser.

Value corpus:

- 0;
- 1;
- -1;
- min/max bounded decimal;
- fractions;
- values close to scale boundary;
- repeated parameters;
- nested neutral forms;
- multiplication by 0/1;
- subtraction from self.

Избегать overflow, если case не классифицирован как expected same failure.

## 12.3. Wist Level 0

Structured model restricted arithmetic:

```text
WistExpression :=
    Number
  | ExternalParameter
  | UnaryMinus
  | Add
  | Subtract
  | Multiply
  | DivideNonZero
  | Parenthesized
```

Начальный numeric profile:

- `double` или один другой явно выбранный type;
- finite values по умолчанию;
- NaN/Infinity отдельным profile;
- division denominator bounded away from zero;
- exact comparison policy заранее определён.

## 12.4. Wist Level 1+

После Level 0:

- SafeMath calls;
- boolean/comparison;
- conditions;
- locals;
- scopes;
- loops с bounded iteration proof;
- typed null;
- external managed calls только в trusted profile.

Каждое расширение должно добавляться отдельным generation capability и ablation dimension.

---

# 13. Plan variation

## 13.1. Relation-aware mutator contract

```csharp
public interface IPlanVariantMutator
{
    MutatorIdentity Identity { get; }
    bool IsApplicable(BaseVariant input, ProgramFacts facts);
    PlanMutationResult Apply(BaseVariant input, IPlanFuzzRandom random);
}

public sealed record PlanMutationResult(
    ExecutionVariant Variant,
    ExpectedRelation Relation,
    MutationProofFacts Facts);
```

Mutator не имеет права сообщать `Equivalent`, если его transformation может менять semantics для данного program/profile.

## 13.2. Обязательные mutators

1. Registry insertion order permutation.
2. Equivalent package enumeration order.
3. Explicit backend selection.
4. Enable/disable optimizer при proven shared semantics.
5. SSA policy variation `Disabled/Prefer/Require` по applicability.
6. Add unused independent feature.
7. Remove used feature — expected fail closed.
8. Exclude capability.
9. Introduce conflicting package contribution — expected deterministic failure.
10. Change component lifetime.
11. Change artifact route where semantic equivalence declared.
12. Change package version/hash for lock mismatch negative case.

## 13.3. Запрещённые naïve mutations

- удаление случайного feature с ожиданием same result;
- смена numeric profile без conversion policy;
- включение unsafe optimizer без semantic descriptors;
- перестановка passes, если order не заявлен commutative;
- сравнение `Prefer` и `Require` без классификации unsupported shape;
- добавление provider, имеющего side effects, как independent feature.

---

# 14. Execution variants

Variant обязан фиксировать:

- package registry snapshot;
- language definition;
- canonical lock;
- plan hash;
- entry artifact;
- route contributions;
- backend/contribution ID;
- optimizer/SSA policy;
- fallback policy;
- runtime policy;
- component lifetimes;
- input values;
- resource limits;
- expected relation role;
- mutation/seeded-fault provenance.

Worker не должен самостоятельно выбирать «лучший доступный backend». Он исполняет зафиксированный variant либо возвращает explicit route/provider error.

---

# 15. Изоляция worker process

## 15.1. Strict worker

Coordinator для каждого testcase:

1. создаёт immutable request file;
2. запускает worker;
3. передаёт один case/variant;
4. читает bounded stdout/stderr асинхронно;
5. применяет timeout;
6. при timeout убивает process tree;
7. атомарно сохраняет response;
8. проверяет response identity;
9. завершает worker.

Findings подтверждаются только strict workers.

## 15.2. Fast worker

Допускается опционально:

- worker обрабатывает bounded batch;
- перезапуск после N cases;
- no finding confirmation;
- используется только для exploration.

## 15.3. Exit codes

```text
0  execution completed; observation available
2  invalid case/schema
3  unsupported adapter/version
4  deterministic program failure captured
5  worker internal failure
6  timeout recorded by coordinator
7  cancellation
```

Program failure не равен process failure. Если программа ожидаемо rejected, worker должен вернуть валидную observation, а не crash.

## 15.4. Output bounds

- stdout max bytes;
- stderr max bytes;
- trace max entries/bytes;
- source max bytes;
- observation max bytes;
- truncation explicit.

## 15.5. Path policy

- coordinator owns campaign root;
- worker не читает arbitrary path из testcase;
- artifact path canonicalized;
- path traversal rejected;
- symlink policy explicit;
- no environment-secret dump.

---

# 16. Observation model

## 16.1. DTO

```csharp
public sealed record NormalizedObservation(
    ObservationSchemaVersion Schema,
    CaseId CaseId,
    VariantId VariantId,
    ExecutionStatus Status,
    ValueSnapshot? Value,
    FailureSnapshot? Failure,
    PlanSnapshot Plan,
    RouteSnapshot Route,
    TraceSnapshot Trace,
    ProcessSnapshot Process,
    EnvironmentSnapshot Environment,
    ObservationCompleteness Completeness);
```

## 16.2. Surface/owner evidence contract

Текущий observation schema — v5. Surface evidence contract v3 разделяет semantic surface IDs и runtime owner IDs и явно связывает каждый independent extension с обоими доменами:

```csharp
public sealed record IndependentExtensionEvidence(
    string ExtensionId,
    IReadOnlyList<string> SurfaceIds,
    IReadOnlyList<string> OwnerIds);

public sealed record SurfaceEvidence(
    int EvidenceContractVersion,
    IReadOnlyList<string> SelectedSurfaceIds,
    IReadOnlyList<string> SelectedOwnerIds,
    IReadOnlyList<string> ExcludedOwnerIds,
    IReadOnlyList<string> DeclaredIndependentSurfaceIds,
    IReadOnlyList<string> DeclaredIndependentOwnerIds,
    IReadOnlyList<IndependentExtensionEvidence> IndependentExtensions,
    IReadOnlyList<string> ActivatedOwnerIds,
    ActivationTraceStatus ActivationTraceStatus,
    string TraceKind,
    string RouteIdentity);
```

Инварианты fail-closed:

- blank, whitespace-surrounded и duplicate IDs rejected;
- selected и excluded owner sets disjoint;
- independent IDs являются subset соответствующего selected domain;
- activated owner должен быть selected либо explicitly excluded;
- `Complete` требует непустого selected-owner set;
- unknown evidence-contract version и unknown trace status rejected;
- extension IDs unique;
- каждый independent surface/owner принадлежит ровно одному binding;
- bindings покрывают declared-independent sets точно, без пропусков и лишних IDs;
- schema-v1..v3 остаются читаемыми для истории;
- schema-v4/evidence-v2 остаётся пригодной для O-004, но не может дать `Passed` текущему O-005 без explicit bindings.

`ActivationTraceStatus` — typed enum: `Unsupported`, `Partial`, `Complete`. Boolean completeness больше не является текущим контрактом.

## 16.3. Typed value snapshot

```csharp
public sealed record ValueSnapshot(
    string SemanticType,
    ValueEncoding Encoding,
    string CanonicalValue,
    IReadOnlyDictionary<string, string> Properties);
```

Не использовать:

```csharp
actual?.ToString()
```

как основной oracle.

## 16.3. Numeric policies

### Integer/decimal

Exact canonical comparison, если semantics exact.

### Floating point

Policy задаёт:

- bitwise equality;
- IEEE semantic equality;
- tolerance;
- NaN equivalence;
- signed zero distinction.

Tolerance не может быть global default без обоснования.

## 16.4. Failure snapshot

```csharp
public sealed record FailureSnapshot(
    FailureStage Stage,
    string Category,
    string? StableCode,
    string? ExceptionType,
    BoundedMessage Message,
    SourceSpan? Span,
    bool Expected);
```

Сравнение по category/stage/code; raw message используется только как evidence и diagnostic aid.

## 16.5. Plan snapshot

- `PlanHash`;
- canonical lock hash;
- ordered selected features;
- ordered selected contributions;
- selected routes;
- selected executor;
- policies/lifetimes;
- normalized planning failure.

## 16.6. Trace snapshot

Минимальные события:

- contribution selected;
- runtime component activated;
- executor resolved;
- route stage executed;
- fallback decision;
- session created/disposed;
- worker timeout/process failure.

Instrumentation должна быть adapter/decorator-owned. Не расширять production public API только ради research tool без independent use case.

---

# 17. Oracle engine

## 17.1. Contract

```csharp
public interface IPlanFuzzOracle
{
    OracleIdentity Identity { get; }
    OracleApplicability CheckApplicability(OracleContext context);
    OracleResult Evaluate(OracleContext context);
}
```

OracleResult:

```text
Passed
Violated
NotApplicable
Inconclusive
InfrastructureFailure
```

## 17.2. Общие требования

- oracle pure относительно observations;
- deterministic;
- no hidden runtime execution;
- applicability проверяется до evaluation;
- missing evidence не считается pass;
- unknown status fail closed;
- result содержит explanation и fingerprint material;
- oracle version входит в case/campaign identity.

---

# 18. Обязательные oracles

## O-001. Backend parity

### Проверяет

Два backend’а одного shared language subset возвращают эквивалентный typed result либо одинаковую normalized failure.

### Preconditions

- один program model;
- один semantic input;
- оба backend’а объявляют поддержку feature set;
- relation `EquivalentOnSharedDomain`;
- нет intentionally different route.

### Сравнение

- status;
- value semantic type;
- canonical value;
- failure stage/category/code.

### Seeded fault

Compiled Acme backend меняет subtraction на addition.

---

## O-002. Optimization/route parity

### Проверяет

Включение optimization или `AIR -> SSA -> AIR` route не меняет semantics.

### Preconditions

- base and optimized variants;
- program входит в supported optimizer subset;
- no unknown side-effect callable;
- route completed без classified fallback либо fallback является отдельным checked relation.

### Seeded fault

Optimizer заменяет `x - y` на `x + y`.

---

## O-003. Plan determinism

### Проверяет

Equivalent physical discovery/registration order не меняет canonical plan.

### Сравнение

- plan hash;
- canonical lock;
- selected features;
- selected contributions;
- route order;
- diagnostics.

### Seeded fault

Planner использует first registration wins при equal candidates вместо deterministic resolution/conflict.

---

## O-004. Negative-surface preservation

### Проверяет

Если capability/feature/provider отсутствует в selected plan, ни activation trace, ни route, ни runtime call не использует его.

Формально:

```text
f ∉ SelectedSurface(P)
⇒
Owner(f) ∉ ActivationTrace(P, program)
```

### Preconditions

- отрицание нормализовано в явный `ExcludedOwnerIds`, а не смешано с feature/capability IDs;
- используется текущая версия evidence contract;
- trace имеет статус `Complete` для заявленного `traceKind`;
- каждый activated owner объявлен selected либо explicitly excluded;
- program/plan relation требует exclusion.

Oracle агрегирует все variants независимо от их порядка. Детерминированный приоритет результата:

```text
Violated > InfrastructureFailure > Inconclusive > NotApplicable > Passed
```

Подтверждённое пересечение `ExcludedOwnerIds ∩ ActivatedOwnerIds` не может быть скрыто неполной трассой другого variant.

### Seeded faults

- activate-all-before-filter;
- provider allowlist derived from AIR;
- fallback reintroduces excluded feature.

---

## O-005. Extension noninterference

### Проверяет

Добавление одного independent unused extension не меняет semantics, selected route или фактически activated owners для программы, которая extension не использует.

### Preconditions

- baseline/extension direction выводится из strict additive relation, а не из порядка variant IDs;
- в selected surface и selected owner domains нет удалений и есть непустые additions;
- additions точно совпадают с newly declared independent surface/owner IDs;
- появляется ровно один новый `IndependentExtensionEvidence`, который связывает exact added surfaces и owners одним stable extension ID;
- все прежние extension bindings сохраняются;
- `Extended.ExcludedOwnerIds = Baseline.ExcludedOwnerIds − AddedOwnerIds`; unrelated exclusion policy не меняется;
- current complete traces use the same evidence contract and `traceKind`;
- no override/shared slot conflict;
- no global side effect;
- source unchanged.

Malformed, non-additive, policy-changing, unbound или неоднозначная пара является contract/infrastructure failure, а не тихим `NotApplicable`.

Oracle не завершает evaluation на первом симптоме. Он детерминированно агрегирует:

```text
extension-owner activated
route identity changed
activated-owner set changed
observable semantics changed
```

Exact fingerprint содержит все конкретные observed dimensions; class fingerprint сохраняет категории без concrete values. Activation-only и activation-plus-semantic-interference обязаны иметь разные exact fingerprints, чтобы replay/reduction не подменял исходный механизм более слабым.

---

## O-006. Controlled fallback

### Проверяет

Fallback разрешён только для classified unsupported shape, но не для optimizer defect/internal exception.

### Cases

- `Disabled` — SSA не запрашивается;
- `Prefer` + classified unsupported — AIR fallback разрешён;
- `Prefer` + pass defect — failure;
- `Require` + unsupported — failure;
- `Debug` — detailed evidence, no silent masking.

### Seeded fault

Catch-all exception превращается в successful AIR fallback.

---

## O-007. Session/runtime-state isolation

### Проверяет

Разные sessions одного artifact и разные runtime instances не разделяют mutable local/input state.

### Schedule

```text
create artifact A
create session S1(args1)
create session S2(args2)
run S1
run S2
run S1 again
```

Expected: результат S1 стабилен.

### Seeded fault

Global/static dictionary хранит current arguments.

---

## O-008. Route conformance

### Проверяет

Фактически executed components соответствуют planned route и selected executor identity.

### Сравнение

- planned stage IDs;
- actual stage events;
- executor ID;
- backend ID;
- entry/output artifact contracts.

### Seeded fault

Runtime silently chooses another executor with compatible type but wrong contribution ID.

---

## O-009. Canonical lock consistency

### Проверяет

Повторное compile одного definition/registry создаёт byte-identical canonical lock и plan hash.

Проверяет также exact package manifest hash binding.

---

## O-010. Diagnostic determinism

Equivalent failure case даёт одинаковые:

- stage;
- stable code;
- category;
- span;
- normalized hints.

Raw stack trace не входит в semantic equality.

---

## O-011. Resource-limit consistency

Equivalent variants одинаково применяют semantic resource limits:

- source length;
- parameter count;
- node count;
- execution step budget, если доступен.

Не утверждает process isolation.

---

## O-012. Worker robustness

Проверяет:

- timeout contained;
- malformed response rejected;
- missing response classified as infrastructure;
- crash не считается program failure;
- partial JSON не считается observation;
- process tree killed.

---

# 19. Oracle applicability

## 19.1. Проблема

Большинство ложных positives в metamorphic testing возникает, когда relation объявлена слишком широко.

## 19.2. Applicability facts

Примеры:

```text
UsesOnlySharedBackendFeatures
HasNoUnknownEffects
SsaShapeSupported
NoFloatingUndefinedPolicy
IndependentExtension
NegativeSurfaceTraceComplete
BoundedExecution
CanonicalFailureComparable
```

## 19.3. Правило

```text
applicable = RequiredFacts(oracle) ⊆ Facts(case, variants)
```

Если facts отсутствуют:

- `NotApplicable`, если relation не обещана;
- `Inconclusive`, если evidence неполно;
- `InfrastructureFailure`, если required observation потеряна;
- `Violated`, если testcase explicitly обещает relation, а вариант не исполнил contract.

`NotApplicable` не засчитывается как pass в coverage metrics.

---

# 20. Execution schedules

## 20.1. Минимальные schedules

1. compile once, run once;
2. compile once, invoke repeatedly;
3. one artifact, two sessions;
4. interleaved sessions;
5. two runtimes from one definition;
6. dispose session then run;
7. dispose one runtime while other remains;
8. failed prepare after successful prepare;
9. synchronous and asynchronous disposal;
10. bounded concurrent invocations.

## 20.2. Schedule model

```csharp
public sealed record ExecutionSchedule(
    IReadOnlyList<ScheduleStep> Steps,
    SchedulePolicy Policy);

public abstract record ScheduleStep;
public sealed record CompileStep(string ArtifactName) : ScheduleStep;
public sealed record CreateSessionStep(string Artifact, string Session, InputDocument Inputs) : ScheduleStep;
public sealed record RunStep(string Session, string ResultSlot) : ScheduleStep;
public sealed record DisposeStep(string Resource) : ScheduleStep;
public sealed record AwaitStep(string Operation) : ScheduleStep;
```

## 20.3. Determinism

Concurrency tests используют explicit barriers, latches и schedules. Нельзя считать `Thread.Sleep` достаточным способом воспроизведения race.

---

# 21. Failure taxonomy

```text
ProgramRejected
PlanRejected
RouteUnavailable
BackendUnsupported
ClassifiedFallback
OptimizerFailure
VerifierFailure
RuntimeFailure
Timeout
Crash
InfrastructureFailure
OracleViolation
Inconclusive
```

Каждый failure содержит:

- stage;
- category;
- stable code, если доступен;
- variant ID;
- adapter ID/version;
- bounded message;
- process exit classification;
- expected/unexpected marker.

Запрещено объединять:

- compiler error и worker crash;
- timeout и slow success;
- expected rejection и infrastructure failure;
- unsupported route и silent fallback;
- seeded fault и real defect.

---

# 22. Finding model

## 22.1. Candidate finding

Создаётся, когда oracle возвращает `Violated`.

Содержит:

- case ID;
- oracle ID/version;
- subjects;
- normalized diff;
- plan/route fingerprints;
- seeded mutation ID;
- environment identity;
- replay state.

## 22.2. Confirmation

Default confirmation:

- 3 fresh-process strict replays;
- одинаковый semantic fingerprint;
- no infrastructure failure;
- no environment drift.

Statuses:

```text
Candidate
Confirmed
Flaky
InfrastructureBlocked
FalsePositive
KnownDefect
SeededFault
Fixed
```

## 22.3. Fingerprint

```text
SHA256(
  oracle ID/version
  + normalized failure/value difference
  + relevant route identity
  + stage/category/code
)
```

Не включать:

- random temp path;
- timestamp;
- PID;
- full raw stack trace;
- absolute source path;
- elapsed time.

## 22.4. Deduplication

Группировать по fingerprint, но сохранять:

- first case;
- smallest case;
- count;
- seeds;
- environments;
- all raw observations.

---

# 23. Reducer

## 23.1. Цель

Минимизировать:

```text
(program, plan delta, variants, schedule, inputs)
```

при сохранении одного target fingerprint.

## 23.2. Complexity metric

Лексикографически:

1. program node count;
2. plan mutation count;
3. schedule step count;
4. variant count;
5. input complexity;
6. source length.

## 23.3. Reduction order

1. Удалить non-subject variants.
2. Удалить oracle-irrelevant schedule steps.
3. Удалить unused plan deltas/features.
4. Упростить program tree.
5. Упростить constants/inputs.
6. Уменьшить resource limits до минимально достаточных.

## 23.4. Acceptance

Candidate reduction принимается, если:

- schema valid;
- adapter renders it;
- required relation remains applicable;
- same target fingerprint воспроизводится M/N раз;
- no new infrastructure failure;
- complexity strictly decreases.

## 23.5. Reducer cache

Key:

```text
SHA256(canonical candidate + environment identity + oracle version)
```

Value:

```text
SameFingerprint | Different | Invalid | InfrastructureFailure
```

Infrastructure failure не кешируется как semantic rejection.

## 23.6. Reduction history

Сохранять:

- candidate hash;
- mutation;
- complexity before/after;
- replay result;
- accepted/rejected;
- timestamp only as metadata, not semantic identity.

---

# 24. Seeded fault suite

## 24.1. Требования

- fault code находится только в test fixtures/test-owned adapter wrapper;
- disabled by default;
- имеет stable ID;
- не попадает в production package;
- не меняет unrelated variants;
- expected oracle/fingerprint documented.

## 24.2. Mandatory faults

| ID | Fault | Expected oracle |
|---|---|---|
| SF-001 | Acme compiled subtract performs addition | O-001 |
| SF-002 | Registry order changes selected owner | O-003 |
| SF-003 | Optimizer changes subtraction semantics | O-002 |
| SF-004 | Prefer swallows internal optimizer exception | O-006 |
| SF-005 | Excluded provider activated | O-004 |
| SF-006 | Session state stored globally | O-007 |
| SF-007 | Runtime chooses wrong same-contract executor | O-008 |
| SF-008 | Lock serializer uses enumeration order | O-009 |
| SF-009 | Worker hangs | O-012 |
| SF-010 | Diagnostic code depends on backend | O-010 |
| SF-011 | Independent extension activates and changes behavior through a test-owned runtime provider | O-005 |

Historical Phase 3a artifacts that used `SF-002-excluded-owner-activation` and `SF-003-extension-noninterference` are superseded: those IDs conflict with this canonical table. They remain historical records only and their fingerprints must not be combined with current evidence.

Seeded faults must execute inside test-owned package/runtime components and reach observations through normal instrumentation. Direct post-execution mutation of values, traces or owner sets does not satisfy mutation-adequacy evidence. Multi-dimensional faults such as SF-011 must retain every observed violation dimension in the exact fingerprint; preserving only the first symptom is insufficient for replay/reduction identity.

## 24.3. Mutation score

```text
mutation score = detected mandatory seeded faults / enabled mandatory seeded faults
```

Каждый oracle должен иметь минимум один unit fault и один out-of-process replay fault, если применимо.

---

# 25. CLI

## 25.1. Commands

```text
planfuzz campaign
planfuzz replay
planfuzz reduce
planfuzz inspect
planfuzz adapters
planfuzz corpus add
planfuzz report
planfuzz worker execute
```

## 25.2. Campaign example

```bash
planfuzz campaign \
  --adapter acme \
  --seed 12345 \
  --cases 10000 \
  --mode strict \
  --workers 4 \
  --timeout 5s \
  --output artifacts/planfuzz/acme-12345
```

## 25.3. Replay example

```bash
planfuzz replay \
  --case findings/O-003/case.json \
  --repeat 3 \
  --strict
```

## 25.4. Reduce example

```bash
planfuzz reduce \
  --finding findings/O-001/finding.json \
  --budget 30m \
  --replay 3
```

## 25.5. Exit codes

```text
0 no confirmed finding
1 usage/config error
2 infrastructure failure
3 confirmed finding exists
4 interrupted
```

Точные codes worker и coordinator разделить; shell automation не должна parsing text logs.

---

# 26. Artifact layout

```text
artifacts/planfuzz/<campaign-id>/
  campaign.json
  environment.json
  state.json
  summary.json
  cases/
  observations/
  candidates/
  findings/
    <oracle>/<fingerprint>/
      finding.json
      original/
      minimized/
      replay/
      triage.md
  corpus/
  metrics/
  logs/
  MANIFEST.sha256
```

## 26.1. Atomicity

- write temp file;
- flush;
- atomic rename;
- update state after durable artifact;
- resume ignores incomplete temp files;
- partial attempt marked explicitly.

## 26.2. Recursive manifest

Manifest includes all files except itself and temporary files.

Before publication:

```bash
sha256sum -c MANIFEST.sha256
```

## 26.3. Environment record

- commit/archive hash;
- dirty marker;
- SDK/runtime versions;
- OS/kernel/architecture;
- CPU count/model;
- relevant env flags;
- adapter versions;
- case/oracle/mutator versions;
- command line;
- NuGet source/cache policy.

Не сохранять secrets.

---

# 27. Campaign coordinator

## 27.1. State machine

```text
Created
Generating
Executing
Confirming
Reducing
Completed
Cancelled
Failed
```

## 27.2. Resume

Campaign может продолжаться после interruption:

- canonical case files immutable;
- completed observation recognised by valid hash/schema;
- incomplete attempts rerun;
- seed/index mapping unchanged;
- duplicate worker completion idempotent;
- reducer state checkpointed.

## 27.3. Scheduling

Первая версия:

- fixed bounded worker count;
- deterministic case assignment;
- completion order не влияет на semantic report ordering;
- stdout/report sorted by case ID/index;
- no unbounded queue.

## 27.4. Budgets

Поддержать минимум:

- max cases;
- wall-clock campaign budget;
- per-worker timeout;
- confirmation attempts;
- reducer budget;
- max findings per fingerprint/oracle;
- max artifact bytes.

---

# 28. Метрики

## 28.1. Correctness/reliability

- generated cases;
- valid/rejected;
- completed/timed out/crashed;
- applicable/not-applicable oracles;
- candidate/confirmed/flaky findings;
- false positives;
- infrastructure failures;
- seeded mutation score;
- real defects by root-cause class.

## 28.2. Semantic coverage

Не ограничиваться line coverage.

- language features used;
- feature pairs/triples;
- plan mutations;
- backend pairs;
- optimizer/SSA policies;
- failure stages;
- fallback classes;
- lifecycle schedules;
- route shapes;
- capability presence/absence;
- callable effect/trust categories.

## 28.3. Cost

- generation time;
- plan compilation;
- worker startup;
- runtime creation;
- execution;
- oracle evaluation;
- replay;
- reduction;
- artifact I/O;
- peak memory where measurable.

## 28.4. Reduction

- original/minimized nodes;
- plan deltas;
- schedule steps;
- source bytes;
- reduction time;
- attempts;
- reproduction rate.

---

# 29. Research experiment protocol

## 29.1. Modes

### B0 — Existing tests

Контрольная характеристика текущего suite, без искусственного пересчёта на case budget.

### B1 — Program-only generation

- fixed plan;
- fixed backend pair;
- no plan mutation;
- no lifecycle schedules beyond run once.

### B2 — Pairwise plan enumeration

- generated programs;
- deterministic pairwise plan/backend combinations;
- no feedback/adaptive weighting;
- no lifecycle dimension.

### PF — Full PlanFuzz

- program generation;
- plan mutations;
- route/backend variants;
- lifecycle schedules;
- full oracle set.

## 29.2. Equal budget

Primary comparison по wall-clock.

Secondary:

- execution count;
- CPU time;
- unique program count;
- plan count.

## 29.3. Repetitions

Не менее пяти campaign seeds для research-grade comparison. Confidence intervals/bootstrap для time-to-first-defect и defect count, если данных достаточно.

## 29.4. Ablations

- PF minus plan mutation;
- PF minus lifecycle;
- PF minus negative-surface oracle;
- PF minus reducer;
- PF without strict confirmation for cost only.

## 29.5. Counting defects

Считать уникальный root cause, а не каждую программу.

Таблица:

```text
Defect ID
Root-cause class
Affected layer
Found by mode
Needs plan mutation?
Needs lifecycle?
Oracle
Seeded/real
Fixed?
Regression test?
```

## 29.6. Stop rules

Pilot stage прекращается/перепланируется, если:

- false-positive rate >10%;
- infrastructure failure >2%;
- same-case replay <95% для deterministic profiles;
- reducer меняет fingerprint;
- clean baseline содержит необъяснённые findings;
- manifest/replay не работает на clean environment.

---

# 30. Testing strategy

## 30.1. Unit

- PRNG golden values;
- canonical JSON;
- ID hashing;
- DTO immutability;
- adapter renderer;
- value normalization;
- oracle applicability;
- each oracle pass/fail/NA;
- fingerprint stability;
- reducer monotonicity;
- manifest generation.

## 30.2. Integration

- coordinator-worker protocol;
- timeout + kill-tree;
- crash/malformed response;
- Acme interpreter/compiled parity;
- Wist interpreter/CIL parity;
- package order determinism;
- lock consistency;
- session isolation;
- route conformance;
- three-attempt confirmation;
- CLI exit codes.

## 30.3. Architecture guards

- Core has no Wist dependency;
- Core has no concrete backend dependency;
- adapters do not leak into production facade;
- no unbounded process output;
- no raw exception-message-only comparison;
- no `System.Random` in generation;
- no source regex parser in core;
- no PlanFuzz package in Wist NuGet closure;
- no reflection scan outside selected packages.

## 30.4. Seeded fault adequacy

- every mandatory oracle detects corresponding seeded fault;
- no unrelated oracle required for detection;
- clean control remains pass;
- fault disabled by default;
- out-of-process replay stable.

---

# 31. CI integration

## 31.1. Pull request gate

Не запускать большой fuzz campaign на каждый PR.

PR gate:

- PlanFuzz unit tests;
- integration smoke 20–100 deterministic cases;
- seeded fault focused tests;
- canonical serialization golden tests;
- process timeout smoke;
- architecture guards.

## 31.2. Scheduled campaign

Nightly/weekly:

- fixed-seed regression corpus;
- rotating-seed exploration;
- strict workers;
- artifact upload;
- no automatic bug classification without triage.

## 31.3. Release gate

PlanFuzz не является release blocker для Wist alpha до отдельного решения. Однако подтверждённый semantic defect, затрагивающий shipped path, обязан стать release blocker согласно severity policy.

---

# 32. Robustness and safety

- Worker input path must be confined to campaign root or explicitly allowed path.
- No dynamic arbitrary assembly loading from testcase.
- Adapter IDs resolve only from built-in registry in MVP.
- Case source/trace size bounded.
- Stdout/stderr bounded with truncation marker.
- Worker timeout configurable, default finite.
- Kill entire process tree.
- Atomic writes for case/observation/state.
- Partial observation is marked incomplete, never parsed as success.
- Generated loops bounded unless dedicated timeout campaign.
- OOM cases limited in default profile.
- Raw source and exception messages may contain sensitive data for future external adapters; report redaction policy must be extensible.

---

# 33. Implementation phases

## Phase 0 — Baseline lock and design skeleton

Deliverables:

- project files;
- case schema v1;
- adapter interface;
- PRNG v1;
- canonical serializer;
- architecture docs;
- focused tests.

Acceptance:

- existing repo untouched semantically;
- same seed yields same case JSON;
- Core has no Wist dependency;
- all projects build.

## Phase 1 — Acme vertical slice

Deliverables:

- Acme generator;
- two backends;
- O-001, O-003, O-009;
- strict worker;
- CLI campaign/replay;
- basic finding output.

Acceptance:

- 10 000 valid cases complete;
- no unexplained clean-baseline findings;
- wrong arithmetic seeded fault detected and replayed 3/3;
- order-dependent plan seeded fault detected;
- worker timeout contained.

## Phase 2 — Wist arithmetic and SSA matrix

Deliverables:

- Wist Level 0 model/renderer;
- interpreter/CIL variants;
- SSA Disabled/Prefer/Require variants;
- O-002 and O-006;
- structured Wist diagnostic normalization.

Acceptance:

- 5 000 Wist cases;
- no string-only value comparison;
- `Require` and `Prefer` policies classified correctly;
- optimization miscompile seeded fault detected;
- silent fallback seeded fault detected.

## Phase 3 — Lifecycle and negative surface

Implemented Phase 3a slice: schema-v4 fail-closed observed surface/owner evidence plus hardened O-004/O-005. Lifecycle/session/concurrency schedules, O-007/O-008 and disposal acceptance remain incomplete; therefore Phase 3 as a whole is not complete.

Deliverables:

- execution schedules;
- runtime trace decorators;
- O-004, O-005, O-007, O-008;
- lifecycle seeded faults.

Acceptance:

- two-runtime isolation cases pass clean baseline;
- excluded activation fault detected;
- wrong route/executor identity detected;
- disposal scenarios deterministic.

## Phase 4 — Reducer and corpus

Deliverables:

- program/plan/schedule reducers;
- finding deduplication;
- corpus store;
- regression seed promotion.

Acceptance:

- every mandatory seeded fault reduced;
- reduced case reproduces fingerprint 3/3;
- no accepted reduction enlarges defined complexity metric;
- reduction history replayable.

## Phase 5 — Research campaigns

Deliverables:

- B1/B2/PF modes;
- semantic coverage;
- ablations;
- raw reports;
- triage workflow.

Acceptance:

- equal-budget comparison complete;
- all findings classified;
- real/seeded/false-positive counts separate;
- raw artifacts and environment manifest preserved.

## Phase 6 — External validation

Optional but strongly recommended:

- third independent adapter;
- external maintainer reproduction;
- clean-machine artifact run;
- artifact evaluation package.

---

# 34. Definition of Done

PlanFuzz MVP считается реализованным только если одновременно выполнено:

1. Core не содержит Wist-specific hardcode.
2. Case generation deterministic и versioned.
3. Strict out-of-process execution работает.
4. Acme и Wist adapters работают через один core path.
5. Минимум семь обязательных oracles реализованы:
   - backend parity;
   - optimization parity;
   - plan determinism;
   - negative surface;
   - controlled fallback;
   - state isolation;
   - route conformance.
6. Mandatory seeded fault suite проходит.
7. Finding replay и reduction воспроизводимы.
8. Clean baseline campaigns не содержат необъяснённых false positives.
9. Existing relevant test suite остаётся green.
10. Документация и CLI examples проходят smoke checks.
11. Campaign artifact имеет recursive manifest.
12. Research report отделяет:
    - actual defects;
    - seeded faults;
    - flaky outcomes;
    - infrastructure failures;
    - inconclusive cases.

Научная статья не считается доказанной только фактом реализации MVP. Для publication-ready claim дополнительно требуются реальные подтверждённые findings и controlled comparison с baselines.

---

# 35. Риски и replan triggers

## R1. Инструмент не находит реальных дефектов

**Trigger:** после Acme + Wist pilot найдены только seeded faults и known regressions.

**Action:**

- усилить plan/lifecycle mutations;
- добавить broader Wist surface;
- добавить third-party adapter;
- сузить research claim до negative-surface or route-conformance testing;
- не писать, что техника превосходит baseline без данных.

## R2. Слишком много ложных расхождений

**Trigger:** >10% candidates после triage оказываются oracle/model errors.

**Action:**

- остановить большой campaign;
- усилить applicability facts;
- улучшить typed snapshots;
- разделить valid/negative profiles;
- добавить oracle-focused tests.

## R3. Worker startup доминирует cost

**Trigger:** >70% времени strict campaign уходит на process startup.

**Action:**

- сохранить strict confirmation;
- добавить bounded batch workers для exploration;
- перезапускать worker после N cases;
- findings подтверждать только fresh process.

## R4. Wist generator становится вторым parser

**Trigger:** generator/reducer анализирует raw source regex’ами.

**Action:**

- остановить extension;
- вернуть structured model + renderer;
- parser ownership остаётся у Wist.

## R5. Research tool загрязняет production packages

**Trigger:** Wist package closure или public API изменяются без необходимости.

**Action:**

- вынести instrumentation в decorators/adapters;
- исключить PlanFuzz projects из package matrix;
- public observer добавлять только с independent non-Wist use case.

## R6. Novelty boundary оказывается слабее ожидаемой

**Trigger:** literature review показывает близкий configuration-aware compiler fuzzer.

**Action:**

- сравнить exact dimensions/oracles;
- сфокусироваться на negative surface, executable language plans, lifecycle или route conformance;
- сменить claim с algorithmic novelty на new empirical defect class/tool evaluation.

---

# 36. Открытые решения, которые нужно зафиксировать до кодирования Phase 2

1. Остаётся ли PlanFuzz internal research tool или планируется отдельный NuGet package?
2. Нужен ли generic production observer contract или достаточно adapter decorators?
3. Какой Wist numeric semantic profile является canonical для exact comparison?
4. Какие SSA unsupported diagnostics считаются classified fallback?
5. Какие Wist features входят в shared interpreter/CIL subset для первого experiment?
6. Нужен ли reverse loader для schema-v5 language lock или достаточно canonical serialization checks?
7. Как хранить full source в artifacts при внешних adapters?
8. Какой third independent language/system будет использоваться для external validation?
9. Какой final equal-budget protocol фиксируется до publication run?

Эти вопросы не блокируют Phase 0–1. Пункты 2–5 блокируют окончательный Wist oracle contract.

---

# 37. Рекомендуемый первый vertical slice

Первый mergeable milestone должен быть намеренно узким:

```text
Acme structured generator
+ registry order mutation
+ interpreter/compiled variants
+ typed decimal snapshot
+ backend parity oracle
+ plan determinism oracle
+ fresh worker per case
+ finding replay
+ wrong-arithmetic seeded fault
```

Не начинать одновременно с Wist loops, concurrency, reducer и coverage guidance. Сначала необходимо доказать полный путь:

```text
generate
-> serialize
-> isolated execute
-> observe
-> compare
-> confirm
-> preserve finding artifact
```

После этого добавлять Wist и новые dimensions по одному, сохраняя baseline comparison.

---

# 38. Итоговый комплект поставки

```text
Source code
CLI executable
Acme adapter
Wist adapter
Seeded fault fixtures
Unit/integration/architecture tests
Deterministic seed corpus
Campaign artifacts
Reducer
Research protocol
Raw CSV/JSON results
Markdown report
Reproduction scripts
Recursive SHA-256 manifest
```

Минимальный acceptance command set должен быть задокументирован после реализации, например:

```bash
./build.sh --skip-docs

dotnet test UniversalToolchain/UniversalToolchain.PlanFuzz.Tests/UniversalToolchain.PlanFuzz.Tests.csproj

dotnet test UniversalToolchain/UniversalToolchain.PlanFuzz.IntegrationTests/UniversalToolchain.PlanFuzz.IntegrationTests.csproj

dotnet run --project UniversalToolchain/UniversalToolchain.PlanFuzz.Cli -- \
  campaign --adapter acme --seed 1 --cases 10000 --mode strict

dotnet run --project UniversalToolchain/UniversalToolchain.PlanFuzz.Cli -- \
  campaign --adapter wist --profile restricted-arithmetic --seed 1 --cases 5000 --mode strict
```

Точные paths и команды должны быть синхронизированы с фактическими project names и canonical test matrix при реализации.
