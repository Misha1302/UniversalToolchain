# Ближайшее ТЗ: ограниченный gate перед журнальным исследованием

**Authority: proposal. Версия 0.1 от 2026-10-26.** Основание: [независимое решение](INDEPENDENT_DECISION.md), [evidence](EVIDENCE_INDEX.md), [protocol A](RESEARCH_PROTOCOL.md). Базовый код `f4d1dd1de2d2e140437613191008b9cdf2552605`. Это единственный backlog ближайшей итерации. Прежние NUM/BACK/CORE требования относятся к условной аппаратной ветке и не активируются этим ТЗ.

## 1. Цель и конечный результат

Получить воспроизводимое **GO / NO-GO / INCONCLUSIVE / BLOCKED** решение: имеет ли смысл независимое эмпирическое исследование UTS contract maintenance относительно сильного typed baseline. Каждый статус относится к конкретному объекту из §8 [протокола](RESEARCH_PROTOCOL.md): local feasibility, external access, superiority claim или самостоятельный вклад. Подготовить конкретный внешний пилот и условный журнальный маршрут. Не требуется доказать заранее, что UTS выигрывает.

Управляемая приёмка — добросовестное сравнение, raw evidence, точные границы и решение. Принятие статьи, дата publication/indexing и зачёт ВШЭ не являются результатами реализации этого ТЗ.

**Предлагаемый cap:** пять рабочих дней исполнителя на local engineering gate после freeze task list. Внешние согласования и review имеют отдельный elapsed time; календарная граница программы неизвестна. Если cap исчерпан, сохраняются результаты, фактические затраты и одно следующее решение; объём не расширяется автоматически.

## 2. Уже завершено в текущем поручении

- Проверены PR #382, исходные head/base и шесть PR workflows именно `f4d1dd1`; локальная Fedora worktree до работы была чистая.
- Прочитаны действующие инструкции, досье и научный корпус; проверены первичные literature/journal sources с явными access gaps.
- Выполнен отдельный linked-source mechanism probe: четыре случая, 8 policy observations, 12 matched observations, 2 scheduler counts. Исходный production code не менялся.
- Зафиксированы новая стратегия, publication matrix и отдельный будущий protocol. Preservation/invalidation assumptions и пределы P3 не объявлены доказанными.

Точное состояние сохранения, focused replay и документационных checks публикуется в [VERIFICATION](VERIFICATION.md). Результаты ниже со статусом NEW/PROPOSED не следует считать выполненными только потому, что этот файл существует.

## 3. Готовая к реализации исследовательская инфраструктура

| ID / статус | Причина / вопрос | Owner / компонент | Зависимости | Конкретный результат | Приёмка и evidence | Условие остановки |
|---|---|---|---|---|---|---|
| **I-01 NEW** | Три historical runner теряют raw при acceptance failure | Research runner owner; отдельный non-packable runner или новый prospective mode, без изменения frozen evidence | Прочитать experiment owners; определить новые output schema и directory | Запись identity и каждой завершённой observation с подтверждённым flush до scientific validation; execution/harness/science status раздельны | Намеренный устойчивый P2/P3 mismatch сохраняется с обеими строками и independent outcome; process crash/SIGKILL оставляет flush-acknowledged записи, незавершённый tail отдельно помечается; power/disk-loss guarantee не заявляется; nonzero verifier verdict не стирает данные. Raw files + tests + hashes | Нельзя обеспечить запись до verdict или предлагается ослабить старый parity gate — gate закрыт |
| **I-02 NEW** | P0/P1 слишком слабы как контроль; probe покрывает лишь одно правило | ModuleContracts experiment adapter owner, отдельный typed baseline | I-01 и **R-01a до реализации M**; freeze проверяемых predicates/boundaries | M с теми же checkers, context, applicability, deterministic behavior и fail-closed handling либо статическим предотвращением невозможных состояний; U/M/A в одном observation schema | Code review подтверждает одинаковую информацию; declared fault/clean/unknown-owner/conflict cases дают объяснимые результаты. Verifier assembly/source hashes и inventory различий | Если baseline лишён checks или принудительно дублирует shared code, comparison не допускается |
| **I-03 NEW** | P3 и backend parity пропускают общую семантическую ошибку | Domain/reference owner и независимый reviewer | R-01a, фиксированный subset и I-01 | Отдельный acceptance oracle для values/errors/effects и scope map | Oracle отвергает типо/shape-сохраняющий неверный результат и совпадающий wrong output двух routes; положительные cases проходят. Контроль не вызывает treatment optimizer | Если oracle повторяет реализацию treatment или истина спорна, соответствующая ячейка INCONCLUSIVE |
| **I-04 NEW** | Marginal LOC и invocation count не измеряют цель | Experiment measurement owner | I-02/I-03 | Полный inventory setup/change/tests/docs/migration; отдельный source→checked-artifact timer без смешения cold/warm | Сумма затрат раскрыта, shared work учитывается симметрично, незавершённые задания сохранены; raw distributions и environment | Нет равных условий/устойчивой среды — runtime cost NOT_MEASURED, без efficiency claim |

