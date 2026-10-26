# Независимая проверка UniversalToolchain: индекс evidence

**Статус: dated-review, 2026-10-26.** Исходный снимок проверки — `f4d1dd1de2d2e140437613191008b9cdf2552605`. Это индекс наблюдений и границ выводов; он не заменяет текущий архитектурный контракт и не меняет исторические протоколы, знаменатели, пороги или receipts.

Связанные результаты: [независимое решение](INDEPENDENT_DECISION.md), [основной prospective protocol](RESEARCH_PROTOCOL.md), [проверки и ограничения](VERIFICATION.md). Интерпретации ниже относятся к названному снимку. Подтверждение новизны, журнального квартиля, зачёта программой и сроков рассматриваются отдельно.

## 1. Актуальная идентичность репозитория

В этом проходе основной исполнитель получил сведения через GitHub и проверил существующую рабочую копию Fedora; это новые наблюдения состояния, а не пересказ старого PR description.

| Объект | Наблюдение | Что это подтверждает |
|---|---|---|
| Репозиторий | [Misha1302/UniversalToolchain](https://github.com/Misha1302/UniversalToolchain), публичный; основная ветка `master` | Проверяется UniversalToolchain, не TuringSquare-LMS |
| [PR #382](https://github.com/Misha1302/UniversalToolchain/pull/382) | `open`, `draft`, не merged | Материалы исследовательской ветки нельзя считать частью `master` |
| Head PR | `research/heterogeneous-semantic-strategy-20261026`, `f4d1dd1de2d2e140437613191008b9cdf2552605` | Это baseline настоящего чтения кода и нового диагностического опыта |
| Base PR | `master`, `de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1` | Фиксирует проверенную базу сравнения |
| Существующая Fedora worktree | Чистая, HEAD совпал с `f4d1dd1…` | Незавершённые изменения в ней не обнаружены; опыт подготовлен отдельно |
| Новый локальный checkout | Получен из указанной исследовательской ветки, тот же HEAD | Исходники доступны для независимого чтения |
| Проверки head | Все шесть возвращённых PR workflows завершились `success` | Текущая CI-проверка этого head; не новое выполнение всей suite в данной сессии и не научная приёмка |

Шесть CI-свидетельств для `f4d1dd1…`: [Architecture Boundaries](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550946), [Ownership and Package Verification](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550879), [Docs Check](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550818), [Package Compatibility Review](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550841), [.NET CI](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360551208), [UniversalToolchain validation](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550982). Для опубликованного по итогам этого прохода commit требуются собственные результаты, записываемые в [VERIFICATION](VERIFICATION.md).

## 2. Что прочитано и какой у источников статус

| Группа | Источники | Статус использования |
|---|---|---|
| Требование пользователя | [NEXT_ANALYSIS_PROMPT](../NEXT_ANALYSIS_PROMPT.md) полностью, приложенная копия; текущий запрос | Поручение: независимый выбор с обязательной журнальной целью, допустимость сужения/отказа от гипотезы |
| Досье текущей ветки | [README](../README.md), [BUSINESS_AND_RESEARCH_STRATEGY](../BUSINESS_AND_RESEARCH_STRATEGY.md), [RESEARCH_ANSWERS](../RESEARCH_ANSWERS.md), [SPEC_RESEARCH_PLAN](../SPEC_RESEARCH_PLAN.md), [EXPERIMENT_PLAN](../EXPERIMENT_PLAN.md), [VALIDATION](../VALIDATION.md), [verification-results.json](../verification-results.json), [PUBLICATION_REQUIREMENTS](../PUBLICATION_REQUIREMENTS.md) | Прочитано командой в этом проходе. Предыдущие решения — гипотезы; предыдущие runs — исторические receipts |
| Архитектурная власть | [AGENTS](../../../../AGENTS.md), [CURRENT_ARCHITECTURE_STATUS](../../../../docs/CURRENT_ARCHITECTURE_STATUS.md), [DOCUMENTATION_INDEX](../../../policies-and-reports/DOCUMENTATION_INDEX.md), [DOCUMENTATION_RULES](../../../policies-and-reports/DOCUMENTATION_RULES.md), [SYNTAX_OWNERSHIP_RULES](../../../policies-and-reports/SYNTAX_OWNERSHIP_RULES.md) | Текущие ограничения. Композиция принадлежит compiler/plan, runtime исполняет выбранное |
| CGO27 claims и результаты | [CLAIM_EVIDENCE_LEDGER](../../../../CGO27/CLAIM_EVIDENCE_LEDGER.md), [RESULTS_SUMMARY](../../../../CGO27/RESULTS_SUMMARY.md), [REVIEW_FINDINGS](../../../../CGO27/REVIEW_FINDINGS.md), [DEVIATION_LEDGER](../../../../CGO27/DEVIATION_LEDGER.md), [PERFORMANCE_ENVIRONMENT](../../../../CGO27/PERFORMANCE_ENVIRONMENT.md), [SECOND_LANGUAGE_REPORT](../../../../CGO27/SECOND_LANGUAGE_REPORT.md), [ABLATION_REPORT](../../../../CGO27/ABLATION_REPORT.md), [EXPERIMENT_PROTOCOL](../../../../CGO27/EXPERIMENT_PROTOCOL.md) | Прочитаны существующие документы, но исторические workflow artifacts не объявлены заново скачанными и воспроизведёнными |
| Статья и внешняя проверка | [paper/README](../../../../CGO27/paper/README.md), разделы [design](../../../../CGO27/paper/sections/03-design.tex), [methodology](../../../../CGO27/paper/sections/05-methodology.tex), [results](../../../../CGO27/paper/sections/06-results.tex), [related work](../../../../CGO27/paper/sections/08-related-work.tex), [threats](../../../../CGO27/paper/sections/09-threats.tex), [external-fault-kit/README](../../../../CGO27/external-fault-kit/README.md) | Черновик и протокол, не submission/acceptance receipt. Threats уже явно сообщает риск missing invalidation |
| PlanFuzz | [README](../../../proposals/planfuzz/README.md), [implementation-status](../../../proposals/planfuzz/implementation-status.md), относящиеся к baselines/оценке части [ТЗ](../../../proposals/planfuzz/technical-specification.ru.md), генераторы/адаптеры/oracles/worker/replay/campaign code | Реализация отделена от оставшихся proposal stages |
| Владельцы исходников | `LanguageCompiler`, `LanguagePlan`, `LanguageRuntime`, route execution/build; semantic descriptors; SSA route contracts; ModuleContracts scheduler/effects/pipeline observer; TensorRules; CGO27 runner/validation paths | Прочитаны актуальные исходники с baseline выше; ключевые привязки — в §3 |
| Карта более ранних исследований | Q01–Q42 и revision-bearing ссылки в `RESEARCH_ANSWERS.md` | Восстановлена по текущему досье; это не утверждение, что все перечисленные исторические ветки заново checkout/read/run |

CompilationLab использован как методика разделения гипотез, evidence и независимой критики. Проверка целостности доступных архивных байтов не является trusted-host attestation или provider validation научного результата.

## 3. Утверждение → evidence → ограничение → последствие

Номера строк в таблице относятся к baseline `f4d1dd1…`; относительные ссылки ведут к владельцу исходника. «Прочитано» означает наблюдение кода; «исторический результат» означает результат из названного источника; «выполнено» относится только к новому опыту §4.

| Утверждение | Источник и наблюдение | Ограничение | Что меняет в решении |
|---|---|---|---|
| Общий scheduler существует | [ModuleContractVerificationScheduler](../../../../UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractVerificationScheduler.cs), строки 18–62: P2 выбирает request keys, P3 — applicable route keys; неизвестный request/конфликт owner отвергаются | Routing проверяет представленные сведения, не истинность/полноту declarations | Сохраняем механизм; для статьи нужен вклад сверх корректной маршрутизации |
| Wist реально использует scheduler | [ModuleContractPipelineObserver](../../../../UniversalToolchain/UniversalToolchain.ModuleContracts/ModuleContractPipelineObserver.cs), 147–188: structural check всегда, semantic baseline на первом AIR, scheduling на optimized AIR | Конкретные production boundaries и зарегистрированные verifier | Это наиболее сильный существующий implementation evidence |
| `Preserves` гарантирует семантическое сохранение | [PipelineEffectVerifier](../../../../UniversalToolchain/UniversalToolchain.ModuleContracts/PipelineEffectVerifier.cs), 126–156: проверяется доступность preserved fact; requests создаёт `Invalidates` | Код не доказывает, что transformation действительно сохраняет property | Разделить truthful-declaration stratum и robustness к ошибочным декларациям |
| TensorRules показывает общий scheduler в другом языке | [TensorRules Program](../../../../UniversalToolchain/Experiments/UniversalToolchain.TensorRules/Program.cs), 286–294: локальные `fault != "none"` и прямой `TensorVerifier`; [project](../../../../UniversalToolchain/Experiments/UniversalToolchain.TensorRules/UniversalToolchain.TensorRules.csproj) не ссылается на ModuleContracts | Общий scheduler не переиспользуется; факт fault одновременно служит invalidation oracle | Допустимо прежнее C4 «policy distinction evaluated in two packages»; нельзя усиливать до transfer общего scheduler/независимых declarations |
| Проверку незаметно добавляет generic runtime | [RuntimeContracts](../../../../UniversalToolchain/UniversalToolchain.Runtime/RuntimeContracts.cs), 808–839: typed connectivity, выбранные `Transform`, observer dispatch, `Execute` | В этих SDK/runtime paths нет вызова ModuleContract scheduler | Подтверждает границу предыдущей строки; новую интеграцию нельзя считать уже выполненной |
| TensorRules выполняет численный matmul | [Program](../../../../UniversalToolchain/Experiments/UniversalToolchain.TensorRules/Program.cs), 104–109, 241–264: backend возвращает `plan.Output`, например `2x4:row` | Нет численных tensor values/kernels, transfers, synchronization, аппаратных результатов | Не использовать этот опыт как numerical portability/GPU evidence |
| TensorRules fault-owner проверяет реальный реестр owners | Тот же файл, 277–278 и 318: специальная строка `owner-conflict` | Авторский diagnostic fixture, не самостоятельный registry-conflict опыт | Для независимой эволюции нужен настоящий ownership/change scenario |
| P2/P3 эквивалентны вообще | [CGO27 ledger](../../../../CGO27/CLAIM_EVIDENCE_LEDGER.md) C2/C8; [threats](../../../../CGO27/paper/sections/09-threats.tex) | Старое parity относится к фиксированным корпусам; threats уже предупреждает о missing invalidation и ограниченности P3 | Новый miss сохраняется как результат, не переписывает старые числа и не нарушает честно ограниченный claim |
| Пять Wist E2E faults — пять независимых операторов | [EndToEnd Program](../../../../UniversalToolchain/Experiments/UniversalToolchain.EndToEndExperiments/Program.cs), 174, 551–566: один `replace-result` / `cgo27.replace-result-v1` | Пять fault-bearing source cases одного оператора | Единица независимости не равна пяти случаям или числу repetitions |
| Raw записывается до assertions | TensorRules `Program.cs:49,60`; [EndToEnd Program](../../../../UniversalToolchain/Experiments/UniversalToolchain.EndToEndExperiments/Program.cs):97–108; [Cgo27Program.Core](../../../../UniversalToolchain/Experiments/UniversalToolchain.ContractExperiments/Cgo27Program.Core.cs):158–170 | В этих трёх путях scientific/repetition validation предшествует записи raw; это расходится с общей фразой протокола | Первый integrity gate — отдельная prospective raw-first запись с тестом сохранения расхождений. Это не доказательство ошибочности старых PASS результатов |
| PlanFuzz уже ищет по joint configuration/input space | [Wist adapter](../../../../UniversalToolchain/UniversalToolchain.PlanFuzz.Adapter.Wist/WistPlanFuzzAdapter.cs), 68–120; [Acme adapter](../../../../UniversalToolchain/UniversalToolchain.PlanFuzz.Adapter.Acme/AcmePlanFuzzAdapter.cs), 42–123; [campaign](../../../../UniversalToolchain/UniversalToolchain.PlanFuzz.Cli/PlanFuzzCampaignRunner.cs), 41–56 | Генерируется программа; configuration matrices фиксированы: 4 Wist и 6 Acme основных variants | Нужен baseline «тот же generator × fixed matrix × те же oracles»; нельзя приписать search эффект более сильным проверкам |
| Lifecycle schedules уже реализованы | [PlanFuzzTestCase](../../../../UniversalToolchain/UniversalToolchain.PlanFuzz.Core/PlanFuzzTestCase.cs), 8–18, 60–69 | Schedule dimension отсутствует; это согласуется со status document | Lifecycle остаётся резервом после проверки потребности, не обещанной текущей возможностью |
| Backend parity — независимая проверка значения | [BackendParityOracle](../../../../UniversalToolchain/UniversalToolchain.PlanFuzz.Core/BackendParityOracle.cs), 25–38; [comparer](../../../../UniversalToolchain/UniversalToolchain.PlanFuzz.Core/PlanFuzzObservationComparer.cs), 5–19 | Одинаковый неправильный success может пройти; shared failure на valid case отдельно отвергается | При correctness claim нужен независимый oracle и common-mode negative control |
| Каждый PlanFuzz variant работает в новом процессе | [worker command](../../../../UniversalToolchain/UniversalToolchain.PlanFuzz.Cli/PlanFuzzCommandHost.cs), 207–214 | Один fresh worker исполняет все variants testcase attempt | Не смешивать attempt independence и variant isolation; при lifecycle исследовании моделировать их явно |
| PlanFuzz уже имеет полезный evidence pipeline | [ReplayCoordinator](../../../../UniversalToolchain/UniversalToolchain.PlanFuzz.Cli/PlanFuzzReplayCoordinator.cs), 33–57 | Observations сохраняются до oracle evaluation; есть typed replay/fingerprints, но нет controlled superiority evidence | Можно повторно использовать инженерную основу; она не доказывает самостоятельную научную новизну |
| Semantic metadata удостоверяет смысл | [SemanticDescriptors](../../../../UniversalToolchain/UniversalToolchain.Semantics.Abstractions/SemanticDescriptors.cs), 41–60, 145–160: signatures/effects/trust и проверки противоречий | Типизированность, `Pure`, provenance и trust labels не доказывают implementation semantics | Минимальные typed contracts/queries достаточны до доказанного спроса на общий inference service |
| Runtime может выбирать новую композицию | [LanguageCompiler](../../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs), 22–51; [LanguagePlan](../../../../UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlan.cs), 95–130; [LanguageRuntime](../../../../UniversalToolchain/UniversalToolchain.Runtime/LanguageRuntime.cs), 83–143 | Планирование и исполнение имеют разных owners | Ближайшее исследование сохраняет действующие границы |

## 4. Новый выполненный механизмный опыт

**Статус: выполнено на Fedora, exit 0.** Основной исполнитель запустил изолированный harness на исходниках `f4d1dd1…`, .NET SDK `10.0.111`. Сборка и запуск заняли 20.84 секунды; это служебная длительность, **не performance result**.

Исходники: [DiagnosticProbe.cs](probe/DiagnosticProbe.cs), [DiagnosticProbe.csproj](probe/DiagnosticProbe.csproj). Наблюдения: [results.json](probe/results.json). Полная идентичность и команда: [receipt.json](probe/receipt.json) и [VERIFICATION](VERIFICATION.md).

Harness линкует неизменённый TensorRules source и использует настоящий production scheduler. Исторический entrypoint/corpus не изменён. Четыре случая придуманы моделью для проверки механизма; недекларированная порча вводится непосредственно в `TensorPlan`, поэтому это **не полный source-to-result путь Wist или TensorRules**.

Восемь наблюдений существующего `TensorPolicyPass`:

| Случай | Область проверки | P2 selective | P3 always | Независимая проверка |
|---|---|---|---|---|
| `clean-control` | Корректный artifact, invalidation отсутствует | Принимает, 0 вызовов | Принимает, 1 вызов | Shape/output relation корректны |
| `declared-inner-corruption` | Известная порча и корректная декларация | Отвергает `TR-SHAPE-001`, 1 вызов | Отвергает `TR-SHAPE-001`, 1 вызов | Несовпадение внутренних размеров |
| `undeclared-inner-corruption` | Robustness: ложная полнота declarations | Принимает, 0 вызовов | Отвергает `TR-SHAPE-001`, 1 вызов | Несовпадение внутренних размеров сохраняется; P2 miss |
| `unverified-output-corruption` | Negative control покрытия checker | Принимает `unrelated-output`, 0 вызовов | Принимает `unrelated-output`, 1 вызов | Canonical output projection нарушена; оба checker paths пропускают |

Независимая локальная проверка использует relation размеров и projection результата, не вызывает `TensorVerifier`. Она проверяет только эти заданные отношения; это не формальная спецификация всех свойств языка.

Дополнительные **12 matched observations** применяют к тем же четырём артефактам один и тот же `TensorVerifier` и одинаковую информацию об invalidation: production selective, production always и типизированный ручной selective вызов. Ручной baseline совпал с production selective по classification/diagnostic/invocation count во всех четырёх случаях. Это функциональный контроль **одного правила**; он не измеряет трудоёмкость независимо эволюционирующих пакетов, качество широкой ручной интеграции или скорость.

Ещё **2 наблюдения scheduler** показывают при одной applicable route и пустых requests: P2 планирует 0 вызовов, P3 — 1. Итого сохранено 8 + 12 + 2 наблюдения **четырёх model-designed cases**, не 22 дефекта и не 22 независимых опыта.

SHA-256 `results.json`: `cb0cca960de68749d4b7415e0d1010a3c3ad99325ab99bd89bbd39c0f96aef28`. Идентичность файлов опыта фиксируется receipt; она не удостоверяет истинность гипотезы.

### Что этот опыт меняет

- Подтверждает конкретным исполнением уже описанную в manuscript threats зависимость selective policy от completeness declarations.
- Подтверждает, что always-check также ограничен представленными verifier properties.
- Не выявляет в данном сценарии с одним правилом преимущества общего scheduler над прямым typed conditional. Широкий вывод об отсутствии ценности UTS из этого не следует.
- Требует нового независимого эксперимента по изменению/интеграции контрактов, если на этой линии строится journal claim.

### Чем этот опыт не является

Это не новый natural-defect finding, не externally authored blind corpus, не дополнительный независимый язык, не formal proof, не hardware/numerical execution, не full-source E2E, не end-to-end performance benchmark и не измерение customer demand. Он не меняет старые CGO27 denominators. Отрицательные наблюдения сохранены, а не переклассифицированы как infrastructure failure.

## 5. Исторические результаты и правильные единицы счёта

Эти числа прочитаны в существующих источниках; в данном проходе не запускалась заново вся кампания.

| Корпус | Что сообщает источник | Допустимая единица/граница |
|---|---|---|
| Wist boundary | 40 primary instances / 32 shapes; 10 challenge shapes; 100 valid controls на policy; parity P2/P3 | Авторские operator shapes и instances; repetitions не независимые defects |
| Wist source-to-result | 30 cases, 240 raw records; 5 fault-bearing cases, 24 controls, P07 baseline failure | Пять случаев одного replace-result operator; P07 сохранён отдельно |
| TensorRules | 12 cases, 48 policy observations; 8 fault operators; parity 12/12 | Model-authored shape/policy fixtures внутри одного framework |
| Mechanism ablation | 8 минимальных counterexamples и 8 matched controls | Свидетели необходимости механизма для своих fixtures, не репрезентативная популяция ошибок |
| Call reduction | P2 120 против P3 140 verifier calls на соответствующем boundary control corpus: 14.3% | Ниже прежнего frozen headline threshold 25%; не whole-compilation speedup |
| PlanFuzz Phase 1 | 21 violating cases, 2 triage classes, включён regression corpus | Не 21 bug, не доказанные 2 root causes, не clean discovery yield |
| Последующие PlanFuzz smokes | Ограниченные clean/replay/reducer/surface результаты из implementation status | Harness adequacy/non-regression, не equal-budget superiority |

Исторические provider receipts привязаны к иным ревизиям, в частности `acc60612361f240d5bd24f148ea7fa6eb5e1f111` и ablation receipt с собственными head/input commit. Они не превращаются в результаты `f4d1dd1…` из-за чтения соответствующего файла на этой ветке. Существующие receipts, hashes, thresholds и corpus files оставлены неизменными.

## 6. Что остаётся неизвестным

| Неизвестное | Почему существенно | Как закрывается |
|---|---|---|
| Самостоятельная научная добавка против сильной прямой интеграции | Корректный scheduler и авторские counterexamples сами не дают journal contribution | Один bounded prospective protocol, отдельные oracle/selection факторы, внешняя критика |
| Общий scheduler на независимо созданной второй package | Текущая TensorRules policy локальна | Реальная интеграция/изменение чужой package либо честное сужение claim |
| External human-authored faults | GPT-критик не удовлетворяет существующему blind protocol | Только автор вне result-producing process, freeze/import до execution |
| Полная стоимость и performance | Call counts и CI timings недостаточны | Full integration/change/support ledger; end-to-end timing только при отдельной обоснованной задаче |
| Независимые найденные PlanFuzz дефекты и search advantage | Текущий corpus содержит known regressions, фиксированные матрицы и seeded faults | Prospective discovery и matched-budget comparison при доказанной потребности |
| Спрос, покупатель, willingness to pay | Публичный код и научная привлекательность не являются спросом | Внешний problem/pilot owner и сравнение с обычным решением |
| Точное правило программы ВШЭ, дедлайн, нужный статус статьи | Требование Q1/Q2 не определяет базу/год/категорию и eligibility | Письменное правило/фрагмент от пользователя; личный документ не публиковать |
| Фактическая submission history CGO27/manuscript | Каталог и готовый PDF не submission receipt | Receipt или прямое подтверждение автора; до этого unknown |

Индекс поддерживает ограниченное решение и следующий различающий опыт. Он не объявляет ни публикационную готовность, ни журнал/квартиль, ни административный зачёт достигнутыми.
