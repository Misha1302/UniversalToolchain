# LangDev → UniversalToolchain: проверка гипотез и следующие эксперименты

Дата: 2026-10-25. Исследованный baseline: `de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1`.
Статус: исследовательские идеи, НЕ новая архитектурная authority и НЕ объявление реализации.

## Что проверено

Прочитаны AGENTS, `docs/CURRENT_ARCHITECTURE_STATUS.md`, реальные типы наблюдений и reducer PlanFuzz, тесты классификации находок; проверен состав CLI, адаптеров, SDK и тестов. .NET-suite этого репозитория в исследовании не запускался. Наличие тестов — статическое свидетельство, а не свежий PASS. Указанный AGENTS файл `docs/DOCUMENTATION_INDEX.md` отсутствовал в baseline: это documentation drift, не разрешение менять ownership.

**Гипотеза «нужен новый дифференциальный fuzzer» опровергнута.** Уже есть PlanFuzz: нейтральное ядро, Acme/Wist adapters, parity/negative-surface/noninterference/fallback/lock oracles, isolated workers, replay, structured shrinking. Написать рядом второй генератор означало бы дублировать архитектуру.

Конкретные опоры:

- `UniversalToolchain/UniversalToolchain.PlanFuzz.Core/PlanFuzzObservation.cs`: Success требует value; неуспех требует failure; Timeout/InfrastructureFailure отделены.
- `UniversalToolchain/UniversalToolchain.PlanFuzz.Core/IPlanFuzzProgramReducer.cs`: сокращение программы принадлежит адаптеру.
- `UniversalToolchain/UniversalToolchain.PlanFuzz.Tests/PlanFuzzFindingClassificationTests.cs`: exact replay fingerprint отличается от triage class fingerprint.
- CLI уже имеет CampaignRunner, ReplayCoordinator и ReductionCoordinator.
- Architecture явно оставляет lifecycle schedules, schedule reduction, equal-budget baselines и третий внешний adapter незавершёнными.

## UT-01. Пакет доказательств ошибки для агента, а не полный dump репозитория

**Гипотеза:** воспроизводимая ошибка компилятора исправляется дешевле, если агент начинает с минимального testcase и точных нарушенных контрактов, а не заново исследует весь проект. Пока экономия не измерена.

**Предложение:** отдельный tooling-проектор `FailureEvidencePacket/v1` над существующими PlanFuzz artifacts. Поля: baseline commit, adapter/version, exact replay fingerprint, отдельный class fingerprint, plan/package identity, selected/excluded surface, violated contracts, reduced program, diagnostics/stages, ссылки на owning modules/tests. Полный evidence остаётся доступным; краткая выдача сообщает размеры, `truncated` и `omitted_sections`. Не группировать автоматические fixes только по class fingerprint: одинаковая форма не доказывает одинаковую причину.

**Границы:** formatter не становится вторым LanguageCompiler, registry или источником activation. Никакого распознавания Wist через regex. Сначала offline CLI; MCP — лишь при доказанной отдельной потребности.

**Опыт:** заранее заморозить 12 regression tasks и 6 holdout. A — обычные grep/read; B — packet плюс дозагрузка. Одна модель/version/effort и общий лимит; независимые повторные сессии. Метрики: корректные fixes по независимым тестам, число LLM round trips, input/output/cached tokens, стоимость на корректный fix, p50/p95 времени. Пакет выигрывает только при неухудшении качества.

**Опровержение:** пропущен решающий invariant, агент всё равно читает весь репозиторий либо локальный fix ломает holdout. Тогда оставить formatter для человека, не строить агентную платформу. Приоритет P1, размер средний, НЕ реализовано.

## UT-02. Lifecycle schedules поверх существующего PlanFuzz

**Гипотеза:** expression-only генерация не покрывает ошибки взаимодействия execute/dispose/cancel и PerSession/SingletonStateless. Добавление управляемых расписаний даст новые классы нарушений.

**Проверка предпосылки:** lifecycle/in-flight coordination уже входят в runtime contract, а schedule testing явно incomplete. Это пробел в исследовательском покрытии, НЕ найденный runtime bug.

**Минимальный дизайн:** ограниченные действия create-session / start-operation / await-barrier / cancel / dispose / observe; barriers и partial order вместо случайных sleep. Oracle проверяет существующие обязательства runtime, а не произвольно выбранного победителя гонки. Отдельные program hash и schedule hash. Сокращение расписания сохраняет исходный exact fingerprint; timeout не превращается автоматически в semantic failure.

