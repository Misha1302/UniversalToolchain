---
title: Ответы на исследовательские вопросы UniversalToolchain и границы специализации CPU/GPU
authority: dated-review
status: Source-backed research synthesis; experimental obligations remain explicit
review_date: 2026-10-26
baseline: de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1
---

# Исследовательские ответы: семантика, расширения и разные процессоры

**Датированный разбор источников, а не новая архитектурная authority или отчёт о запусках.** Базовая реализация — [`master@de0da4a8`][baseline]. Документ сохраняет Q01–Q42 исходного [реестра вопросов][questions] и учитывает исследования в других ветках, которые не входят в этот baseline. Он не переписывает их исторические статусы и не объявляет завершёнными E1–E8, M0–M6 или S0–S8. Результаты исполнения, выполненного в рамках текущей работы, должны быть отдельно зафиксированы в [VALIDATION.md](VALIDATION.md). В частности, C# decision control повторён в текущей сессии; это не повышает его до интегрированного E1.

## 1. Вывод, который меняет направление

Для DSL, компилируемого под CPU, GPU и конкретные тензорные ускорители, необходимы **точные семантические контракты операций, чисел, эффектов и памяти**. Из этого не следует необходимость общего хранилища фактов или универсального механизма вывода. Наименьший разумный первый шаг — предметное представление вычисления, обычные typed C# interfaces и адаптер к существующей инфраструктуре генерации кода. Опциональный сервис evidence должен отдельно доказать пользу на реальном преобразовании и независимых потребителях. Это рекомендация данного обзора, согласованная с более поздним ограничительным [исследованием 23 октября][findings], а не измеренное превосходство выбранной архитектуры.

Слова «семантическое ядро» сейчас объединяют три разных решения:

| Уровень | Что требуется | Решение этого обзора |
| --- | --- | --- |
| Семантика предметного DSL | Типы, shapes, правила вычисления, overflow/FP, эффекты, допустимые преобразования | Нужна до оптимизаций; владелец — соответствующий language/domain package. |
| Общие typed queries и проверяемые условия применения | Неизменный consumer спрашивает известное свойство; provider отвечает в точном контексте | Начать с существующих контрактов и обычных интерфейсов; обобщать по реальным повторениям. |
| General evidence store, вывод, proof DAG, точная инвалидация | Общая инфраструктура многих независимых поставщиков/потребителей и правил | Отдельная условная гипотеза; не предпосылка подключения GPU и не готовая возможность UT. |

Сильнейшее сравнение для нового направления — **UT → тот же MLIR/IREE backend** против **обычного DSL frontend → тот же MLIR/IREE backend**. Сравнение только с ручным написанием отдельных C++/CUDA программ несправедливо: оно приписало бы UT выгоды общего downstream-компилятора, которые доступны без UT. Предыдущее [RQ3 исследование][rq3] уже показало на ограниченном примере, что downstream reuse не является уникальным свойством UT.

## 2. Как понимать статусы

- **Источник:** вывод следует из прочитанного кода, контракта или официального API; это не выполненный здесь сравнительный опыт.
- **Архитектурный ответ:** ограничение или выбранная граница обоснованы устройством задачи; эмпирическая полезность конкретного API ещё не показана.
- **Исторический контроль:** источник содержит воспроизводимый код/результат прошлой работы; его исторические данные не считаются новым запуском. Исключение — отдельно записанный в [VALIDATION.md](VALIDATION.md) повтор C# decision control.
- **NOT_TESTED:** решающий опыт для указанного утверждения отсутствует в рассматриваемой доказательной базе. Частичный ответ не снимает этого обязательства.

`SUPPORTED` в [ответах 23 октября][answers] означает узкий подтверждённый тезис, а не универсальную soundness. Данные источника о toy-модели, проверенном маршруте и успешном CI не заменяют семантический oracle для нового преобразования.

## 3. Q01–Q42: disposition и оставшееся обязательство

### I. Сильные альтернативы и prior art

