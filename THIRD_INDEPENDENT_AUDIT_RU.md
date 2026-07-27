# Третий независимый аудит Wist no-legacy

> Исторический snapshot до ремедиации 2026-07-27. Текущий статус и проверяемые результаты находятся в `REMEDIATION_REPORT_RU.md`; числа и release-оценки ниже не являются authority для исправленного дерева.


Дата: 2026-07-26

## Вердикт

Предыдущий `Wist-no-legacy-audited-2026-07-26.zip` отозван. Повторная проверка обнаружила дополнительные ложнозелёные verification-пути: installer мог принять пустой test filter, актуальная документация противоречила current-only контрактам, PlanFuzz продолжал сохранять `compiler.*` evidence IDs, parity matrix ссылалась на непроверяемые имена тестов, а официальные verification-страницы фиксировали устаревшие 1465 тестов.

Исправленный релиз-кандидат использует единый машиночитаемый test-count contract, проверяет точное число успешно выполненных cases по TRX и запрещает возврат обнаруженных stale claims/IDs архитектурными и документационными gate-ами.

## Новые подтверждённые дефекты

| ID | Severity | Дефект | Исправление | Проверка |
|---|---|---|---|---|
| R-13 | Critical | `dotnet vstest --Tests:<missing>` возвращал exit 0, поэтому installer мог принять ноль выполненных integration tests | Добавлен строгий TRX-verifier: файл обязателен, количество cases точное, все outcomes `Passed`, counters согласованы | Negative smoke с отсутствующим test filter; canonical run 1508/1508 |
| R-14 | High | Текущие public/internal docs обещали удалённые legacy adapters, schema readers и manifest fallbacks | Документы синхронизированы с current-only контрактами; documentation checker запрещает точные stale claims | `check_documentation_status.py`, `check_documentation_links.py` |
| R-15 | High | Wist PlanFuzz сохранял публичные evidence IDs `compiler.disabled`/`compiler.ssa-*` | IDs переведены на `cil.*`; adapter version поднята до 0.2.0, generator schema — до v2 | 41/41 PlanFuzz tests; no-legacy source gate |
| R-16 | High | Parity matrix требовала лишь непустые строки в `tests`, поэтому могла ссылаться на несуществующие доказательства | Architecture gate парсит NUnit test methods через Roslyn и проверяет каждую evidence reference | 505/505 core tests |
| R-17 | High | `VERIFICATION.md` и current verification page фиксировали 1465 тестов, а checker охранял устаревшее число | Добавлен `eng/test-counts.json` как единый источник истины для docs и installer; canonical total 1508 | JSON schema/sum gate, exact documentation rows, installer execution total |

## Повторно проверенные ранее исправленные дефекты

R-01—R-12 остаются закрытыми: policy обходы `CSharpInterop`, прямой `CreateSession`, потеря `unsafe-interop`, неканонический preset ID, ложная parity-классификация, session-only smoke, stale build outputs installer-а, divergent baseline, commit false-green, недетерминированный порядок capabilities и неверная package metadata не вернулись.

## Границы утверждений

- Удалён Wist compatibility runtime/API legacy и текстовый `.wistdialect` round-trip.
- Typed LanguagePack использует канонический Wist dialect composer/runtime как единственного владельца исполнения; это не второй независимый execution engine.
- В generic Language SDK остаются planning-only untyped artifact descriptors. Они не являются Wist compatibility fallback и не могут соединяться/исполняться как typed routes.
- Selection equivalence отдельных modules/optimizers не объявляется универсальным semantic proof вне shipped preset compositions.

## Acceptance criteria релиза

Релиз считается готовым только после выполнения из чистой распаковки:

1. `Wist.sln`, `PlanFuzz.sln` и оба sample-проекта: 0 warnings / 0 errors;
2. canonical test plan из `eng/test-counts.json`: ровно 1508 passed, 0 failed/skipped/not-executed;
3. 9/9 NuGet packages, Wist surface `1 compile DLL / 64 runtime DLLs`, два consumer-smoke со значением `42`;
4. documentation/static/no-legacy gates;
5. recursive manifest, ZIP path safety и source-only cleanliness;
6. installer negative/positive smoke, включая отказ пустого filter и удаление stale outputs.
