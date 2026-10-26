# Первый численный CPU/GPU-срез: протокол принятия решения

**Статус: proposal, 2026-10-26; численный аппаратный эксперимент ещё не выполнен.** Обоснование — [README](README.md). Имена ролей ниже обозначают проектные обязанности, а не готовые UT API.

## Проверяемая гипотеза

Для выбранного класса численных программ UT позволяет добавлять независимые предметные операции и варианты исполнения с меньшей полной стоимостью интеграции, сохраняя заданную семантику и приемлемые расходы, чем прямой frontend к тому же компиляторному backend.

Гипотеза не состоит в том, что MLIR не умеет расширяемые анализы, что C#-интерфейсы не позволяют независимые providers или что всякий GPU kernel быстро работает на CPU. Общий semantic evidence engine в treatment не требуется. Если получится удобный небольшой adapter без него, это допустимый успешный итог.

## G0. Зафиксировать предметную задачу и baseline

Первый gate выбирает **одну** workload family с реальным предполагаемым потребителем. До этого варианты ниже — кандидаты, не подтверждённый product-market fit. Не начинать одновременно три языка и три аппаратных backend.

| Кандидат | Какую неопределённость снимает | Сильный baseline |
|---|---|---|
| Пакетная численная формула: map → несколько elementwise stages → reduction | Сохраняет связь с текущим .NET продуктом; проверяет fusion, residency и small-input CPU fallback | Обычный C#/SIMD pipeline и direct lowering в тот же IREE target |
| `activation(matmul(A,B)+bias)` | Проверяет dtype/accumulation, shape/layout, library-vs-fused выбор и тензорные инструкции | Оптимизированная библиотека устройства и direct StableHLO/MLIR→тот же IREE/XLA stack |
| Двухэтапный stencil/image pipeline | Проверяет inter-stage reuse, tiling и recomputation-vs-storage | Halide при одинаковых boundary conditions и output semantics |

Предпочтение для первого опыта — пакетная формула, если есть конкретная .NET-нагрузка. При реально матричной задаче выбрать второй вариант сразу; не вставлять искусственную формулу только ради связи с Wist. Эффективный GPU вариант для маленькой или низкоинтенсивной задачи не предполагается заранее.

До реализации зафиксировать: corpus, распределение размеров, типы, разрешённую ошибку, требования reproducibility, lifetime входов, memory budget, cold/warm режим и основной критерий успеха. Разделить CPU↔GPU переносимость и будущую TPU/NPU переносимость.

## G1. Исполняемая семантика и честный reference

1. Описать программу структурированными domain records/IR через существующую owned syntax/AST pipeline. Не распознавать синтаксис regex в optimizer, facade или backend.
2. Реализовать понятный reference evaluator/CPU baseline для исходного semantic graph до преобразований; он не использует rewrite, schedule или effect-approval logic treatment. Общий parser сам по себе допустим, но source-to-IR ошибки дополнительно проверяются hand-constructed semantic cases.
3. Уточнить semantically observable результат: values, traps/errors, effects, порядок необходимых действий, mutable buffers и completion/lifetime.
4. Поддержать только объявленный subset. Opaque host callbacks, неподдерживаемая рекурсия, неизвестные effects и неразрешённая FP-relaxation получают явный отказ либо объявленный fallback.

Для floating point заранее выбрать, что сравнивается: exact bits там, где контракт их требует; ULP/absolute/relative error в остальных разрешённых областях; отдельные правила NaN/infinity/signed-zero. Допуск не выбирается после просмотра провалившихся тестов. Для редукции reference должен соответствовать объявленной математической/численной модели: иной reduction tree нельзя принять только потому, что результат «похож».

G1 завершён, когда положительные и отрицательные случаи исполняются с независимым ожидаемым результатом, а смысл каждой будущей оптимизации определён. Строка shape metadata или флаг `CanOptimize` не засчитывается как численное выполнение.

## G2. Сначала прямой backend, затем UT adapter

### Сравниваемые варианты