| ID | Ограниченный ответ и основание | Что остаётся NOT_TESTED / практическое решение |
| --- | --- | --- |
| **Q01** | **Источник / PARTIAL.** MLIR External Models позволяют реализовать Op/Type/Attribute interface вне исходного dialect; неизменный consumer должен уже запрашивать этот interface. [MLIR Interfaces][mlir-interfaces], [предыдущий разбор][prior-art]. | Отдельные frozen producer/consumer binaries с изменением реального результата. Независимое подключение интерфейса не объявлять новизной UT. |
| **Q02** | **Источник / PARTIAL.** LLVM AAManager агрегирует зарегистрированные AA для существующих потребителей AAResults; порядок регистрации имеет значение. [AAManager][llvm-aa], [разбор][prior-art]. | Контролируемый AA→LICM/GVN опыт с IR diff и query trace. Нельзя обещать монотонное улучшение при произвольной добавке/перестановке analyses. |
| **Q03** | **Источник / PARTIAL.** MLIR DataFlowSolver координирует несколько analyses и зависимости до fixed point. Это серьёзная альтернатива новому rule engine. [DataFlow][mlir-dataflow]. | Конкретный корректный Range+Shape→SafeIndex на одинаковом corpus и стоимости. Сам fixed point не является аргументом за новый semantic core. |
| **Q04** | **Источник / SUPPORTED в узком смысле.** LLVM Attributor уже реализует совместное выведение взаимодействующих IR-свойств. [Attributor][llvm-attributor]. | Соответствие всем требованиям UT к внешним пакетам/контекстам не показано. Исключить novelty claim о самом факте совместного вывода. |
| **Q05** | **Источник / PARTIAL.** LLVM/MLIR analysis managers уже поддерживают кеширование и invalidation. [LLVM New PM][llvm-pm], [MLIR PassManagement][mlir-pm]. | Одинаковые edit/query traces с external executable identity и cross-IR scopes. Начать с полной инвалидации/reanalysis; точный DAG оправдывать измерениями. |
| **Q06** | **Источник / SUPPORTED в узком смысле.** One-Shot Bufferization разделяет AnalysisState и общий rewriting, используя BufferizableOpInterface, включая external models. [Bufferization][mlir-buffer]. | Два реально собранных анализа, один transform, наблюдаемые copy/allocation differences. Это сильный baseline, не «MLIR умеет только локальные passes». |
| **Q07** | **Исторический контроль / PARTIAL.** Пять C# проектов провели 12 decision assertions с заранее известным ISafeIndexQuery и неизменным Consumer; текущий повтор 12/12 записан в [VALIDATION.md](VALIDATION.md). [Код/ограничения][csharp-control], [протокол][experiment-design]. | Реальное удаление checks в UT, independent oracle и равнобюджетное сравнение отсутствуют. Direct typed C# остаётся предпочтительным вариантом при равном результате. |
| **Q08** | **Архитектурный ответ; решающий опыт NOT_TESTED.** Наличие AST/Bytecode/AIR/SSA само по себе не доказывает новую проблему: MLIR имеет multi-dialect conversion. [Dialect Conversion][mlir-conversion]. | Два реальных lowering cases с checked mappings, одинаковыми наблюдениями и сравнением с reanalysis. Не строить общий transport только ради нескольких названий IR. |

### II. Независимые анализы, доверие и время жизни знания

