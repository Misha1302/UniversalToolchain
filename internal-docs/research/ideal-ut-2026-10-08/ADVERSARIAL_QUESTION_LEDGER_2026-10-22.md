# Adversarial questions — UniversalToolchain research (2026-10-22)

> **QUESTION_LEDGER_V1 · research only.** These are falsification questions, not answers, defects, implementation tasks, scientific-novelty claims, or approval for a semantic kernel. Do not select the winning talk concept here.

**Source baseline:** [UniversalToolchain master @ `886bf241623e83566585bf2aec5b635fefe1675f`](https://github.com/Misha1302/UniversalToolchain/tree/886bf241623e83566585bf2aec5b635fefe1675f). Stage 2 must recheck HEAD. **Knowledge boundary:** [current implemented status](../../../docs/CURRENT_ARCHITECTURE_STATUS.md) outranks the [Ideal UT proposal](README.md). `LanguageCompiler/LanguagePlan/LanguageRuntime` currently own configuration selection/materialization; a general shared semantic evidence service is *proposed*, not implemented.

**Talk material limitation:** `Scientific_Open_Source_Meetup_2026.html/.pdf`, `Сценарий_9м30с.md`, `Независимая_оценка_и_валидация.md`, and `Тема_и_аннотация.md` were not accessible in stage 1. Stage 2 must not claim they were inspected.

## Procedure and statuses

- Keep **Q01–Q42 stable** forever; new questions get new IDs. Preserve history rather than rewriting earlier questions to fit outcomes.
- Every row starts `OPEN / NOT_TESTED`. After actual investigation, attach revision-bearing sources and one of `SUPPORTED / REFUTED / PARTIAL / INCONCLUSIVE / NOT_TESTED`. `SUPPORTED` is bounded evidence, not general proof.
- `CRITICAL / HIGH / MEDIUM` = capacity to change the research direction, not established defect severity.
- Assess alternatives with matched tests and budgets. In particular, unchanged consumer source code ≠ no integration/adapter/schema/registration changes.
- Treat source statements and old papers as **evidence**, not as claims of current UT behavior. Keep false-positive optimization at zero for declared observation model.

## TOP_CRITICAL_QUESTIONS

Investigate, in this order: **Q07 → Q02 → Q01 → Q09 → Q10 → Q06 → Q12 → Q15 → Q03 → Q22 → Q40 → Q42**. This ranking deliberately includes questions whose acceptable answer is **“do not build the semantic core.”**

## QUESTION_LEDGER_V1

### I. Prior art: LLVM, MLIR and simpler implementations

#### Q01 — Может ли MLIR ExternalModel обогатить старый optimizer через Op/Type Interface без изменения consumer и исходного dialect?

- **Priority / type / state:** `CRITICAL` / `COMPARISON` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Для этой интеграции обязательно требуется новый механизм.
- **Why it changes the outcome:** Может исчезнуть основное отличие UT от MLIR.
- **Required primary evidence / experiment:** Изолированные producer/consumer библиотеки, external registration, IR до/после и diff consumer.
- **Potential effect:** Изменить тезис.

#### Q02 — Изменяет ли подключение независимого LLVM Alias Analysis через AAManager поведение неизменного LICM/GVN-подобного consumer?

- **Priority / type / state:** `CRITICAL` / `COMPARISON` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Старый optimizer нельзя улучшить новым анализом в LLVM.
- **Why it changes the outcome:** Прямой prior-art контрпример центральному N+1 примеру.
- **Required primary evidence / experiment:** AA pipeline variants, alias-query traces, LLVM IR, замороженный исходник pass.
- **Potential effect:** Удалить claim новизны.

#### Q03 — Могут ли MLIR DataFlowAnalysis и общий AnalysisState вывести SafeIndex из Range и Shape без собственного rule engine?

- **Priority / type / state:** `CRITICAL` / `COMPARISON` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Семантическое комбинирование требует нового inference kernel.
- **Why it changes the outcome:** Может оказаться достаточно DataFlowSolver.
- **Required primary evidence / experiment:** MLIR prototype с несколькими анализами, dependency graph, fixpoint/Unknown/cost.
- **Potential effect:** Отказаться от inference kernel.

#### Q04 — Что из предполагаемого calculus фактов, зависимостей и выводов уже реализует LLVM Attributor?

- **Priority / type / state:** `HIGH` / `COMPARISON` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** LLVM Attributor не сопоставим с данной задачей.
- **Why it changes the outcome:** Может потребоваться более сильный исходный конкурент.
- **Required primary evidence / experiment:** Source AbstractAttribute/IRPosition/Attributor, работающий multi-attribute case.
- **Potential effect:** Изменить сравнение.

#### Q05 — Какие конкретные сценарии invalidation/reuse нельзя покрыть AnalysisManager и PreservedAnalyses LLVM/MLIR?

- **Priority / type / state:** `HIGH` / `COMPARISON` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Обычная инвалидация нуждается в новой универсальной реализации.
- **Why it changes the outcome:** Может остаться только небольшой новый scope-dependent слой.
- **Required primary evidence / experiment:** Идентичные edit/query traces и анализы с зависимостями в обоих подходах.
- **Potential effect:** Сузить механизм.

#### Q06 — Могут ли MLIR One-Shot Bufferization, AnalysisState и BufferizableOpInterface реализовать смену анализа без переписывания преобразования?

- **Priority / type / state:** `CRITICAL` / `COMPARISON` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** MLIR не имеет близкого production-механизма раздельных анализов и consumers.
- **Why it changes the outcome:** Сильный конкурент может разрушить пример выступления.
- **Required primary evidence / experiment:** Two AnalysisState, ExternalModel, same transform, observable copy/allocation diff.
- **Potential effect:** Изменить пример.

#### Q07 — Могут ли обычные C# interfaces, DI и explicit adapters пройти тот же Range+Shape→Bounds corpus проще evidence service?

- **Priority / type / state:** `CRITICAL` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Без отдельного semantic core N+1 не реализуется корректно и независимо.
- **Why it changes the outcome:** Равный успех более простой альтернативы отвергает архитектурную необходимость.
- **Required primary evidence / experiment:** Три independently built baselines, identical tests/oracle, glue LOC, correctness, overhead.
- **Potential effect:** Отказаться от semantic core.

#### Q08 — Даёт ли разнородность AST/Bytecode/AIR/SSA задачу semantic transport, не решаемую столь же просто MLIR multi-dialect conversion?

- **Priority / type / state:** `HIGH` / `COMPARISON` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Несколько разных IR автоматически означают новую проблему.
- **Why it changes the outcome:** Может потребоваться иная граница сравнения.
- **Required primary evidence / experiment:** Два real lowering cases, checked mapping witnesses, costs.
- **Potential effect:** Изменить постановку.

### II. Independent analyses and semantic safety

#### Q09 — Какие исходники, adapters, registries, schema и composition root реально меняются при добавлении нового независимого анализа?

- **Priority / type / state:** `CRITICAL` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Нулевые изменения consumer означают нулевую интеграцию.
- **Why it changes the outcome:** Раскрывает реальные издержки и ограничения независимости.
- **Required primary evidence / experiment:** Frozen assemblies, separate diffs, registration trace, результат optimizer.
- **Potential effect:** Изменить центральный тезис.

#### Q10 — Кто использует новую категорию факта, если старый optimizer даже не знает о SafeIndex и не имеет общей точки запроса?

- **Priority / type / state:** `CRITICAL` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Новое знание автоматически создаёт новое поведение старого кода.
- **Why it changes the outcome:** Может потребоваться заранее предусмотренный hook или glue.
- **Required primary evidence / experiment:** Consumer с hook и без; provider-only additions и код связывания.
- **Potential effect:** Удалить автоматизм.

#### Q11 — Как версионные shared semantic contracts переживают обновление provider без пересборки consumer?

- **Priority / type / state:** `HIGH` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Typed shared API устраняет coupling навсегда.
- **Why it changes the outcome:** Общий schema/ABI может стать центральным источником связности.
- **Required primary evidence / experiment:** Consumer/provider/schema version matrix, migrations, binary compatibility tests.
- **Potential effect:** Изменить ABI.

#### Q12 — Достаточны ли Range и Shape для безопасного удаления Read(a,i) bounds check при aliasing, mutable length, path, exceptions и overflow?

- **Priority / type / state:** `CRITICAL` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Пара сведений сама доказывает безопасность оптимизации.
- **Why it changes the outcome:** Основной опыт может давать false-safe.
- **Required primary evidence / experiment:** Real program+IR, independent execution oracle, mutation/path/overflow cases.
- **Potential effect:** Удалить safety claim.

#### Q13 — Как запретить использование CFG-local i<len(a) за пределами доказанного пути, петли и program point?

- **Priority / type / state:** `HIGH` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Value ID и revision фиксируют полный scope факта.
- **Why it changes the outcome:** Unsafe hoisting факта может дать неверную оптимизацию.
- **Required primary evidence / experiment:** CFG/path/dominance fixtures, loops, effectful calls.
- **Potential effect:** Изменить scope.

#### Q14 — Кто удостоверяет сохранение фактов между анализом, промежуточным transform A и optimizer B?

- **Priority / type / state:** `HIGH` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Декларации preserves/invalidates всегда правдивы.
- **Why it changes the outcome:** Риск устаревших доказательств после преобразований.
- **Required primary evidence / experiment:** Analysis→A→B, independent postcondition checks, stale-fact negative tests.
- **Potential effect:** Изменить lifecycle.

#### Q15 — Как предотвратить опасную оптимизацию от заведомо ложного факта стороннего provider (Pure/NoAlias/SafeIndex)?

- **Priority / type / state:** `CRITICAL` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Типизированная декларация эквивалентна доказанному семантическому закону.
- **Why it changes the outcome:** Иначе движок корректно комбинирует ложные посылки.
- **Required primary evidence / experiment:** Malicious/buggy providers, trust boundaries, independent verifier, negative oracle.
- **Potential effect:** Отказаться от автоматического доверия.

#### Q16 — Нужна ли четырёхзначная Proven/Disproven/Unknown/Contradiction модель против простого conservative lattice и failure?

- **Priority / type / state:** `HIGH` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Четыре состояния обязательны в общем API.
- **Why it changes the outcome:** Можно сократить общую сложность без потери решений.
- **Required primary evidence / experiment:** Truth tables, composition algebra, consumer-visible behaviors.
- **Potential effect:** Упростить модель.

#### Q17 — Как доказать DependencySetComplete, если часть зависимостей определяется неизвестными независимыми модулями?

- **Priority / type / state:** `HIGH` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Отсутствие отрицательного факта гарантирует полноту зависимостей.
- **Why it changes the outcome:** Open-world inference иначе небезопасен.
- **Required primary evidence / experiment:** Incomplete graph, missing providers, explicit completeness certificates.
- **Potential effect:** Изменить правила.

#### Q18 — Сохраняются ли факты при замене DLL/backend/native dependencies, когда PlanHash не изменился?

- **Priority / type / state:** `HIGH` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Hash конфигурации подтверждает идентичность executable world.
- **Why it changes the outcome:** Ложная identity привязка разрешит устаревший proof.
- **Required primary evidence / experiment:** Swap executable SHA, constant plan, platform and dependency variations.
- **Potential effect:** Изменить provenance.

#### Q19 — Реально ли отзывать только нужные derived facts без false-preserve и без почти полного recomputation?

- **Priority / type / state:** `HIGH` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Selective invalidation и эффективность совместимы при произвольных edits.
- **Why it changes the outcome:** Либо некорректность, либо неприемлемые накладные расходы.
- **Required primary evidence / experiment:** Edit traces, dependency DAG, full recompute reference, perf and safety measures.
- **Potential effect:** Отказаться от selective invalidation.

#### Q20 — Независим ли inference result от порядка регистрации providers, scheduling, рекурсии и исчерпания budget?

- **Priority / type / state:** `HIGH` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Typed rules автоматически детерминированны и завершаются.
- **Why it changes the outcome:** Результат может зависеть от нерелевантных деталей execution.
- **Required primary evidence / experiment:** Permutation tests, loops, budgets, fixed point, Unknown on exhaustion.
- **Potential effect:** Изменить rule semantics.

#### Q21 — Можно ли одним Pure/Associative/Equivalent охватить checked integers, FP, exceptions, target effects?

- **Priority / type / state:** `HIGH` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Универсальный vocabulary переносится между доменами без semantic drift.
- **Why it changes the outcome:** В противном случае нужны target-scoped законы.
- **Required primary evidence / experiment:** Overflow/FP/exception observation matrix and legality tests.
- **Potential effect:** Сузить словарь.

### III. IR identity, legality and backend parity

#### Q22 — Что именно наследует факт при one-to-many AST→Bytecode→AIR→SSA lowering?

- **Priority / type / state:** `CRITICAL` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Origin anchors достаточно для representation-neutral knowledge.
- **Why it changes the outcome:** Без transport obligations факт оказывается привязан не к тому объекту.
- **Required primary evidence / experiment:** Production lowering trace, source/target correspondence, independent witnesses.
- **Potential effect:** Отказаться от neutral transport.

#### Q23 — Когда верное происхождение объекта НЕ доказывает переносимость свойства?

- **Priority / type / state:** `HIGH` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Provenance равен semantic equivalence.
- **Why it changes the outcome:** Может потребоваться независимая proof obligation на каждое свойство.
- **Required primary evidence / experiment:** Checked→unchecked, side effects, exception ordering, alias remapping cases.
- **Potential effect:** Изменить transport.

#### Q24 — Нужно ли SSA для заявленного механизма или хватает Bytecode/AIR?

- **Priority / type / state:** `MEDIUM` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** SSA — необходимая часть любой такой композиции.
- **Why it changes the outcome:** Может выявиться лишнее архитектурное ограничение.
- **Required primary evidence / experiment:** Same query and optimizer with SSA enabled/disabled, precision and costs.
- **Potential effect:** Изменить representation requirement.

#### Q25 — Как сохранить идентичность смыслового субъекта после inlining, CSE, duplication и code motion?

- **Priority / type / state:** `HIGH` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** IR id стабилен как semantic subject.
- **Why it changes the outcome:** False reuse опасен, избыточная инвалидация дорога.
- **Required primary evidence / experiment:** Transform corpus, one-to-many IDs, remap checker.
- **Potential effect:** Изменить subject identity.

#### Q26 — Кто решает program-local legality двух программ с одним и тем же LanguagePlan, не запуская второй config planner?

- **Priority / type / state:** `HIGH` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Global plan уже знает допустимость всех будущих transforms.
- **Why it changes the outcome:** Возможно дублирование ownership или hidden replanning.
- **Required primary evidence / experiment:** Same frozen plan, two programs, tracing decisions, legality-before-cost.
- **Potential effect:** Изменить ownership.

#### Q27 — Каковы границы interpreter/CIL parity по exception/effects/overflow, а не только по возвращаемому value?

- **Priority / type / state:** `HIGH` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Value parity достаточна для семантического переноса оптимизаций.
- **Why it changes the outcome:** Нужен backend-specific scope и stronger observable behavior.
- **Required primary evidence / experiment:** Dual-backend differential corpus with traps/effects/lifetimes.
- **Potential effect:** Изменить parity claim.

### IV. Verified present-day UniversalToolchain versus research

#### Q28 — Где фактически кончается global config planning в LanguageCompiler и начинается анализ конкретной программы?

- **Priority / type / state:** `HIGH` / `FACT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Конфигурационный planner уже решает program-dependent legality.
- **Why it changes the outcome:** Может потребоваться исправить описание реализованных свойств.
- **Required primary evidence / experiment:** Compile/route source tracing on two distinct programs with same plan.
- **Potential effect:** Удалить overclaim.

#### Q29 — Какие структурно корректные, но семантически неверные программы проходят Bytecode/AIR/module verifiers?

- **Priority / type / state:** `HIGH` / `FACT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Structural verification доказывает полную программу.
- **Why it changes the outcome:** Силу claims придётся ограничить текущим verifier boundary.
- **Required primary evidence / experiment:** Production verifier implementations, invalid-but-well-typed mutants, oracle.
- **Potential effect:** Ограничить correctness claim.

#### Q30 — Доказывает ли Acme.PricingLanguage семантическое N+1 расширение или только независимость одного языка от Wist?

- **Priority / type / state:** `CRITICAL` / `FACT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Wist-free single-language sample доказывает open-world semantic composition.
- **Why it changes the outcome:** Потребуется другой sample для central claim.
- **Required primary evidence / experiment:** Read Acme source; separately built contributors; no-edit N+1 tests.
- **Potential effect:** Изменить пример.

#### Q31 — Какие реальные AIR↔SSA↔AIR transforms семантически проверены, а где есть только structural gates?

- **Priority / type / state:** `HIGH` / `FACT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Opt-in SSA verification означает universal semantic verification.
- **Why it changes the outcome:** Риск представить узкую проверку общим доказательством.
- **Required primary evidence / experiment:** SSA source, supported operations, Disabled/Prefer/Require route tests.
- **Potential effect:** Ограничить SSA claim.

#### Q32 — Находит ли PlanFuzz классы composition/activation defects сверх обычного differential fuzzing при сопоставимом бюджете?

- **Priority / type / state:** `CRITICAL` / `COMPARISON` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Только проектируемое ядро способно стать сильным исследовательским вкладом.
- **Why it changes the outcome:** Более интересный результат может уже существовать в коде.
- **Required primary evidence / experiment:** Independent baseline, controlled mutations, real root causes, equal budget.
- **Potential effect:** Сменить тему.

#### Q33 — Держится ли результат ContractExperiments selective reverification на независимо авторских faults и end-to-end compilation?

- **Priority / type / state:** `HIGH` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Author-designed faults представляют реальную внешнюю нагрузку.
- **Why it changes the outcome:** Иначе вывод ограничен corpus-design bias.
- **Required primary evidence / experiment:** P0–P3 fixed corpus and independent holdouts, raw logs, exact oracle.
- **Potential effect:** Ограничить результат.

#### Q34 — Сколько стоит UT versus handwritten pipeline для planning/runtime/compile/steady-state, включая amortization?

- **Priority / type / state:** `HIGH` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Общность не создаёт существенного overhead.
- **Why it changes the outcome:** Практическое достоинство может зависеть от workload.
- **Required primary evidence / experiment:** Equal workload and hardware, cold/warm, memory, compilation, execution-only bench.
- **Potential effect:** Изменить perf claim.

#### Q35 — Может ли выбранный package/runtime policy реально изолировать hostile managed/native plugin?

- **Priority / type / state:** `HIGH` / `ARCHITECTURE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Проверка compositional contract равна sandbox security.
- **Why it changes the outcome:** Может потребоваться явно отказаться от security claim.
- **Required primary evidence / experiment:** Malicious plugin fixtures, threat model, process isolation control.
- **Potential effect:** Удалить security claim.

### V. Scientific contribution and audience

#### Q36 — Какую полезную реальную задачу для non-compiler engineer решает предлагаемый механизм лучше простой plugin/workflow системы?

- **Priority / type / state:** `HIGH` / `AUDIENCE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Практическая ценность очевидна широкой аудитории.
- **Why it changes the outcome:** Может понадобиться другой use case или иная аудитория.
- **Required primary evidence / experiment:** Independent non-compiler prototype, baseline and user outcome.
- **Potential effect:** Изменить пример.

#### Q37 — Можно ли за 9m30s показать факт проблемы, альтернативу и проверяемый результат без ложных упрощений?

- **Priority / type / state:** `HIGH` / `AUDIENCE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Сложность идеи совместима с коротким докладом без редизайна.
- **Why it changes the outcome:** Доклад может превратиться в перечисление намерений.
- **Required primary evidence / experiment:** Actual meetup script/deck, time-boxed dry-run, comprehension assessment.
- **Potential effect:** Изменить структуру.

#### Q38 — Можно ли пререгистрировать фальсифицируемую гипотезу и отрицательный исход до реализации эксперимента?

- **Priority / type / state:** `HIGH` / `AUDIENCE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Любая архитектурная vision уже является проверяемой гипотезой.
- **Why it changes the outcome:** Иначе нельзя отличить research от product pitch.
- **Required primary evidence / experiment:** Hypothesis, thresholds, same oracle, holdout and alternatives.
- **Potential effect:** Изменить научную аргументацию.

#### Q39 — Какие точные statements о LLVM/MLIR опровергнет их штатный production API или минимальный пример?

- **Priority / type / state:** `HIGH` / `AUDIENCE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Существующие системы действительно не поддерживают заявленные механизмы.
- **Why it changes the outcome:** Один сильный контрпример подорвёт сравнение.
- **Required primary evidence / experiment:** Claim-to-upstream API map, runnable repro, independent review.
- **Potential effect:** Удалить неверный claim.

#### Q40 — Сильнее ли история о выявлении ошибок на стыках расширений, чем создание гипотетического semantic kernel?

- **Priority / type / state:** `CRITICAL` / `AUDIENCE` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Семантическое ядро — заранее лучший сюжет.
- **Why it changes the outcome:** Может оказаться существеннее уже экспериментально реализованный PlanFuzz.
- **Required primary evidence / experiment:** Fair contribution comparison, replications, independent audience assessment.
- **Potential effect:** Сменить тему.

#### Q41 — Уменьшается ли полная стоимость добавления 1/3/5/10 расширений с учётом glue/tests/schema evolution?

- **Priority / type / state:** `HIGH` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Отсутствие pairwise source changes снижает total engineering cost.
- **Why it changes the outcome:** Framework может только перенести трудозатраты в общий слой.
- **Required primary evidence / experiment:** Controlled extension tasks, source diffs, time and defect rates.
- **Potential effect:** Изменить engineering claim.

#### Q42 — При каких заранее заданных исходах следует прекратить разработку общего semantic core?

- **Priority / type / state:** `CRITICAL` / `EXPERIMENT` / `OPEN: NOT_TESTED`.
- **Hidden assumption to challenge:** Гипотеза допускает реальный отказ независимо от результатов.
- **Why it changes the outcome:** Без go/no-go вопросы можно бесконечно переинтерпретировать.
- **Required primary evidence / experiment:** Precommitted matrix: equal simpler baseline, unsafe facts, consumer edits, costs, trust failure.
- **Potential effect:** Отказаться от механизма.

## BLIND_SPOTS

1. Actual meetup deck, timed script, abstract and independent critique were unavailable.
2. Real Wist semantics and supported AIR/SSA laws require method-level code inspection and executable counterexamples.
3. LLVM/MLIR comparisons need pinned upstream versions and runnable demonstrations, not marketing comparisons.
4. Independent third-party N+1 packages, hostile providers, dependency-completeness witnesses and schema-evolution tests are not yet integrated experiments.
5. Total integration cost, cross-IR proof preservation, audience value and replication outside author-designed corpora remain insufficiently tested.

## REQUIRED_EVIDENCE — Stage 2

| Experiment | Scope | Linked questions | Minimum acceptance / failure record |
| --- | --- | --- | --- |
| E-A | Direct C# typed-interface baseline and scoped-evidence baseline | Q07 Q09 Q10 Q41 | Independent assemblies; unchanged consumers; explicit glue diff, same inputs/oracle and authoring/overhead metrics |
| E-B | LLVM AA/AAManager plus existing optimizer | Q02 Q04 Q05 | Pinned upstream revision; AA pipeline and optimized IR; consumer source unchanged |
| E-C | MLIR Interfaces, External Models, Bufferization | Q01 Q06 Q08 | Production API integration with separate provider registration, observable consumer result |
| E-D | MLIR DataFlowSolver multi-analysis | Q03 Q16 Q20 | Shared state, Unknown, loops/fixpoint, measured comparison |
| E-E | Executable Range/Shape/Bounds transformation | Q09–Q21 | Valid and invalid inputs; fresh processes; independent reference oracle; safety-sensitive false-positive count |
| E-F | AST/Bytecode/AIR/SSA mapping | Q22–Q27 | One-to-many mapping, stale origins, change in effects/exception ordering and checked preservation |
| E-G | UT actual implementation audit | Q28–Q31 Q35 | Production code paths, direct tests, evidence of precisely stated verified boundaries |
| E-H | PlanFuzz and ContractExperiments independent baselines | Q32 Q33 Q40 | Holdouts, distinct root causes, fixed comparison budget, negative/null results |
| E-I | Cold/warm runtime and authoring study | Q34 Q36 Q37 Q38 Q41 Q42 | Full cost and time scope, not hot-only disguised as total; precommitted stop/go |

Primary-source pointers: [MLIR interfaces](https://mlir.llvm.org/docs/Interfaces/), [MLIR dataflow](https://mlir.llvm.org/docs/Tutorials/DataFlowAnalysis/), [MLIR bufferization](https://mlir.llvm.org/docs/Bufferization/), [LLVM pass manager](https://llvm.org/docs/NewPassManager.html), [LLVM AA](https://llvm.org/docs/AliasAnalysis.html), [Ideal UT experiments](EXPERIMENTS.md), [PlanFuzz](../../proposals/planfuzz/implementation-status.md), [ContractExperiments](../../../UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md).

## HANDOFF_TO_STAGE_2

1. Pin current UT SHA, SDK, LLVM and MLIR revisions; log which previously unavailable talk files are now actually accessible.
2. Investigate TOP_CRITICAL_QUESTIONS before attempting to implement new semantics. Try the strongest static C# and LLVM/MLIR alternatives first.
3. For each Q-ID record: status; exact bounded claim; source locator + commit; runnable command/seed; actual output; limitations; strongest opposing explanation; and potential effect on architecture/talk. A question stays `INCONCLUSIVE` if evidence is incomplete.
4. A passing type check, typed metadata, provenance trail or preservation declaration does not establish semantic truth; use independent execution/verification for unsafe rewrites.
5. Stop or reduce the semantic-core proposal on false-safe, unsupported trust, unacceptable old-code changes, or equally correct simpler baseline. Do not rationalize negative results.
6. Only *after* evidence, draft the revised talk thesis and update the public claim ledger. **Stage 1 does not edit slides or production code.**

**Cross-repository ownership:** this file is the canonical detailed question ledger. A private ChatGPT Knowledge Base may separately hold curated `research-question` cards with matching Q identifiers and links here; it must not become a second copy of this ledger or override UT implementation facts.
