# Независимая проверка ближайших работ и предполагаемой новизны

**Authority: dated-review. Дата проверки: 2026-10-26.** Исходная ревизия UniversalToolchain: `f4d1dd1de2d2e140437613191008b9cdf2552605`. Это ограниченный обзор первичных источников для решения о следующем исследовании, а не систематический обзор всей области и не доказательство отсутствия prior art. Публикационный вклад ещё не установлен.

Связанные документы: [решение](INDEPENDENT_DECISION.md), [основной протокол](RESEARCH_PROTOCOL.md), [индекс свидетельств и выполненных проверок](EVIDENCE_INDEX.md). Исторические результаты `CGO27/` остаются отдельным корпусом.

## 1. Вывод, влияющий на выбор

Существующий reverification выбран для ближайшего ограниченного опыта **за наличие исполняемых компонентов; сравнительная стоимость альтернатив не измерена**. Его проверяемый вопрос: даёт ли модель UTS добавочную пользу при интеграции и изменении расширений по сравнению с качественным обычным решением, использующим те же проверки? Уже имеющаяся инфраструктура позволяет проверить эту гипотезу без строительства нового compiler stack.

Нельзя обосновывать новизну только тем, что invalidation приводит к последующей проверке. LLVM и MLIR уже предоставляют отложенное вычисление анализов, сохранение результатов и зависимости инвалидирования. Canonical verifier owner, явная обязанность проверки и граница её исполнения могут быть полезной инженерной абстракцией. Для утверждения о превосходстве метода требуется показать, какую измеримую проблему сильный обычный baseline решает хуже и при каких предпосылках.

Резерв PlanFuzz необходимо сузить. Совместный поиск по программам и конфигурациям, изменение последовательностей passes и композиционные генераторы уже исследованы. Более конкретная предполагаемая разница UTS — наблюдаемая активация providers, сохранение исключённых возможностей, невмешательство независимого расширения и поведение сессий. Последний пункт пока отсутствует в реализации PlanFuzz. Новая комбинация известных механизмов не становится новым результатом только из-за другого API.

## 2. Инфраструктура и проверка семантики

### LLVM и MLIR: сильнейшая обычная альтернатива