| ID | Ограниченный ответ и основание | Что остаётся NOT_TESTED / практическое решение |
| --- | --- | --- |
| **Q09** | **Исторический контроль / PARTIAL.** В малом C# примере меняется host registration, прежний Consumer не меняется; Shared/glue существует заранее. [Контроль][csharp-control]. | Полная стоимость schemas, adapters, registry/config, tests и package upgrades. Нулевой consumer diff не равен нулевой интеграции. |
| **Q10** | **Архитектурный ответ / PARTIAL.** Новая категория факта не изменяет код, который её не запрашивает. Нужен известный query hook, общий механизм применения или новый transform. [Выводы][findings]. | Controlled hook/no-hook test не выполнен. Убрать обещание «любой старый optimizer сам научится использовать любой новый факт». |
| **Q11** | **NOT_TESTED.** Одинаковые typed names не гарантируют ABI и семантическую совместимость новых схем. [Проект API][api-proposal]. | Матрица provider/consumer/schema N/N+1, serialization и миграции. Не замораживать публичный evidence ABI до независимых consumers. |
| **Q12** | **Архитектурный ответ; integrated опыт NOT_TESTED.** Одних Range и Shape недостаточно при неопределённых path, aliasing, mutation, signedness, overflow и traps. [E1 protocol][experiment-design]. | Настоящий Read lowering, фактический IR diff и reference execution с негативными случаями. Boolean CanElideCheck не засчитывается как устранение проверки. |
| **Q13** | **Архитектурный ответ; NOT_TESTED.** Факт должен иметь program point/path/region и корректный dominance scope; value ID + revision недостаточны. [API proposal][api-proposal]. | Branch merge, loops, code motion, effectful calls и отмена локальных фактов. Вне проверенного scope — Unknown/reanalysis. |
| **Q14** | **Источник / PARTIAL.** В UT есть typed invalidation и reverification scheduling; заявление pass о preserves не является независимым доказательством. [Scheduler][scheduler], [study protocol][contract-study]. | Analysis→transform A→consumer B со лживым preserves и независимым postcondition oracle. Использовать existing owner, не дублировать scheduler. |
| **Q15** | **Источник + исторический контроль / PARTIAL.** Типизированная ложная посылка остаётся ложной. Low-level trust study показывает потерю эффекта при вручную доверенном ложном descriptor, но stock profile сохраняет вызов. [Trust decision][trust-decision]. | Реальная выбранная UT route, авторизация внешнего provider, end-to-end effect oracle и невзаимодействующие holdouts. Не объявлять default Wist уязвимым по low-level конструкции. |
| **Q16** | **UNDETERMINED.** Proven/Disproven/Unknown/Contradiction различают отсутствие и конфликт, но минимальность четырёх публичных состояний не доказана. [Ответы][answers]. | Показать consumer-visible случаи, которые нельзя столь же просто выразить conservative lattice + diagnostic. Пока оставить внутренней формой эксперимента. |
| **Q17** | **Архитектурный ответ; NOT_TESTED.** «Все зависимости имеют X» допустимо только при установленной полноте набора зависимостей. [Semantic note][semantic-note]. | Missing provider/incomplete dependency cases и authority для completeness. Отсутствие отрицательного факта не доказывает полноту. |
| **Q18** | **Источник / PARTIAL.** PlanHash и manifest/registration binding защищают конфигурационную идентичность; это не общий proof cache по реальным DLL/native dependencies/driver/target. [Current status][current-status], [world scope][architecture-proposal]. | Замена фактических binary digests при неизменном плане, runtime/backend/trust changes. Caller-claimed hash из toy-контроля не является аутентификацией. |
| **Q19** | **NOT_TESTED для общего сервиса.** Селективный отзыв произвольных derived facts не продемонстрирован. Library-first pilot начинает с session-local DAG и полной инвалидации snapshot после commit. [Pilot architecture][library-architecture]. | Сравнить selective invalidation с полным reanalysis по soundness, памяти и времени на held-out edits. Не вводить persistent graph заранее. |
| **Q20** | **Источник / PARTIAL.** Детерминированность LanguageCompiler не распространяется автоматически на будущие inference rules. [Compiler][compiler], [ответы][answers]. | Provider permutations, recursion, budgets и cancellation на настоящем inference-пилоте. Исчерпание бюджета не даёт положительного разрешения. |
| **Q21** | **Архитектурный ответ; generic corpus NOT_TESTED.** Pure/Associative/Equivalent требуют domain, FP/overflow/trap/effect semantics и target scope. [Semantic note][semantic-note], [scope model][api-proposal]. | Проверить checked/wrapping integers, NaN, signed zero, reassociation, FMA и reduction order. Для CPU/GPU это часть specification DSL, а не необязательный metadata декор. |

### III. Представления, legality и разные backend

| ID | Ограниченный ответ и основание | Что остаётся NOT_TESTED / практическое решение |
| --- | --- | --- |
| **Q22** | **Архитектурный ответ; NOT_TESTED.** One-to-many source anchors показывают происхождение, но не доказанную переносимость факта. [API proposal][api-proposal], [E2][experiments]. | Реальная AST→Bytecode→AIR→SSA correspondence с проверкой отдельного свойства. Предпочесть target reanalysis, если он проще. |
| **Q23** | **Источник / PARTIAL.** Законная type/op conversion не означает поведенческую эквивалентность всех свойств. [MLIR Conversion][mlir-conversion], [Alive2][alive2]. | Checked→unchecked, changed traps/order, alias remap и stale origin mutants. Ни source map, ни proof DAG сами по себе не разрешают оптимизацию. |
| **Q24** | **Источник / PARTIAL.** Текущий generic SDK не требует обязательного SSA; Wist SSA — opt-in route. [Current status][current-status], [SSA coverage][ssa-coverage]. | Одинаковая семантическая query с SSA on/off и стоимость. Новый tensor DSL может иметь собственный high-level IR без обязательного stack AIR roundtrip. |
| **Q25** | **NOT_TESTED.** Общей стабильной semantic subject identity после inlining/CSE/duplication/code motion не установлено. [Ответы][answers]. | Exact transform corpus и mapping checker. При неоднозначном наследовании — invalidate/reanalyse, а не сохранять факт по старому ID. |
| **Q26** | **Источник / PARTIAL.** LanguagePlan фиксирует компоненты и конфигурационные маршруты; legality конкретной программы — другая ответственность. [Compiler][compiler], [route phase][route-phase]. | Один frozen plan, две программы, разные допустимые преобразования. Program-specific schedule/placement разрешены внутри выбранного набора компонентов; повторно выбирать packages нельзя. |
| **Q27** | **Источник / PARTIAL.** Shared-supported-behavior parity полезна, но value equality слабее сохранения exceptions, effects, ordering и lifetime. [Current status][current-status]. | Независимый reference и target-specific observation matrix. CPU/GPU strict и approximate numerical profiles должны быть названы явно. |

