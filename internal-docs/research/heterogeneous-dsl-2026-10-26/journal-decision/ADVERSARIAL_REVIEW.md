# Критическая проверка решения

**Authority: dated-review, 2026-10-26.** Выполнены отдельные обзоры тремя GPT-агентами: код/доказательства, литература/дизайн, публикационные условия. Затем им переданы подготовленные материалы для критики. Это реальная отдельная модельная проверка, но агенты имеют общий контекст и не заменяют внешнего человека, независимого автора faults, научного руководителя или рецензию журнала.

## Замечания и решения

| Возражение / severity | Проверяемое основание | Изменение и состояние |
|---|---|---|
| **HIGH: baseline может быть реализован раньше freeze tasks** | Первая версия NEXT_SPEC допускала зависимость freeze от уже реализованного M, противореча prospective protocol | Введены R-01a до I-02/I-03 и R-01b после реализации. Повторная отдельная проверка: устранено |
| **HIGH: исключение неверных declarations делает primary outcome удобным для U** | Если missing invalidation/false preservation объявить только robustness material и исключить из main denominator, study не измеряет ошибки сопровождения | RESEARCH_PROTOCOL §2 и NEXT_SPEC сохраняют все назначенные tasks, включая failures, незавершённую работу и ложные declarations. Strata диагностические. Повторная проверка: устранено |
| **HIGH: проверка до consumer не обеспечивает parity с проверкой каждого промежуточного checkpoint** | Нарушение могло появиться и исчезнуть между разными checkpoints | Модель требует одинаковых checkpoints, artifact/context revision и discharge до наблюдаемого решения. Добавлен предел transient-invalid-then-repaired. Не заявляется общая P2/P3 equivalence. Повторная проверка: устранено |
| **MEDIUM: revision validity приписывается существующему scheduler** | Actual ModuleContractVerificationScheduler не содержит универсального revision-aware proof cache | Ответственность явно назначена будущему adapter/runner либо immutable context; такой mechanism не объявлен уже реализованным |
| **MEDIUM: сильный manual baseline вынужденно копирует registry и callback machinery** | Typed pipeline может статически предотвращать невозможные состояния | M разрешено делать это статически при одинаковой информации/checkers; симметрия измеряется по задаче, а не идентичному объёму framework |
| **MEDIUM: raw-first не означает стойкость к любому сбою** | Текущий probe собирает JSON в памяти; flush сам по себе не гарантирует power/disk-loss durability | I-01 — будущая работа. Приёмка ограничена flush-acknowledged observations при process crash/SIGKILL, partial tail помечается, power/disk-loss guarantee отсутствует |
| **MEDIUM: GO и NO-GO относятся к разным объектам** | Local feasibility, external access и superiority могут дать разные ответы одновременно | В §8 протокола явная таблица объектов: GO_LOCAL, INCONCLUSIVE_LOCAL, BLOCKED_EXTERNAL, GO_EXTERNAL_DESIGN, NO-GO_SUPERIORITY, NO-GO_CONTRIBUTION |
| **MEDIUM: RELATED_WORK требовал обязательной победы U для любого вклада** | Значимый отрицательный empirical result не обязан показывать superiority | Требование преимущества ограничено claim о превосходстве; отрицательный результат должен отдельно обосновать важность вопроса |
| **MEDIUM: literature coverage не полностью отвечает новому effort/error RQ** | Основной обзор сосредоточен на compiler semantics/invalidation/fuzzing | Добавлен адресный empirical screening; R-03 требует ближайших empirical comparators и конкретной неизвестности. Новизна этого RQ остаётся неустановленной |

Последние уточнения статусов и адресного literature gap — редакционное разрешение оставшихся замечаний; отдельная третья полная рецензия комплекта не заявляется. Критика не дала основания превращать четыре модельных случая в статистически независимую выборку или устанавливать power без pilot variance.

## Сильнейшие оставшиеся возражения

**Рецензент:** UTS может оказаться удобной .NET упаковкой давно известной invalidation дисциплины. При одинаковых verifier качественный typed baseline может иметь те же outcomes и меньшую полную стоимость. TensorRules демонстрирует concept replication, но не перенос общего scheduler. Без внешней задачи и важного отличия от prior art положительный local result не даёт самостоятельного contribution. Ответ: ограниченный prospective gate; не начинать большую статью с заранее обещанным преимуществом.

**Внешний пользователь:** «Зачем принимать net10.0, adapters, schemas, migration и обучение, если текущий typed pipeline уже решает задачу?» Ответ пока гипотетический: проверить один реальный change/defect case, полную стоимость и повторное использование. Ни интервью, ни внешний demand, ни готовность платить в этой работе не наблюдались. При отсутствии владельца задачи пилот BLOCKED_EXTERNAL, а не объявлен успешным по демонстрации.

**Публикационный reviewer:** применимый документ программы неизвестен; primary ranking full cards частично недоступны; submission receipt CGO27 не найден в доступном material. Registered report stage 1 не означает финальное принятие и зачёт. Ответ: separate states в PUBLICATION_MATRIX, письменный administrative gate, точные metric/year/category и разрешённый overlap до подачи. Тематический shortlist не называется подтверждённым Q1/Q2 маршрутом.

**Методолог:** и данные, и oracle четырёх кейсов предложены моделью, а historical injected faults связаны общими генераторами. Ответ: probe — mechanism diagnostic; все human independence / external generalization / performance claims исключены. Новая cohort требует заранее определённого clustering, разделения development/confirmatory и достаточного внешнего материала.

## Что опровергнет рекомендацию

Если после одинаковых checks, полной стоимости и внешней задачи существенная неизвестность исчезает, основной научный scope закрывается. Если M проще, superiority claim снимается; содержательная отрицательная статья возможна только при обоснованной важности и достаточных данных. Если источник истины не разрешает disagreement, результат INCONCLUSIVE. Ни смена порога после просмотра результата, ни автоматический переход к новому GPU/framework не являются допустимым исправлением.