**Опыт:** несколько ручных deterministic schedules на Acme, затем Wist. Временный mutant только в disposable fixture доказывает чувствительность теста; production runtime ради опыта не менять. Сравнить confirmed failure classes при одинаковом времени с expression-only baseline. Cold replay обязателен.

**Опровержение:** находки зависят от harness timing, сокращение меняет причину ошибки, наблюдаемое поведение разрешено контрактом. Приоритет P1, размер средний/большой, НЕ реализовано.

## UT-03. Независимые diagnostic twins и третий adapter

**Гипотеза:** дополнительная тысяча программ того же генератора менее полезна, чем независимые отрицательные пары и внешняя реализация языка: генератор и oracle могут разделять один ошибочный подход.

**Предложение:** LLM предлагает campaign/пару программ, но не назначает себе окончательный expected result. Пара различается одним проверяемым ограничением, ожидается stable diagnostic code/stage. Campaign валидируется по существующим typed contracts до дорогой генерации. Attempts/time/output ограничены. Автор campaign, oracle и holdout должны иметь независимые основания.

**Опыт:** 20 независимо подготовленных пар; затем третий adapter без Wist dependencies и конкретных веток в generic core. Считать distinct confirmed classes на равном CPU/wall-clock budget, а не только generated cases. Включить контрольный дефект, который известен независимому oracle, но не генератору.

**Опровержение:** новый язык потребовал hardcode в core; пара имеет несколько различий; expected result построен тем же lowering, который проверяется. Не объявлять универсальность по двум согласным реализациям. Приоритет P1, НЕ реализовано.

## UT-04. Продавать узкий сценарий управляемых формул

**Бизнес-гипотеза, не подтверждённая рынком:** .NET-команде проще купить решение «изменяемая формула с проверкой и rollback», чем абстрактный универсальный компилятор.

**Техническая основа уже существует:** WistEngine, Validate/TryCompile, typed delegates, ограниченный arithmetic preset. Не предлагать новый engine; не обещать sandbox, стабильность 1.0 или публикацию source candidate на NuGet.

**Пилот:** один неавторитетный numeric score: versioned formula + approved input schema + preview на примерах + audit + rollback. Host сохраняет авторизацию, платежи и side effects. Формулу компилировать после одобрения, а не заново на каждое выполнение. Альтернатива — фиксированный C# или простая конфигурация: она должна участвовать в сравнении.

**Проверка спроса:** 3–5 интервью с владельцами соответствующей .NET-проблемы, затем один ограниченный пилот. Это предлагаемые действия, интервью не проводились. Метрики: время до первой интеграции, объём host glue, цена ошибки, готовность платить за поддержку/интеграцию. Прекратить направление, если конфигурация решает задачу проще. Legacy migration consulting отложить до реального клиентского корпуса. Приоритет P2; размер первого интервью малый, продукта — большой.

## Что не внедрять сейчас

Обязательный LionWeb/PostgreSQL AST store без профилирования; новый DSL тестов вместо PlanFuzz; собственный агентный loop; семантический кэш по одному source text без plan/package/policy identity. Эти изменения расширяют поверхность ошибок без подтверждённой выгоды.

## Источники и переносимость

- Teaching AI Agents to Work with MPS — `rOB4V547hU8`, 12:30–18:45: benchmarks, round trips, возможная регрессия от усложнения tools.
- Context Over Corpus — `xQd8bCu1bj8`, 06:15–11:40: выбор контекста по структуре задачи и reuse primer.
- Lanzer — `oEzJOoCkzb0`, 07:30–12:05 и 19:10–22:30: проверяемые campaigns, отрицательные пары, ограниченный loop.
- Release the mops — `JNoVmvOOwOM`, 05:25–08:20: headless CLI и повторно используемые скрипты.
- LionWeb in the Real World — `942AXKahgpg`, 07:30–12:30: стоимость serialization boundaries.

Локатор видео: `https://www.youtube.com/watch?v=<id>`. Использованы расшифровки, а не визуальный аудит слайдов. ASR содержит ошибки. Чужие демонстрационные числа стоимости/скорости не являются benchmark этого репозитория. Идеи — адаптация, а не обещания докладчиков.