Прочитаны разделы *Using Analyses* и *Implementing Analysis Invalidation* [LLVM New Pass Manager](https://llvm.org/docs/NewPassManager.html), включая transitive dependencies и proxy cases. LLVM позволяет сохранять или инвалидировать результаты анализов; документация отдельно связывает усложнение selective invalidation с измеримым выигрышем полной компиляции. Это действующий инженерный baseline, а не отрицательный контроль без нужных проверок.

В [MLIR Pass Management](https://mlir.llvm.org/docs/PassManagement/) прочитаны analysis management, `isInvalidated`, preserving analyses и dynamic pipelines. Анализы вычисляются по запросу и кэшируются; без явного сохранения результат инвалидируется. Зависимые анализы должны учитывать инвалидирование своих зависимостей. Проверка, оформленная как запрашиваемый анализ либо явный boundary check, — серьёзная альтернатива UTS. Это аргумент для сравнения, не выполненное доказательство эквивалентности всех возможностей.

В [MLIR Interfaces](https://mlir.llvm.org/docs/Interfaces/) прочитаны motivation, dialect interfaces и generic inliner example. Интерфейсы позволяют писать общий анализ без ветвления по конкретным dialect names. Поэтому «старый consumer понимает новое расширение через известный семантический интерфейс» само по себе не новое архитектурное свойство. Для независимой эволюции DSL нужны измерения полной стоимости: descriptors, adapters, schemas, tests, documentation и migration.

### First-Class Verification Dialects for MLIR

Fehr et al., PLDI 2025, DOI [10.1145/3729309](https://doi.org/10.1145/3729309); [авторский полный текст](https://users.cs.utah.edu/~regehr/papers/pldi25.pdf). Изучены существенные части §§4–5 и §6.1: semantic dialects, memory/poison/undefined behavior, translation validation, rewrite checking и проверка transfer functions.

Семантика задаётся преобразованиями в специальные dialects, затем в SMT; поверх неё строятся общие инструменты проверки. Арифметическая оценка ограничена integer operations без control flow, поэтому работа не означает решение произвольной численной или конкурентной верификации. Но она исключает позиционирование UTS как ответа на отсутствие переиспользуемой semantic verification в экосистеме MLIR. Маршрутизация metadata существенно слабее проверки сохранения семантики. Разница должна относиться к координации существующих heterogeneous checks или их сопровождению и быть отдельно показана.

### Alive2 и PEQC-MLIR

Lopes et al., *Alive2: Bounded Translation Validation for LLVM*, PLDI 2021, DOI [10.1145/3453483.3454030](https://doi.org/10.1145/3453483.3454030); [текст](https://users.cs.utah.edu/~regehr/alive2-pldi21.pdf). Прочитаны refinement relation, UB model, intra-procedural scope и §7 про bounded loops. Alive2 проверяет отношение уточнения исходной и преобразованной функций; ограничение числа развёрнутых итераций допускает пропуск ошибок за пределом. Его нельзя считать универсальным oracle, но сравнение с ним показывает различие между semantic preservation и проверкой выбранных инвариантов UTS.

Tucker et al., *Practical Formal Verification for MLIR Programs*, [arXiv:2605.01124](https://arxiv.org/abs/2605.01124), preprint от 2026-05-01; [текст](https://arxiv.org/pdf/2605.01124). Изучены §2, существенные части §3, §4.3 и ограничения преобразования AIR в §5. Hybrid concrete/symbolic interpretation строит нормализованные computation DAGs и проверяет выходные состояния для ограниченного класса программ, включая синхронизацию. Правила дополнительной нормализации должны сохранять семантику. Это близкая работа для численной переносимости и изменения schedules/storage, но не свидетельство наличия такой проверки или численного исполнения в UTS. Preprint не объявляется подтверждённой журнальной публикацией.

## 3. Тестирование компиляторов и композиции

### Ratte

Yu, Wu, Donaldson, ASPLOS 2025, DOI [10.1145/3676641.3716270](https://doi.org/10.1145/3676641.3716270); [авторский текст](https://www.doc.ic.ac.uk/~afd/papers/2025/ASPLOS-Ratte.pdf). Изучены generation principles, композиция interpreters, §3.4 oracles, supported operations и §§4.1–4.3.

Ratte сочетает модульные semantics и generators, инкрементально строит программы без UB и использует reference interpretation/differential execution. Поддержка ограничена конкретными operations и dialect subsets. В работе отдельно считаются bugs и generated tests. Для UTS это прямое ограничение широкой формулы «семантика расширений плюс fuzzing». Нужны собственная задача и oracle; название нового общего semantic service не составляет содержательной разницы.

### SynthFuzz и TargetFuzz

Limpanukorn et al., *Fuzzing MLIR Compilers with Custom Mutation Synthesis*, ICSE 2025, DOI [10.1109/ICSE55347.2025.00037](https://doi.org/10.1109/ICSE55347.2025.00037); [текст](https://web.cs.ucla.edu/~miryung/Publications/SynthFuzz_ICSE2025.pdf). Прочитаны custom mutation synthesis, context matching, evaluation design и threats. SynthFuzz выводит параметризованные мутации из тестов, учитывая контекст; сравнивается с Grammarinator, MLIRSmith и NeuRI на нескольких MLIR projects. Четырёхчасовой бюджет coverage experiments и выбранные проекты ограничивают обобщение. Автоматическая адаптация мутаций к новым dialects уже занята prior art.

Zhou et al., *Targeted Testing of Compiler Optimizations via Grammar-Level Composition Styles*, [arXiv:2512.04344v1](https://arxiv.org/html/2512.04344v1), preprint от 2025-12-04. Прочитаны §3 и §§4.1–4.4. Работа восстанавливает структурные отношения между конструкциями программ и сравнивает targeted pass fuzzing с pipeline fuzzing. Используются optimization-specific corpus, preparation pipelines и согласованные seed corpora для baselines. Разные harnesses и бюджеты двух режимов не следует напрямую сопоставлять. Для UTS это дополнительное ограничение novelty через «композицию программ ради взаимодействия optimizations», не универсальная замена тестированию lifecycle.

### Optimuzz и DESIL

Kwon et al., *Optimization-Directed Compiler Fuzzing for Continuous Translation Validation*, PLDI 2025, DOI [10.1145/3729275](https://doi.org/10.1145/3729275); [текст](https://prosys.kaist.ac.kr/publications/pldi25.pdf). Изучены target-branch selection, guided mutation, §§5.1–5.2. Поиск направляется к оптимизациям, а oracle предоставляют существующие Alive2/TurboTV. LLVM benchmark отобран по detectability применяемым validator; TurboFan baseline адаптирован к поддерживаемой семантике. Урок для PlanFuzz: одинаковые oracles и supported scope необходимы, чтобы приписать результат поиску.

Suo et al., *DESIL: Detecting Silent Bugs in MLIR Compiler Infrastructure*, OOPSLA2 2025; [авторский текст](https://xgdsmileboy.github.io/files/paper/oopsla25.pdf). Прочитаны §3, §4.5 и threats. DESIL устраняет UB в generated programs, строит lowering paths и сравнивает исполнения после различных optimizations. Расширение набора проверок может уменьшить finding yield из-за затрат. Работа уже занимает часть постановки «варианты lowering плюс реальное исполнение». Её differential oracle использует общий compiler; независимый reference interpreter остаётся иной границей доверия. DOI здесь не переписывается из непроверенной сторонней карточки.

## 4. Совместный поиск: обязательные baselines PlanFuzz

**ConfigFuzz:** Zhang et al., TOSEM 2023, DOI [10.1145/3580597](https://doi.org/10.1145/3580597); [авторский текст](https://slaterlatiao.github.io/docs/configfuzz.pdf), [страница автора](https://personal.utdallas.edu/~swei/publications/fuzzing-configurations-program-options-tosem/). Изучены §4 grammar/stub/expanded input и разбор разнородных результатов §5. Grammar описывает options и связи dependence/conflict; stub преобразует префикс fuzzable input в configuration, остальные байты — в data. Оба пространства изменяются совместно. Default/pairwise baselines иногда выигрывают; больше комбинаций не гарантирует лучший результат.

**Tzer:** Liu et al., *Coverage-Guided Tensor Compiler Fuzzing with Joint IR-Pass Mutation*, OOPSLA1 2022, DOI [10.1145/3527317](https://doi.org/10.1145/3527317); [текст](https://arxiv.org/pdf/2202.09947). Изучены §3.1 paired seed pool/interleaving, oracle и component/random-joint ablations. Tzer совместно изменяет TVM IR и pass sequences, сохраняя пары и управляя чередованием. Это более прямой конкурент program/plan search, чем одни command-line options. Для PlanFuzz недостаточно сравнения только с program-only и pairwise configuration selection.

**VAFuzz:** Ahmed, Dev, Wei, *Variability-Aware Fuzzing*, ICSE 2026; [официальная программа](https://conf.researchr.org/details/icse-2026/icse-2026-research-track/188/Variability-Aware-Fuzzing), [авторский текст](https://chaoticcross12.github.io/papers/Variability-Aware%20Fuzzing.pdf). Прочитаны §§3.1–3.3, ablations и threats. Presence-condition queue и связь configuration–seed используют обратную связь совместных исполнений. Сравниваются ConfigFuzz, ZigZagFuzz и sampling; присутствуют проигрышные targets. Следовательно, требуются равные бюджеты и вариативность между независимыми запусками.

Библиографическая оговорка: author PDF VAFuzz содержит DOI `10.1145/3744916.3773247`, соответствующий другой статье. [Карточка publisher по названию](https://dl.acm.org/doi/10.1145/3744916.3773230) указывает `10.1145/3744916.3773230`. При финальной библиографии использовать проверенную карточку, не копировать ошибочный номер из PDF.

## 5. Что именно проверять дальше

| Утверждение | Решение обзора | Различающее свидетельство |
|---|---|---|
| P2 лучше P0/P1 | Объясняет роль checks, не сильное comparative claim | Те же predicates в качественном typed/manual baseline |
| Canonical owner и boundary obligations облегчают расширения | Кандидатная инженерная гипотеза | Независимые change tasks; ошибки, время и полная стоимость сопровождения |
| Меньше verifier calls означает ускорение | Не установлено | Полная компиляция, allocations, scheduler cost, cold/warm cases и неопределённость |
| P2 равно P3 | Только corpus-bounded observation | Явные предпосылки; отдельные missing invalidation/false preservation strata |
| Program × plan fuzzing — новизна | Недостаточно после Tzer/ConfigFuzz/VAFuzz | Особый oracle/lifecycle property и controlled comparison |
| Metadata доказывает premises | Неверно | Независимая проверка фактической семантики |

Сильный обычный baseline должен иметь те же owner/route checks и ту же полноту predicates. Возможное сопоставление — fact ID с typed analysis key, dependencies с invalidation dependencies, boundary obligation с явным запросом проверки. Это проект сравнения; он не доказывает полной выразительной эквивалентности frameworks.

Четыре случая текущего probe, описанные в [индексе evidence](EVIDENCE_INDEX.md), соответствуют этой постановке: P2 совпадает с typed/manual checker на проверенных случаях; missing invalidation позволяет P2 пропустить нарушение, обнаруженное P3; нарушение вне проверяемого свойства пропускают оба. Исполнение принадлежит основному verification receipt. Эти случаи не являются внешним blind corpus и не доказывают общую эквивалентность.

Следующий опыт должен отделить корректные declarations от ошибочных. При корректных сравниваются detection, localization и total cost. При ошибочных фиксируется граница доверия, без исключения неудобных misses. Для PlanFuzz отдельно варьируются search и oracle; если выигрыш исчезает после выдачи baseline тех же checks, вклад следует приписать checks. Независимые repeated seeds не заменяют независимые системы, human authors или root causes.

Положительный результат может обосновать исследование extension maintenance либо узкий assurance mechanism. Отрицательный оставляет UTS полезной инженерией, но останавливает широкий superiority claim. Публикационная пригодность отрицательного результата также требует содержательного вопроса и достаточной внешней валидности.

## 6. Поиск, полнота чтения и ограничения

Поиск выполнен 2026-10-26 через `system1_search_query`; затем открывались primary fulltexts и нужные method/evaluation passages. Указание полного текста означает доступ к документу, **не чтение каждой страницы**. Объём фактически изученных разделов указан выше; чужие experiments и artifacts здесь не воспроизводились.

| Поисковые запросы / группа | Причина включения |
|---|---|
| `"Ratte" "Fuzzing for Miscompilations" pdf`; `"First-Class Verification Dialects" MLIR` | Modular semantics и повторное использование verification |
| `"Optimuzz" compiler paper pdf`; `"SynthFuzz" compiler paper` | Разделение генерации, поиска и oracle |
| `"Fuzzing Configurations of Program Options" pdf Zhang` и author/DOI queries | Joint configuration/input baseline; прочитан метод |
| `"Coverage-Guided Tensor Compiler Fuzzing with Joint IR-Pass Mutation" pdf` | Прямое пересечение с program/plan search |
| `"Variability-Aware Fuzzing" ICSE 2026` | Более актуальный configuration-aware comparator |
| DESIL и TargetFuzz по follow-up ссылкам | Lowering paths и targeted composition testing |
| `"Practical Formal Verification for MLIR Programs" pdf` | Ограничения numerical equivalence/schedule claims |
| `compiler "invalidation" "verification" "2026" "contract"`; `compiler "selective verification" OR "reverification" paper` | Дополнительный поиск близких работ, фильтр 730 дней; отсутствие релевантного результата не доказывает новизну |

Свежие запросы с фильтром 365 дней также выявили **LOOL**, TOSEM 2026, DOI [10.1145/3807508](https://doi.org/10.1145/3807508), и **OptFuzz: Enhancing Compiler Testing via LLM-Powered Compilation Option Generation**, DOI [10.1145/3817602](https://doi.org/10.1145/3817602). Их недоступные в этом просмотре recent full methods не объявляются прочитанными. Они остаются обязательными screening leads, если будет выбрана пересекающаяся search-гипотеза. OptFuzz 2026 отличается от одноимённой работы USENIX Security 2024 про JavaScript JIT. Предшествующий registered report LOOL также нельзя подменять завершённой статьёй.

Локальная HTTP-загрузка была недоступна; использован web connector. ACM fulltext на отдельных запросах возвращал 403, поэтому применялись открытые авторские версии; доступ не обходился. При чтении репозитория изучены AGENTS, current architecture, documentation rules, CGO27 ledger/results/design/related-work/threats и PlanFuzz implementation status. Прочие части стратегии использованы по релевантным фрагментам. Правила программы ВШЭ, квартиль и срок зачёта из этой литературы не выводятся.

## 7. Адресный screening нового вопроса о сопровождении

Поворот к effort/error также требует prior-art проверки. Дополнительный ограниченный поиск 2026-10-26 отобрал две первичные работы; он не устанавливает новизну RQ.

**Близкий эмпирический comparator:** Thüm et al., *Product-Line Maintenance with Emergent Contract Interfaces*, SPLC 2016, DOI [10.1145/2934466.2934471](https://doi.org/10.1145/2934466.2934471), [авторский PDF](https://github.com/SoftVarE-Group/Papers/blob/main/2016/2016-SPLC-Thuem.pdf). Прочитаны abstract, §3.4, §§4–5 полного текста, полученного через GitHub connector. Контрактный интерфейс показывает условия для зависимых features при изменении assignment; baseline показывает структурные зависимости. Из 25 студентов анализируются 22: три исключены из-за невосстановимых измерений времени. Четыре задания на двух Java product lines проверяют понимание допустимых диапазонов. Авторы сообщают меньше ошибочных ответов, без статистически подтверждённого выигрыша времени. Это повторные попытки ответа, не число независимых defects. Contracts предполагаются уже заданными; полная цена их создания и реальная реализация изменений не измерены. Работа непосредственно ограничивает широкое обещание «контракты облегчают независимые изменения», но не сравнивает UTS с matched typed pipeline.

**Косвенный методологический comparator:** Shamshiri et al., *How Do Automatically Generated Unit Tests Influence Software Maintenance?*, [авторский PDF](https://www.evosuite.org/wp-content/uploads/2018/04/PID5238857.pdf), [replication page](https://www.evosuite.org/experimental-data/maintenance/). Прочитаны abstract и §III полного текста, включая selection, procedure, adjudication и threats; DOI/venue в этом screening отдельно не подтверждались. В исследовании 75 участников и 150 заданий на двух Java classes; отдельно измеряются диагноз причины, правильность исправления и время. Проверки бывают ручными и сгенерированными. После неверного диагноза участнику сообщают нужный тип исправления, что ограничивает перенос результата на естественный workflow. Для UTS полезны разделение ошибок implementation/oracle и independent adjudication; это не прямой comparator scheduler и не доказательство его эффективности.

**Следствие для R-03:** сам вопрос о пользе contracts для maintenance уже исследовался. Остаётся проверить конкретную неопределённость: меняет ли способ координации одинаковых verifier частоту ошибок интеграции и полную цену независимо заданных изменений, когда оплачены declarations, adapters, обучение и последующее сопровождение? Ни эти две работы, ни нынешний probe этого для UTS не устанавливают. До confirmatory freeze нужны пригодные внешние задачи, участники и сопоставление с ближайшими эмпирическими методами; отсутствие точного совпадения в ограниченном поиске не является novelty evidence.

Search log этого дополнения: четыре `system1_search_query` — `"design by contract" "controlled experiment" software maintenance`; `"extensible" "framework" "empirical study" "maintenance" developers effort`; `"Product-line maintenance with emergent contract interfaces" pdf`; `"A Controlled Experiment on the Impact of the Object Constraint Language" pdf`. Последний lead не включён как прочитанный comparator: publisher page не предоставила доступного текста. Дальнейших поисковых запросов в этом screening не выполнялось.