### IV. Реально реализованный UT и исследовательские границы

| ID | Ограниченный ответ и основание | Что остаётся NOT_TESTED / практическое решение |
| --- | --- | --- |
| **Q28** | **Источник / SUPPORTED в узком смысле.** LanguageCompiler получает LanguageDefinition, выбирает feature/contribution/provider/route; это не вход ProgramSnapshot и не end-to-end device cost model. [Compiler][compiler], [route phase][route-phase]. | Не выдавать minimum descriptor-route cost за глобальный оптимум программы. Program planning должен учитывать копии, layouts, fusion, memory и synchronization отдельно. |
| **Q29** | **Источник + исторический контроль / PARTIAL.** Structural/type/owner verification проверяет представленную модель. Корректная форма IR не доказывает сохранение значения/эффектов. [Current status][current-status], [trust study][trust-decision]. | Независимые semantic mutants через реальные production paths и bounded checker. Каждый verifier обязан назвать собственную гарантию. |
| **Q30** | **Источник / SUPPORTED в узком смысле.** Acme показывает non-Wist language с собственными parser и execution routes; это не N+1 semantic inference. [Acme][acme], [current status][current-status]. | Независимые frozen provider/consumer assemblies и реально применённое новое знание. Tensor/stream язык полезен как новый holdout, но не доказательство универсальности сам по себе. |
| **Q31** | **Источник / PARTIAL.** AIR↔SSA coverage ограничена; local CSE не включён в preview roundtrip до repeated-value scheduling, broader GVN/LICM/inlining отложены. [SSA coverage][ssa-coverage]. | Негативный/положительный oracle каждого нового pass/shape и actual backend outcome. Verifier gate не повышать до универсального semantic preservation theorem. |
| **Q32** | **Источник / PARTIAL.** PlanFuzz уже имеет configuration-aware oracles, fresh-process replay и reducer. [Implementation status][planfuzz]. | Equal-budget сравнение с program-only/pairwise baselines. Для hardware DSL расширять adapter/oracles, не писать второй fuzzer и не объявлять сравнительное превосходство. |
| **Q33** | **Источник / PARTIAL.** ContractExperiments вызывает production verifiers и сравнивает frozen policies/fault corpus. [Study protocol][contract-study]. | Independently authored faults и whole-source-to-execution validation. Время verification kernel не считать полной стоимостью компиляции. |
| **Q34** | **NOT_TESTED в требуемом полном объёме.** Нет сопоставимого полного planning/materialization/compile/first/hot hardware benchmark, доказывающего преимущество нового направления. [E6][experiments], [measurement protocol][experiment-design]. | Показать cold и amortized end-to-end стоимость, allocations, transfer/sync, residency и конфигурационный churn; device-only latency вынести отдельно. |
| **Q35** | **Источник / SUPPORTED отрицательное ограничение.** In-process policies, signatures, metadata и PlanHash не являются hardened sandbox для hostile managed/native plugin. [Current status][current-status]. | Если появится такой продуктовый сценарий — отдельный process/OS threat model. Не навязывать sandbox задаче доверенной компиляции, но и не обещать его. |

### V. Польза, цена расширений и критерии остановки