Эти изменения касаются evidence infrastructure. Они не требуют нового общего FactStore, DSL parser, hardware backend или публичного compatibility promise. В каждом изменении сохраняются AGENTS и документационные owners. Для будущих behavior changes нужны targeted regressions и применимые repo gates, а не новый полный suite по инерции.

## 4. Исследовательские gates

| ID / статус | Причина / вопрос | Owner | Зависимости | Конкретный результат | Приёмка / evidence | Остановка |
|---|---|---|---|---|---|---|
| **R-00 DONE** | Может ли текущий механизм обнаружить неверные declarations и непроверяемый output? | Текущий reviewer; linked source probe | Baseline f4d1dd1 | Четыре диагностических случая с U/M/A | [probe/results.json](probe/results.json), source hashes, replay command, receipt; считать cases, не «22 ошибки» | Не расширять из этого вывод до human corpus, full E2E или performance |
| **R-01a PROPOSED** | Предотвратить выбор заданий под уже реализованный M | Исследователь и reviewer задачи | До I-02/I-03; существующий код и development evidence доступны | Freeze шести конкретных tasks, origins, predicates/boundaries, outcome model и критериев; затем реализация arms | Неизменяемый manifest датирован до реализации M и сравнительных исходов; known/seeded/external strata разделены | Нет конкретных применимых tasks — INCONCLUSIVE, без притворного freeze |
| **R-01b PROPOSED** | Есть ли различающий вопрос после сильного baseline? | Исследователь и reviewer baseline | R-01a, I-01–I-03 | Исполнение всех уже frozen tasks и matched outcome ledger | Все назначенные tasks входят в primary completion/error denominator, включая missing invalidation, false preservation, failures и отклонения; изменение protocol после freeze получает новую версию | Результат сводится к переименованию callbacks или нет важной external uncertainty — NO-GO broad novelty |
| **R-02 PROPOSED** | Можно ли перенести способ интеграции без локальной имитации policy? | External target maintainer + adapter owner, пока не назначен | Реальная система, разрешённый material, I-02 | Один внешний integration witness с общим production mechanism либо явно отдельный concept replication | Реальные code paths/source identities; complete cost; no hidden planner; независимая оценка acceptance | Копия `if invalidated` не засчитывается как перенос scheduler; ограничения dependencies/net10.0 фиксируются, не скрываются |
| **R-03 PROPOSED** | Достаточен ли будущий study design для выбранного claim? | Научный руководитель/методолог, пока не подтверждён | R-01b/R-02, preliminary variance и адресное сопоставление с эмпирическими работами о contracts/стоимости изменений; publication route rules | Новый prospective protocol, main endpoint, clustering, precision/power, sample/stop/exclusion rules | Новая cohort ещё не запущена; corpus freeze и методика предшествуют outcomes; human authors независимы в объявленном смысле; объяснена неизвестность относительно ближайших empirical comparators | Нельзя получить нужные данные/людей — сузить scope или BLOCKED_EXTERNAL; не заменять human автора GPT |
| **R-04 PROPOSED** | Как результаты изменяют решение? | Владелец исследования и отдельный adversarial reviewer | R-01a/R-01b–R-03 в доступном scope | Claim ledger U/M/A/O и один go/no-go decision | Miss сохраняется; superiority не заявляется без полного сравнения; disagreement reviewer записан | Отсутствие выигрыша не «чинится» сменой frozen threshold; следующий фреймворк автоматически не начинается |

