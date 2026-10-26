# Проверки и границы выполненной работы

**Статус: dated-review, 2026-10-26.** Исходный Git baseline: `de0da4a81b1ce38fc296f9de7b1e9ad10515f9a1`. Исследовательский вывод находится в [README](README.md); будущий аппаратный опыт — в [EXPERIMENT_PLAN](EXPERIMENT_PLAN.md).

## Изменения кода

### Противоречивые effects

В `SemanticDescriptorSet.ValidateCallable` условие `IsPure && Count > 1` было недостижимо: effects дедуплицируются, а `IsPure` истинно только для пустого набора или набора из одного `Pure`. Проверка заменена на явное наличие `Pure` рядом с другими effects. Три регрессии проверяют сочетания с `MayThrow`, `WritesMemory`, `UnknownExternalEffect`; положительные случаи сохраняют default/empty/pure и корректные effectful descriptors.

Это дефект validation contract, а не обнаруженный silent wrong-code: прежние optimizer checks уже считали mixed summaries effectful.

### Использования SSA-параметров за пределами блока

Удаление мёртвых block parameters проверяло использования только внутри определяющего блока, хотя dominance contract разрешает использование в доминируемых блоках. Две новые регрессии показывают удаление живого параметра, используемого в downstream return/transfer, и появление `ssa.value.undefined`.

Исправление переиспользует существующий `SsaUseDefMap` для всей функции после удаления инструкций. Fixed-point cleanup продолжает удалять действительно мёртвые параметры, transfer arguments и producers. Новые тесты проверяют валидность исходного SSA, сохранение живых cross-block uses, удаление dead sibling и валидность результата.

Обычный pipeline верифицирует выход прохода. Под `Prefer` такой typed failure может привести к диагностируемому fallback; под `Require` — к отказу. Не утверждается, что невалидный SSA молча исполнялся.

### Документация

- `ssa-coverage-matrix.md` теперь описывает фактический typed fallback при lowering/optimization/emission failures, включая существующий regression для invalid optimizer output. Политика runtime не изменена.
- Callable owner document уточняет отказ при mixed `Pure`.
- В `AGENTS.md` исправлены ссылки на уже перемещённые policy files; содержательные правила не менялись.
- Exact test manifest увеличен на семь случаев; текущие count projections обновлены без переписывания исторических workflow receipts.
- Новый research dossier связан с существующим Ideal UT index, ideas и experiments; прежние исследования сохранены.

## Новые выполненные проверки

Среда: Fedora Linux x86_64; SDK `.NET 10.0.111`; изолированная worktree исходного baseline. Основные пользовательские checkout и незакоммиченные материалы не изменялись. Аппаратное обнаружение показало Intel Core i9-13900HK и Iris Xe; это инвентаризация, а не проверка GPU compiler/runtime.

| Проверка | Наблюдаемый результат | Что подтверждает |
|---|---|---|
| Baseline + только новые test-file изменения | 24 случая: 19 passed, **5 failed**, 0 skipped | Три mixed-effect и две cross-block DCE регрессии действительно воспроизводятся |
| Оба source fixes + полный Core project | **507 passed**, 0 failed, 0 skipped | Исправления и существующие Core regressions прошли в названной среде |
| Канонический integration entrypoint с `--skip-docs --skip-pack` | **1331 passed**, 0 failed, 0 skipped; 15 contract entries; exit 0 | Wist/PlanFuzz builds, полный exact-count набор, topology/runtime, retired-surface/hardening и соответствующие mutant checks |
| `npm run docs:check` после `npm ci` | PASS: 121 public / 57 internal Markdown files; 104 navigated targets | Status/release/readiness, links, compiled first-contact snippets, VitePress build и Pages invariants/selftest |
| Markdown bash smoke | Exit 0; все разрешённые blocks выполнены успешно | Найдены 53 блока; существующие `ci-run=false` остаются пропущены по политике, а не засчитаны как исполнения |
| Прежний пяти-проектный C# control | **12/12 decision assertions**, exit 0 | Ограниченный known-query пример повторяется без общего semantic engine |
| Source checksum index C# control | Все 10 исходных файлов совпали | Повторно исполнялся опубликованный исходный контроль |
| `git diff --check` | PASS | Отсутствуют patch whitespace errors |

Точные per-entry test counts, имена baseline failures, SHA-256 изменённого кода/тестов и fingerprints исходных TRX/logs сохранены в [verification-results.json](verification-results.json). Это receipt наблюдавшегося локального прогона, не заявление о пройденном GitHub Actions run. Raw logs с machine-specific paths не публикуются; их hashes идентифицируют прогон, но сами по себе не доказывают его независимость.

В npm/VitePress выводе были неблокирующие предупреждения о пользовательском npm config `python` и размере JS chunk. Сборка сайта и финальная проверка Pages завершились успешно.

## Повтор C# decision control

Источник: [static-csharp-baseline @ a1e5f25b](https://github.com/Misha1302/UniversalToolchain/tree/a1e5f25bf0afd7850075ed4aeafc8b2a48c6c99d/internal-docs/research/ideal-ut-2026-10-08/controls/static-csharp-baseline). Проверен `SOURCE_SHA256.txt`; запущен `dotnet run --project Host/Host.csproj -c Release --ignore-failed-sources` из изолированного извлечения.

SHA-256 нового полного stdout/stderr log: `f9ca3d06d8783ac59627e88d5b777d3f94fb44f44796bd02c82888565ccb914b`, совпадает с опубликованным историческим выводом. Исторический SDK был `10.0.301`; новый прогон использовал `10.0.111`. Поэтому это повтор поведения на другом SDK, а не тождественное воспроизведение всей исторической среды.

Контроль проверяет boolean decisions с заранее известным `ISafeIndexQuery`. Он не удаляет machine bounds checks, не запускает UT/MLIR, не удостоверяет executable SHA и не проверяет hostile plugins. Двенадцать успешных проверок не закрывают полный E1.

## Как воспроизвести регрессии

На изолированной worktree baseline применить только изменения двух test files из этой ветки; затем выполнить:

```text
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --filter 'FullyQualifiedName~Tests.Ssa.SsaCallableDescriptorTests|FullyQualifiedName~Tests.Ssa.SsaDeadBlockArgumentEliminationTests'
```

Ожидаемый baseline результат — пять named failures и 19 passes. На полной исправленной версии Core запускается без filter. Канонический integration entrypoint:

```text
./build.sh --jobs 4 --skip-docs --skip-pack
npm ci --no-audit --no-fund
npm run docs:check
python3 .github/scripts/run-markdown-bash-blocks.py
```

Release packaging и публикация пакетов являются отдельными baseline-bearing gates; в этой работе они не выполняются.

## Что не проверено этой работой

- GPU/TPU/NPU numerical execution и его performance.
- Любой новый MLIR/IREE/ILGPU backend внутри UT.
- Интегрированный semantic evidence service и полная E1 bounds-elimination задача.
- Формальная корректность всех transformations или сторонних providers.
- Производительность/стоимость новых расширений против matched direct frontend.
- Все исторические эксперименты репозитория: повторён только явно названный C# decision control; остальные результаты используются как revision-bound historical evidence.
- Готовность к release, публикации NuGet или слиянию в `master`.

Зелёный набор регрессий и корректная исследовательская постановка не превращают предложенную аппаратную специализацию в реализованную функцию.