| ID | Ограниченный ответ и основание | Что остаётся NOT_TESTED / практическое решение |
| --- | --- | --- |
| **Q36** | **UNDETERMINED.** Внешне подтверждённого превосходящего сценария semantic core для non-compiler пользователя нет; embedded numeric/rules и hardware compute — гипотезы. [Strategy][strategy], [LangDev ideas][langdev-ideas]. | Реальная программа, независимый пользователь и сильный baseline. Выбрать задачу до большого workbench или ontology. |
| **Q37** | **NOT_TESTED.** Конкретные meetup deck/script не проверены этой работой. Исторический narrative review предпочитает узкую работающую демонстрацию. [Narrative][narrative]. | Фактическая репетиция/оценка понимания; не объявлять переоценённым или исправленным невиденный доклад. |
| **Q38** | **Источник / PARTIAL.** Уже есть falsifiable E1–E8 и frozen controls. [Experiments][experiments], [newer protocol][experiment-design]. | Заморозить именно новый hardware corpus/observation model/baselines до измерений и сохранить отрицательные исходы. Preregistration сама не доказывает гипотезу. |
| **Q39** | **Источник / PARTIAL.** Фразы «LLVM/MLIR не умеют независимые analyses, external interfaces, invalidation/fixpoint» противоречат документированным API. [Prior-art comparison][prior-art]. | Пинованные upstream executable fixtures ещё нужны для точных performance/integration claims. Удалить широкие негативные сравнения уже сейчас. |
| **Q40** | **Источник / PARTIAL.** PlanFuzz/ContractExperiments имеют больше работающего исследовательского кода, чем общий semantic core. [Findings][findings], [PlanFuzz][planfuzz]. | Scientific superiority и audience benefit не измерены. Это самостоятельный сильный сценарий развития независимо от успеха GPU-ветки. |
| **Q41** | **Исторический контроль / PARTIAL.** Микроисследование двух вариантов показывает меньшую marginal feature slice UT, но больший total footprint; масштаб 1/3/5/10 расширений не измерен. [DSL results][dsl-results], [RQ3][rq3]. | Полная стоимость independent additions, adapters, schema evolution, tests, documentation и upgrades. Платформа не выигрывает от одного лишь отсутствия consumer edits. |
| **Q42** | **Архитектурное решение / PARTIAL.** Уже задан отказ от обобщения при unsafe accepted rewrite, неверифицируемом trust, hidden second planner или равноценном более простом baseline. [Findings][findings], [protocol][experiment-design]. | Порог стоимости зависит от реального workload и должен фиксироваться заранее. Не менять критерий после проигрыша; отсутствие необходимости в core — успешный исследовательский итог. |

## 4. Как согласуются решения 20 и 23 октября

[ADR-02 от 20 октября][library-decision] имеет статус **ACCEPTED for a controlled pilot**: `SemanticEvidence.Core` без UT-зависимостей, опциональные numeric rules и отдельный UT adapter. Adapter владеет реальным selected-world admission, IR identity и атомарной публикацией. Core управляет контекстными facts/DAG и не получает право произвольно менять UT IR. Runtime проверки применения trusted C# rules не означают повторное математическое доказательство самих правил.

Существенные границы того ADR:

- Production, публичная NuGet-публикация и rollout остаются **NO-GO**.
- Новое разделение библиотеки и реальный UT adapter не реализованы и не протестированы этим документом.
- Старый прототип смешивал generic inference и имитацию rewrite permission; его нельзя просто переименовать и назвать переносимой библиотекой.
- [Матрица гипотез][library-hypotheses] сообщает о **18 081 одинаковом решении** model и ordinary C# на ограниченном корпусе. Это исторический результат модели, не преимущество выразительности, performance или integrated UT.
- Начальный scope исключает FP, aliasing, arbitrary calls/effects, interprocedural fixpoint, persistent cache и hostile in-process isolation. Эти исключения важны для hardware-направления: наличие модели Int32 intervals не решает tensor semantics.

[Обзор 23 октября][findings] устанавливает **NO-GO для shipping/generalization semantic core сейчас** и рекомендует сначала реальное преобразование с обычными typed interfaces. Это совместимо с сохранением узкого пилота как проверяемой гипотезы. Датированный обзор не должен молча превращать пилот в production roadmap или стирать прежнюю идею.

**Решение для текущего направления:** hardware DSL развивается без зависимости от завершения общего evidence engine. Если один и тот же query/revocation protocol действительно понадобится двум независимо написанным consumers и выиграет у хорошего direct-C# baseline, пилот можно продолжить. Отдельно проверяются portable non-UT consumer, binding к фактическим реализациям и real rewrite gate. До этого новый stable public kernel API не нужен.

## 5. Противоречащие оптимистичному тезису результаты

### Реальная цена composability

[Исследование DSL evolution][dsl-results] использовало маленький pricing-язык; его числа нельзя переносить на все DSL. Тем не менее это более сильное свидетельство, чем абстрактное обещание меньшего boilerplate:

| Историческая метрика | UT | Сильная альтернатива | Корректный вывод |
| --- | ---: | ---: | --- |
| Вторая feature, handwritten slice | 19 SLOC | 35 SLOC clone-and-own | Меньшая предельная добавка на этом шаге. |
| Полный handwritten treatment | 105 SLOC | 61 SLOC clone-and-own; 26 fixed control | Total-cost crossover в двух вариантах не достигнут. |
| Общая percent validation правка | 1 место / 7 changed LOC | 1 место / 7 changed LOC | Преимущество распространения этой правки не показано. |
| Shared downstream canonicalization | 1 место / 5 changed LOC | 1 место / 3 changed LOC в ordinary shared pipeline | Оба подхода не меняли variant-specific semantics. |
| Абсолютные repeated eligible lines | 18 | 18 clone-and-own | Меньшая доля дублей при большем знаменателе не равна меньшему числу дублей. |