| ID | Реализация | Что измеряет |
|---|---|---|
| A | Прямой минимальный domain frontend → выбранный MLIR/IREE backend | Стоимость обычной реализации без UT |
| B | Тот же domain frontend/semantics → UT typed artifacts/selected route → тот же backend | Дополнительную цену и ценность UT composition |
| C | C#/SIMD или оптимизированная библиотека устройства | Практическую цену выбранной компиляционной стратегии |
| D | Опциональный shared evidence/query service поверх B | Добавляется только после зафиксированного недостатка обычных интерфейсов |

A и B используют одинаковые backend build/options, числовую политику, workload и доступные оптимизации. Не сравнивать UT с наивной реализацией, если direct baseline может переиспользовать тот же compiler. Не включать D только ради заранее выбранного победителя.

Первым bridge допустимо сделать экспорт ограниченного, верифицируемого MLIR/StableHLO артефакта с вызовом отдельного compiler tool: это уменьшает раннюю зависимость от нестабильного native C++ ABI. Нужны version pin, diagnostics, cancellation, artifact hashes и настоящая проверка артефакта compiler/verifier. Для A/B обе стороны должны платить одинаковую процессную/serialization цену; последующее native embedding — отдельное сравнение. Конкретный формат выбирается по семантике workload: StableHLO подходит не для любого imperative DSL.

Численный CPU backend и один доступный GPU target должны реально исполнить один семантический corpus. Vulkan/Intel GPU может быть кандидатом для доступного Fedora окружения, но наличие графического устройства ещё не доказывает driver/runtime compatibility. Перед реализацией GPU route выполнить capability probe: enumeration устройства, API/driver/compiler versions и features, затем минимальный kernel с проверенным численным результатом. Нельзя молча заменить недоступный hardware run эмуляцией и объявить gate пройденным.

TPU/NPU — отдельный target gate: назвать устройство, поддерживаемый runtime и версии, dtype/shape subset, способ загрузки, аппаратные ограничения и oracle. CPU interpretation, успешный export и compilation-only acceptance отмечаются отдельно от device execution.

## G3. Оптимизация полного графа

Для первого опыта достаточно двух конкурирующих планов: понятного conservative baseline и одного законного fusion/layout/schedule варианта. Сначала доказать полезность правильного уровня IR и измерения полного пути; большой autotuner не обязателен.

Проверить случаи, где решения расходятся:

- маленький input: launch/transfer costs могут оставить задачу на CPU;
- большой input при resident buffers: GPU может выигрывать без повторной передачи;
- две операции используют один промежуточный результат: fusion может дублировать вычисление;
- layout conversion делает быстрый отдельный kernel невыгодным для pipeline;
- fused epilogue конкурирует с vendor library;
- dynamic/tail shapes не совпадают с удобными tile multiples;
- memory cap запрещает иначе быстрый план;
- stricter numeric mode исключает привлекательную reassociation/tensor instruction.

Legality — жёсткий фильтр, profitability — последующий выбор. Cost model не имеет права «компенсировать» неверную программу ускорением. В метаданных результата различать estimated cost, measured cost и проверенную семантическую допустимость.

Замеры включают planning/materialization, compilation, tuning, first call, warm call, transfers, launch, explicit synchronization, peak host/device memory и code/artifact size. Для overlap показывать critical path и wall time. Device-only timer публиковать только вместе с end-to-end. Указывать число повторов, dispersion/confidence intervals, прогрев и резидентность данных.

Для повторяющихся запусков отдельно считать amortization: дополнительная цена компиляции оправдывается только при достаточном количестве вызовов и положительной экономии каждого вызова. Нулевой или отрицательный warm gain не превращается в выигрыш при увеличении числа запусков.

## G4. Проверка расширяемости, которая различает варианты

Заморозить old consumer/provider binaries и source hashes. Затем независимый участник добавляет:

1. одну предметную операцию или более точный analysis с заранее объявленным query contract;
2. один schedule/lowering variant для уже разрешённого target;
3. при наличии ресурсов — второй consumer того же analysis.

Фиксировать не только consumer diff: все schema/interface changes, adapters, registrations, glue, source generation, verifier/tests, documentation и migration work. Нулевые изменения consumer не означают нулевую интеграцию. Если внешнего автора нет, честно назвать опыт model/author-designed, а не independently authored.

