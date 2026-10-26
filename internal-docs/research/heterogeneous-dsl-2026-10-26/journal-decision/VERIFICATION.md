# Проверки текущего независимого анализа

**Authority: dated-review / execution receipt, 2026-10-26.** Это receipt данной работы. Исторический [VALIDATION](../VALIDATION.md) и его machine-readable результаты не переписываются и не объявляются повторно исполненными.

## Исходное состояние

Через GitHub API подтверждены open/draft PR [#382](https://github.com/Misha1302/UniversalToolchain/pull/382), head `f4d1dd1de2d2e140437613191008b9cdf2552605`, base/master `de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1` и шесть успешных PR workflows именно head. Это **исходные проверки**, не результаты будущего delivery commit:

| Workflow | Run |
|---|---|
| Architecture Boundaries | [40360550946](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550946) |
| Ownership and Package Verification | [40360550879](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550879) |
| Docs Check | [40360550818](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550818) |
| Package Compatibility Review | [40360550841](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550841) |
| .NET CI | [40360551208](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360551208) |
| UniversalToolchain validation | [40360550982](https://github.com/Misha1302/UniversalToolchain/actions/runs/40360550982) |

Прежняя Fedora worktree была чистой на head. Работа выполнена в новом отдельном detached worktree; незавершённые пользовательские изменения и исходная worktree не перезаписывались. Локальная среда имеет Python/Node/git, но не dotnet; фактическое C# исполнение проведено на доступной Fedora с .NET SDK `10.0.111`.

## Новое фактическое выполнение

| Проверка | Состояние / результат | Предел |
|---|---|---|
| Linked-source diagnostic, исходный проект с явным RepoRoot | **PASS**, exit 0; четыре cases, 8 direct + 12 matched + 2 scheduler observations | Model-designed, не blind human study; build+run duration не performance evidence |
| Source/result integrity | Записаны SHA-256 production TensorRules и scheduler, harness/project/results/verifier в [probe/receipt.json](probe/receipt.json) | Hash доказывает identity bytes, не корректность модели |
| Replay доставляемого portable project | **PASS**, dotnet run exit 0, verify_probe.py exit 0; полный JSON и SHA-256 совпали | Запуск из расположения probe без явного RepoRoot; тот же baseline, среда и границы случая |
| Полный `npm run docs:check` на Fedora | **PASS**, exit 0: status/release-state/readiness, два readiness mutants, links, compiled first-contact (2 snippets), Pages self-test, VitePress build и built-site invariants | Readiness проверяет согласованность historical count 1331, не исполняет 1331 тест заново |
| Независимая критика | **Выполнена**: три отдельных модельных reviewer; существенные замечания исправлены, затронутые положения повторно проверены | [ADVERSARIAL_REVIEW](ADVERSARIAL_REVIEW.md); не внешняя human validation |
| Ссылки и согласованность research-комплекта | **PASS**: отдельный reviewer проверил 10 Markdown files, 118 relative file targets; counts/statuses согласованы | Anchors не проверялись. Автоматический docs check не заменяет этот узкий research gate |

Первый результат `results.json`: SHA-256 `cb0cca960de68749d4b7415e0d1010a3c3ad99325ab99bd89bbd39c0f96aef28`. Исходный C# harness не менялся при переносе; project меняет только default RepoRoot и IsPackable=false. [Команды повтора и scope](probe/README.md) отделены от будущего raw-first recorder I-01.

Полный документационный gate завершён `2026-10-26T17:44:06.948780+00:00`; SHA-256 сохранённого лога `82586b48c84e89ad0c2ce66704af939329e687c82e7d8d0cafcb7a4c9cb61a51`. Он исполнен на всём новом комплекте до замены PENDING полей receipt результатами. `npm ci --ignore-scripts` использовал существующий lockfile; его audit summary сообщил 8 findings (2 moderate, 6 high). Отдельный security audit не проводился, зависимости не менялись. Предупреждения npm config и VitePress chunk size не блокировали gate.

## Источники и ограничения доступа

Обязательные репозиторные материалы и source owners перечислены в [EVIDENCE_INDEX](EVIDENCE_INDEX.md). Более ранние исследования на других ветках использованы по доступным revision-bearing материалам; полного нового replay всех исторических исследований не выполнялось. Большие .NET/CGO/PlanFuzz suites в этом поручении не запускались без отдельного риска; прежние 1331 тест, 12 assertions и прочие historical counts не выдаются за нынешние.

Literature/journal источники проверены 26 октября 2026 через web connector, с primary author/publisher sources. Отдельные ACM/SCImago full pages возвращали 403; использованы доступные первичные индексированные extracts и открытые авторские версии. Full JCR/Scopus export и правила программы не получены. Полнота чтения и конкретные gaps сохранены в RELATED_WORK и PUBLICATION_MATRIX, доступ не обходился.

CompilationLab использован из выборочно прочитанных gate/runtime-core/protocol/bootstrap материалов. У архива проверены 459 manifest entries: без missing/mismatch; gate SHA-256 `56903c252e46275f341ddcefaa8b6175e67ee492fb9c7c1983e18e0418d55d9b`. Это проверка локальных bytes, **не host/provider/runtime attestation**: доверенная host attestation недоступна, VERIFIED runtime status не заявлен.

## Сохранение и scope

Основной комплект сохранён и опубликован коммитом [5fd04ca7](https://github.com/Misha1302/UniversalToolchain/commit/5fd04ca7d3c90024f8d5dfdc3c82beca76f8df84) в существующем draft PR #382. Исходная пользовательская worktree осталась чистой на `f4d1dd1`; отдельная delivery worktree и локальная копия были чистыми на опубликованной ревизии.

CI этой ревизии выявил пропущенную регистрацию нового `.csproj`: [Architecture Boundaries run](https://github.com/Misha1302/UniversalToolchain/actions/runs/40371914170) завершился ошибкой `project has no owner`. Она воспроизведена обычным локальным checker. Исправление — одна точная запись probe в `owners.UNIVERSAL` файла `eng/project-ownership.json`; validator, total coverage и запрет `UNIVERSAL -> WIST_PRODUCT` не ослабляются. Отдельный reviewer проверил все шесть разрешённых `$(RepoRoot)` ProjectReference: они существуют и относятся к UNIVERSAL. Это manual resolution свойства MSBuild; validator сам такие property-valued references не раскрывает.

После исправления локальный `python3 Tools/check-project-ownership.py --root .` завершился exit 0: `PROJECT_OWNERSHIP=PASS UNIVERSAL=58 WIST_PRODUCT=39`. Вторая исходная ошибка workflow Ownership and Package Verification имела ту же причину на ownership step; его отдельный Packed Wist clean consumer job прошёл. Эти результаты относятся к конкретным указанным проверкам, не к будущему статусу всего нового CI.

Во время доставки исправления Fedora отключилась: передача patch подтверждена отдельным completed tool receipt, следующая remote process request завершилась timeout, head GitHub остался `5fd04ca7`. Эта мутация не повторялась вслепую. Исправление доставляется через GitHub tree/commit API с ожидаемым head и обычным fast-forward; состояние remote worktree после timeout неизвестно. Успешные probe/docs execution receipts выше получены до отключения и сохраняют свою силу.

Итоговый scope — 18 файлов досье и одна запись ownership manifest. Production compiler/runtime/ModuleContracts, CGO27 raw evidence/corpora/thresholds и прежние verification receipts не изменяются. Probe не добавлен в product solution или пакеты и не вводит public API; он зарегистрирован в полном repository ownership inventory. Итог нового ownership gate и CI следующей ревизии фиксируется в PR #382. Первый неуспешный run не выдаётся за зелёный.

Ни статья, ни заявка, ни письмо внешнему человеку не отправлялись; принятие, indexing, зачёт и календарная выполнимость не подтверждены. Персональные документы/данные в ветку не добавлялись.