Эти результаты прочитаны по исходникам отчётов; свежий replay здесь не заявлен. [RQ3 control][rq3] отдельно опровергает попытку использовать общее downstream улучшение как уникальное обоснование UT. Для CPU/GPU поэтому нужна одинаковая нижележащая инфраструктура в двух сравниваемых вариантах.

### Доверие нельзя получить из типизации и происхождения

В [SSA trust characterization][trust-decision] ложный `IsPure/VerifiedPlugin` удалял эффект только при ручной передаче такого descriptor в low-level DCE. Stock optimizer profile оставлял тот же вызов. Прежний Linux CI receipt относится к указанному в том документе tested commit; это не свежий run текущей ветки. Нельзя делать ни вывод «default Wist небезопасен», ни обратный вывод «structural verifier гарантирует все эффекты».

Для trusted extensions достаточно явно назвать trusted computing base и проверять её контракт. Для неподтверждённых premises правильный ответ — сохранить исходный код или отказать в применении. Evidence provenance объясняет цепочку вывода, но не исправляет ложный root fact. Это сохраняется и при использовании MLIR, LLVM, Datalog или собственного сервиса.

## 6. Что именно проверять в hardware-специализации

Это предложения, а не подтверждённая реализация:

1. **Сохранять смысл высоко в pipeline.** Tensor/map/reduce/region IR должен держать shapes, indexing и memory semantics до fusion/layout/schedule choices. Преждевременное сведение к stack AIR теряет полезную структуру; generic UT route не обязан проходить через Wist IR.
2. **Отделить вычисление от расписания.** Один semantic program может иметь CPU vector/thread schedule и GPU tile/workgroup schedule. Корректное CPU исполнение GPU-oriented программы не означает перенос производительности. Для TPU/NPU нужен конкретный поддерживаемый compiler/runtime и реально доступное устройство.
3. **Сохранить одного владельца конфигурации.** LanguageCompiler выбирает доступные packages, executors и routes. Program plan выбирает законные layout/fusion/placement/specialization внутри этого мира. Так появляется глобальная оптимизация программы без второго configuration resolver.
4. **Измерять конец-в-конец.** Cost включает compile/startup, host↔device copy, residency, layouts, temporaries, synchronization, memory peak и reusable libraries. Быстрый локальный kernel может ухудшить целую программу.
5. **Проверить расширяемость на замороженном потребителе.** Новый operation/analysis/target schedule должны пройти реальный source→result путь; считать все integration edits. Один большой package, который изменяется целиком, не проверяет independent composition.
6. **Использовать нынешнюю проверочную инфраструктуру.** Третий PlanFuzz adapter и новые target-specific observations могут одновременно проверить новую специализацию и универсальность harness. Generic core не должен знать конкретные имена GPU/операций/профилей.

Первый discriminating workload должен быть достаточно мал для независимого reference и достаточно структурирован для нелокального эффекта — например, цепочка map/elementwise/reduction с альтернативами fusion и intermediate materialization. Конкретная задача, числовая политика и hardware фиксируются до performance-выводов. GEMM/library baseline нужен там, где он соответствует задаче; переписанная вручную медленная матричная петля — слабый конкурент.

## 7. Прочитанный корпус и точные ревизии