`LanguagePlan` остаётся неизменным по составу packages/providers внутри каждого frozen configuration. Две разные программы с одним configuration plan вправе получить разные physical plans. Если для нового устройства нужен новый backend package, это новая явно собранная конфигурация, а не скрытое runtime-discovery.

D допускается только при наблюдаемом повторении shared facts/revocation между независимыми consumers. Сначала typed query service и полная инвалидация snapshot; затем, при измеренной необходимости, incremental dependencies; затем restricted inference. Прирост сложности каждого шага сравнивается с direct interface control.

## Негативный corpus

| Область | Обязательные контрпримеры |
|---|---|
| Числа | Overflow, NaN/infinity, signed zero, near-cancellation, different reduction orders, accumulation dtype |
| Индексы/shape | Empty/tail/noncontiguous inputs, incompatible broadcast, dynamic shape mutation, out-of-range |
| Память | Aliasing, overlapping views, mutation после анализа, buffer reuse до device completion |
| Effects | Host callback, throwing/diverging operation, read/write ordering, unknown call |
| Параллелизм | Неправильный barrier, race, asynchronous dependency omission, недопустимый memory space |
| Provenance | Старый program revision, другой selected implementation, target/policy/layout mismatch |
| Доверие | Ложный `Pure`/`NoAlias`, неполные зависимости, неподтверждённый trust claim |
| Runtime | Unsupported target, cancellation, failed compile/load, повторный запуск и disposal |

Случаи вне выбранного semantic subset проверяют явный отказ или `NOT_APPLICABLE`; таблица не требует реализовать все перечисленные features в первом опыте.

Использовать существующий PlanFuzz с новым adapter-owned structured generator и соответствующими observations/oracles. Не создавать параллельную систему fuzzing. Совпадение двух backend, разделяющих один ошибочный lowering, не является независимым oracle; нужны hand-computed edge cases, independent reference и проверенные metamorphic relations.

## Go/no-go и остановка

Численные thresholds производительности и authoring budget фиксируются после выбора реальной задачи, **до** просмотра результатов. Здесь они намеренно не выдуманы: без SLA, workload и оборудования универсальное «ускорение не меньше X%» не является критерием продукта.

Безусловные условия отказа:

- хотя бы одно принятие неправильного safety-sensitive rewrite на объявленном corpus;
- зависимость корректности от self-asserted доверия без подходящей host policy/verifier;
- скрытая пересборка packages/providers после `LanguagePlan` freeze;
- аппаратная готовность заявляется без фактического device execution;
- unsupported inputs молча получают другую числовую семантику.

Условия сокращения архитектуры:

- A столь же корректен и воспроизводим, а суммарная стоимость расширений/поддержки ниже B — не расширять UT ради этого workload;
- B удобен, но D не улучшает наблюдаемую задачу — оставить typed interfaces без общего semantic engine;
- один consumer и отсутствие полезного повторного использования — не извлекать общий broker;
- selective invalidation сложнее/дороже полного пересчёта — оставить пересчёт;
- CPU выигрывает для нужного распределения inputs — сохранить CPU как рекомендуемый путь;
- GPU полезен только на отдельном диапазоне — оформить честную specialization/dispatch область;
- нет реального второго target — не объявлять универсальную аппаратную переносимость.

Отрицательный результат не требует выбрасывать framework: он уточняет, для каких задач композиция полезнее прямого frontend.

## Evidence receipt будущего опыта

Сохранять по каждому случаю: UT/backend/runtime revisions, compiler options, SDK/OS/hardware/driver, source и executable hashes, configuration plan hash, program revision, shape/layout/dtype, numeric policy, выбранный план, причины отказа альтернатив, observation oracle, seed, cold/warm/resident режим, timing distributions, memory, changed-source inventory и raw reproducible logs.

Результаты разных стадий маркировать отдельно: **artifact accepted**, **target executed**, **semantic corpus passed**, **performance measured**, **extension economics measured**. Успех одной стадии не заменяет остальные. Текущая сессия выполнила аудит, две регрессии и небольшой прежний C# decision control; ни один аппаратный gate этой спецификации пока не объявляется завершённым.