R-03 — gate к publication-scale исследованию, не требование выполнить его внутри пятидневного local cap. Значимый отрицательный результат может сохранить research question, но gate обязан объяснить его научную важность относительно литературы.

## 5. Бизнес и административные зависимости

| ID / статус | Причина / owner | Зависимости | Результат и критерий приёмки | Evidence / остановка |
|---|---|---|---|---|
| **B-01 PROPOSED** | Проверить пользователя и полную цену перехода; владелец проекта + внешний maintainer | Один разрешённый реальный change/defect case, его owner и сильный substitute | Карточка пилота из INDEPENDENT_DECISION: задача, baseline, cost ledger, согласованный до outcome критерий полезности, одно повторное использование | Owner подтверждает результат; слова «интересно» не спрос. Нет owner/material/времени — BLOCKED_EXTERNAL; новая коммерческая платформа не строится |
| **P-01 BLOCKED_USER_DOCUMENT** | Нужен применимый критерий зачёта; пользователь/ответственный программы | Название программы и редактированный фрагмент требования | Точная таблица: rule/version/clause, N, deadline/status, authorship/affiliation, article types, metric/year/category, exclusions | Письменное подтверждение названного журнала/ISSN/type/status. В публичной ветке только обезличенные критерии, без личного документа |
| **P-02 PARTIAL** | Выбрать реальную цель подачи; научный руководитель/автор | P-01 и результат R-01b; полные author/track rules + ranking export | Одна выбранная строка TOPLAS/TOSEM/EMSE и один резерв по конкретному contribution; exact ranking tuple и funding | Matrix сейчас содержит первичные extracts и gaps. Неполный ranking либо иной metric/year не превращается в Q1/Q2 certificate |
| **P-03 BLOCKED_SUBMISSION_STATUS** | Нельзя предполагать прошлую подачу и допустимость overlap; corresponding author | Receipt/status CGO27 либо подтверждение, что подача не совершалась | Disclosure/overlap table, разрешённый статус для новой подачи, manuscript/artifact checklist | Имя CGO27 не receipt; simultaneous substantially overlapping submission не выполняется. Никакие письма/подачи этим ТЗ автоматически не разрешаются |

Запрос недостающих программных данных уже задан в текущем диалоге; ответа пока нет. Это блокирует только административное подтверждение и календарный вывод. I-01–I-04 и R-01a и local часть R-01b можно выполнять без него.

## 6. Требования к архитектуре и сдаче

1. У каждого production behavior change должен быть existing owner; исследовательский adapter не становится вторым runtime planner.
2. Не добавлять в generic core branches по Wist/TensorRules, backend, device или case ID. Имена конкретных cases допустимы только в owned experiment fixtures.
3. Существующие CGO27 corpora, historical results, thresholds, anonymous paper provenance и прежние test counts сохраняются. Новый protocol имеет отдельную identity.
4. Не включать transcript личных документов, персональные данные участников или секреты в публичную ветку. Согласование их публикации отдельно.
5. Sufficient validation: focused tests для затронутого runner/adapter, evidence-retention negative case, documented replay, source/hash checks, relevant architecture/docs gates. Большие кампании запускаются только для конкретного claim.
6. Завершение итерации: чистый reviewable commit/PR, linked evidence index, raw retained results, source/environment identities, effort ledger, reviewer findings/disposition и решение по продолжению.

## 7. За пределами этой итерации

Общий inference engine, persistent proof cache, универсальная ontology, новый grammar workbench, GPU/TPU/NPU backends, automatic heterogeneous partitioning, полный stateful fuzzing engine, публичный SaaS/редактор правил и широкая коммерческая поставка не входят в scope. Возможность рассмотреть их позже зависит от отдельного наблюдаемого недостатка существующих средств.

Ни acceptance, ни publication, ни indexing, ни зачёт программой не обещаются как результат выполнения кода. Ближайший конкретный заказ исполнителю: **сначала R-01a и I-01; после freeze — I-02 и I-03; затем исполнить frozen change tasks (R-01b) с полным inventory; результатом сдачи служит evidence-backed решение, включая NO-GO.**