| Источник | Прочитанный объём / смысл | Revision и статус |
| --- | --- | --- |
| Основной Ideal UT dossier | Все 17 файлов каталога: архитектура, IUT-01–13, 222-item inventory, Q01–Q42, APIs, E/M/S, prior art, origins, tooling и traceability. | [Mainline dossier][dossier] @ `de0da4a8`. Это смесь current-source references и proposals. |
| Semantic Fact & Proof Layer | Полный 686-строчный note: vocabulary/laws/rules/obligations, delegates/serialization, completeness, trust и MVP. | [Note][semantic-note] @ `de0da4a8`; proposal. |
| Semantic composition critical review | Все 42 answer dispositions; findings, controls, prior-art, protocol, narrative; отдельные C# control sources. | [Tree][critical-tree] @ `a1e5f25bf0afd7850075ed4aeafc8b2a48c6c99d`; отдельная ветка, не mainline baseline. |
| Library-first evidence | README, ADR, hypotheses, implementation architecture и границы сохранённого model snapshot. | [Tree][library-tree] @ `d7cfc45b25586ddf0fd5dac4e24a4c0c818bb4c9`; pilot design, не UT implementation. |
| Independent architecture review | Review, falsification addendum, inventory audit, prior-art delta, research governance. | [Tree][independent-tree] @ `77ed7d496bbf98e3b7a8b2f40266f66df7ca225f`; отдельный датированный обзор. |
| SSA trust study | Пререгистрация, protocol amendments, decision и stock-profile counter-control. | [Tree][trust-tree] @ `34707feb85e976ce879c1342f4b0a84f0a110ae7`; исторические bounded tests и CI receipt. |
| DSL evolution / RQ3 | README, RESULTS, RQ3_RESULTS, hypotheses, limitations, presentation-safe findings. | [Tree][dsl-tree] @ `01e0a85d9b9742b3654476ec360f98d5d4027819`; исторический micro-study. |
| LangDev ideas | Полный UT-01…04: failure packet, lifecycle schedules, independent adapters и numeric-formula pilot. | [Ideas][langdev-ideas] @ `b517405a7b2c8c63c111eac54abe69a862acd43a`; идеи, не implementation. |
| Current architecture / SSA / PlanFuzz | Current boundaries, coverage, typed oracles/replay/reduction и незавершённые сравнительные исследования. | [Status][current-status], [SSA][ssa-coverage], [PlanFuzz][planfuzz] @ baseline. |
| Локальные неопубликованные материалы 10 октября | Выборочные summaries/conclusions о semantic introspection и agent efficiency; source revision в них `886bf241`. Использованы как указатели для свежей проверки, не как доказательство новых запусков. | Untracked local synthesis; сырые материалы, персональные журналы и их содержимое не скопированы в этот публичный отчёт. |

Ветка `research/langdev-validated-ideas-20261025` при чтении указывала на тот же `de0da4a8`, что и baseline; дополнительных результатов в ней не обнаружено. Название ветки не является evidence. Оригинальные ZIP двух исследований 8 октября, упомянутые в [traceability][traceability], не подменены их curated summaries и не объявлены перечитанными здесь.

## 8. Практический порядок решения открытых вопросов

Сначала фиксируются semantic subset, independent oracle и сильный прямой backend adapter. Затем — первый реальный CPU/GPU execution slice и отдельно полная стоимость. После этого проверяются independent extension и program-level choices. Только наблюдаемая повторная потребность в совместных фактах, trust и revocation оправдывает следующий уровень semantic evidence.

Обобщение прекращается, если появляется false-safe transformation, допускается stale/untrusted premise, теряется один configuration owner либо direct adapter даёт те же гарантии проще. Неудача общей ontology не отменяет полезность предметного DSL, typed SDK или PlanFuzz. Дополнительные research документы не заменяют следующий исполняемый опыт.

## Источники

Ссылки на UT ниже привязаны к неизменяемым commit SHA. Ссылки LLVM/MLIR/Alive2 — официальные первичные материалы; они подтверждают механизм, но не означают, что upstream experiment был собран и запущен в этом обзоре.

[baseline]: https://github.com/Misha1302/UniversalToolchain/tree/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1
[dossier]: https://github.com/Misha1302/UniversalToolchain/tree/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08
[questions]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/ADVERSARIAL_QUESTION_LEDGER_2026-10-22.md
[current-status]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/CURRENT_ARCHITECTURE_STATUS.md
[compiler]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs
[route-phase]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs
[scheduler]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractVerificationScheduler.cs
[contract-study]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/STUDY_PROTOCOL_V3.md
[acme]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/samples/Acme.PricingLanguage/Program.cs
[ssa-coverage]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/architecture/ssa-coverage-matrix.md
[planfuzz]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/proposals/planfuzz/implementation-status.md
[semantic-note]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/docs/research/semantic-fact-proof-layer-2026-09-20.md
[architecture-proposal]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/ARCHITECTURE.md
[api-proposal]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md
[experiments]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/EXPERIMENTS.md
[strategy]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/STRATEGY_PRIOR_ART.md
[traceability]: https://github.com/Misha1302/UniversalToolchain/blob/de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1/internal-docs/research/ideal-ut-2026-10-08/RESEARCH_TRACEABILITY.md
[critical-tree]: https://github.com/Misha1302/UniversalToolchain/tree/a1e5f25bf0afd7850075ed4aeafc8b2a48c6c99d/internal-docs/research/ideal-ut-2026-10-08
[findings]: https://github.com/Misha1302/UniversalToolchain/blob/a1e5f25bf0afd7850075ed4aeafc8b2a48c6c99d/internal-docs/research/ideal-ut-2026-10-08/SEMANTIC_COMPOSITION_FINDINGS.md
[answers]: https://github.com/Misha1302/UniversalToolchain/blob/a1e5f25bf0afd7850075ed4aeafc8b2a48c6c99d/internal-docs/research/ideal-ut-2026-10-08/CRITICAL_QUESTIONS.md
[prior-art]: https://github.com/Misha1302/UniversalToolchain/blob/a1e5f25bf0afd7850075ed4aeafc8b2a48c6c99d/internal-docs/research/ideal-ut-2026-10-08/PRIOR_ART_COMPARISON.md
[experiment-design]: https://github.com/Misha1302/UniversalToolchain/blob/a1e5f25bf0afd7850075ed4aeafc8b2a48c6c99d/internal-docs/research/ideal-ut-2026-10-08/EXPERIMENT_DESIGN.md
[csharp-control]: https://github.com/Misha1302/UniversalToolchain/blob/a1e5f25bf0afd7850075ed4aeafc8b2a48c6c99d/internal-docs/research/ideal-ut-2026-10-08/controls/static-csharp-baseline/README.md
[narrative]: https://github.com/Misha1302/UniversalToolchain/blob/a1e5f25bf0afd7850075ed4aeafc8b2a48c6c99d/internal-docs/research/ideal-ut-2026-10-08/MEETUP_NARRATIVE_EVIDENCE.md
[library-tree]: https://github.com/Misha1302/UniversalToolchain/tree/d7cfc45b25586ddf0fd5dac4e24a4c0c818bb4c9/internal-docs/research/semantic-evidence/library-first-2026-10-20
[library-decision]: https://github.com/Misha1302/UniversalToolchain/blob/d7cfc45b25586ddf0fd5dac4e24a4c0c818bb4c9/internal-docs/research/semantic-evidence/library-first-2026-10-20/docs/00_DECISION.md
[library-hypotheses]: https://github.com/Misha1302/UniversalToolchain/blob/d7cfc45b25586ddf0fd5dac4e24a4c0c818bb4c9/internal-docs/research/semantic-evidence/library-first-2026-10-20/docs/01_HYPOTHESIS_MATRIX.md
[library-architecture]: https://github.com/Misha1302/UniversalToolchain/blob/d7cfc45b25586ddf0fd5dac4e24a4c0c818bb4c9/internal-docs/research/semantic-evidence/library-first-2026-10-20/docs/02_IMPLEMENTATION_ARCHITECTURE.md
[independent-tree]: https://github.com/Misha1302/UniversalToolchain/tree/77ed7d496bbf98e3b7a8b2f40266f66df7ca225f/internal-docs/research/ideal-ut-2026-10-08
[trust-tree]: https://github.com/Misha1302/UniversalToolchain/tree/34707feb85e976ce879c1342f4b0a84f0a110ae7/internal-docs/research/ideal-ut-2026-10-08
[trust-decision]: https://github.com/Misha1302/UniversalToolchain/blob/34707feb85e976ce879c1342f4b0a84f0a110ae7/internal-docs/research/ideal-ut-2026-10-08/SSA_TRUST_DECISION_2026-10-12.md
[dsl-tree]: https://github.com/Misha1302/UniversalToolchain/tree/01e0a85d9b9742b3654476ec360f98d5d4027819/internal-docs/research/dsl-evolution-study
[dsl-results]: https://github.com/Misha1302/UniversalToolchain/blob/01e0a85d9b9742b3654476ec360f98d5d4027819/internal-docs/research/dsl-evolution-study/RESULTS.md
[rq3]: https://github.com/Misha1302/UniversalToolchain/blob/01e0a85d9b9742b3654476ec360f98d5d4027819/internal-docs/research/dsl-evolution-study/RQ3_RESULTS.md
[langdev-ideas]: https://github.com/Misha1302/UniversalToolchain/blob/b517405a7b2c8c63c111eac54abe69a862acd43a/internal-docs/research/LANGDEV_IDEAS_2026-10-25.md
[mlir-interfaces]: https://mlir.llvm.org/docs/Interfaces/
[mlir-dataflow]: https://mlir.llvm.org/docs/Tutorials/DataFlowAnalysis/
[mlir-buffer]: https://mlir.llvm.org/docs/Bufferization/
[mlir-conversion]: https://mlir.llvm.org/docs/DialectConversion/
[mlir-pm]: https://mlir.llvm.org/docs/PassManagement/
[llvm-aa]: https://llvm.org/docs/doxygen/classllvm_1_1AAManager.html
[llvm-attributor]: https://llvm.org/doxygen/structllvm_1_1Attributor.html
[llvm-pm]: https://llvm.org/docs/NewPassManager.html
[alive2]: https://github.com/AliveToolkit/alive2
