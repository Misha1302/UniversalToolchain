# Implementation report

## 1. Краткий итог

Исправления F-01…F-14 внедрены волнами без массового переписывания проекта. Тринадцать findings закрыты со статусом `Fixed`. F-10 имеет статус `PartiallyFixed` намеренно: generic Wist LanguagePack теперь безопасный и проверяемый subset, но не объявляется полной заменой shipped Wist до parity всех presets, legacy modules, optimizers и policies.

Release-верификация: обе solution и оба sample-проекта собраны в Release без warning/error; все шесть test projects прошли — **1504/1504**, failed 0, skipped 0. Проверены 9 NuGet-пакетов, package surface и два clean offline consumer-smoke сценария.

## 2. Baseline и environment

- Baseline commit: `b2058f803a678d673398e834eab88f152f37c4c7`.
- SHA-256 исходного `Wist.zip`: `0c6e4551607d8b21b0455454d5c0c34b23bf3c19eb0d086adb0f9bf884d218ff`.
- Исходный manifest: 1657/1657.
- Debian 13 Linux x64.
- .NET SDK 10.0.301; MSBuild 18.6.4; runtime 10.0.9.
- Offline NuGet package cache применялся для воспроизводимого restore/package smoke.
- Подробности baseline и ограничение первоначального bounded build: `IMPLEMENTATION_BASELINE_RU.md`.

## 3. Изменения по волнам

### Волна A — trust boundary (F-01, F-05)

- Registry выдаёт opaque `LanguagePackageRegistrationIdentity`, которую нельзя правдоподобно сконструировать из id/version/hash.
- `LanguagePlan` создаётся только compiler boundary; canonicalizer вычисляет stable hash, verifier проверяет package identity, manifest digest, selected contributions и routes.
- Wist runtime больше не исполняет `wist.moduleAlias` как сгенерированный DSL. Точный модуль выбирается внутренним typed mapping `WistModuleSelection`.
- Adversarial clone descriptor, foreign/tooling contribution, control chars/newline/backend injection и exact-set mismatch отклоняются.

### Волна B — correctness/lifecycle (F-02, F-03, F-04)

- `Conditions` разделён на comparisons, boolean logic и conditional control flow; старый feature оставлен aggregate shim.
- Module-contract verification имеет validated `Strict/Warn/Off`; Warn требует observable sink.
- Runtime lifetime оформлен state machine; reentrant dispose немедленно отклоняется, concurrent external dispose ждёт общий completion.

### Волна C — semantic stability (F-06…F-09)

- `ExtensibleEnum` использует stable name identity; mutable process-wide registry устранён; добавлен instance-scoped frozen catalog.
- Reflection activation выбирает только exact supported signatures и отклоняет ambiguity.
- Interpreter/CIL используют общий conversion contract и одинаковые failure categories.
- `SetAndList` сохраняет uniqueness и корректно перестраивает индексы после удаления.

### Волна D — migration/architecture (F-10…F-14)

- Опубликованы machine-readable и human-readable parity matrices.
- Static-state guardrail заменён Roslyn syntax analysis по production tree с owner/reason/expiry exceptions.
- Добавлен versioned legacy deprecation registry и migration guide; removal связан с parity gates.
- Runtime profile применяется immutable typed overlay без text/parser round-trip.
- AIR verifier отделяет domain failure от internal implementation failure.

## 4. Статусы F-01…F-14

| Finding | Status | Итог |
|---|---|---|
| F-01 | Fixed | Opaque provenance, metadata не исполняется как DSL, exact selection verified. |
| F-02 | Fixed | Semantic feature split и public SDK parity на interpreter/CIL. |
| F-03 | Fixed | Silent Warn запрещён; diagnostics observable. |
| F-04 | Fixed | Reentrant self-deadlock устранён; lifecycle stress пройден. |
| F-05 | Fixed | Forged executable plan закрыт canonical compiler/verifier boundary. |
| F-06 | Fixed | Process-order identity устранена; scoped frozen catalog. |
| F-07 | Fixed | Deterministic exact constructor policy. |
| F-08 | Fixed | Общая conversion semantics и backend parity. |
| F-09 | Fixed | Set/list uniqueness восстановлена. |
| F-10 | PartiallyFixed | Matrix и removal gate готовы; полная preset/optimizer parity ещё отсутствует. |
| F-11 | Fixed | Roslyn guardrail покрывает production tree. |
| F-12 | Fixed | Versioned deprecation/removal governance. |
| F-13 | Fixed | Typed overlay без повторного parser path. |
| F-14 | Fixed | Internal verifier defects fail closed. |

Полная строковая детализация находится в `FINDINGS_STATUS.csv`.

## 5. Архитектурные решения и зафиксированные инварианты

- **Source of executable truth:** canonical verified `LanguagePlan`, а не DTO/metadata.
- **Capability contract:** selected package/features/contributions/routes/backends должны совпасть с фактически активированными.
- **Persistence/serialization boundary:** identity handle не сериализуется как доверенное доказательство; загруженные данные должны пройти registry/compiler verification заново.
- **Verifier boundary:** expected semantic violation и internal defect — разные типы отказа.
- **Lifecycle boundary:** новая операция после `Disposing` запрещена; owner context не может ждать собственную lease.
- **Identity boundary:** equality/hash не зависит от порядка глобальной регистрации.
- **Migration boundary:** legacy не удаляется по календарю; требуются parity, usage assessment, tests и warning-as-error phase.

## 6. Compatibility impact

- Старый публичный construction path `LanguagePlan` закрыт; callers должны использовать compiler/verifier API.
- Silent `Warn + null sink` больше не принимается.
- `EnumGenerator.GetName(int)` не поддерживает прежнюю insertion-index semantic; migration path — `ExtensibleEnum<TTag>`/`ExtensibleEnumCatalog<TTag>`.
- Ambiguous reflection providers fail fast.
- Invalid runtime conversions больше не возвращают исходное значение молча.
- Legacy runtime/provider APIs сохранены как shims с `[Obsolete]`, stable gate IDs и `removalNotBefore=1.0.0`.

## 7. Выполненные тесты и команды

Release builds:

- `dotnet restore UniversalToolchain/Wist.sln` — exit 0.
- `dotnet build UniversalToolchain/Wist.sln -c Release --no-restore` — exit 0, 0 warnings, 0 errors.
- `dotnet build UniversalToolchain/PlanFuzz.sln -c Release` — exit 0.
- `dotnet build samples/Acme.PricingLanguage/... -c Release` — exit 0.
- `dotnet build samples/Wist.RolloutScoring/... -c Release` — exit 0.

Tests:

| Project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Tests | 506 | 0 | 0 |
| UniversalToolchain.Modules.Tests | 290 | 0 | 0 |
| UniversalToolchain.Dialects.Tests | 589 | 0 | 0 |
| UniversalToolchain.LanguageSdk.Tests | 68 | 0 | 0 |
| UniversalToolchain.PlanFuzz.Tests | 41 | 0 | 0 |
| UniversalToolchain.PlanFuzz.IntegrationTests | 10 | 0 | 0 |
| **Total** | **1504** | **0** | **0** |

Race/order repetition:

- lifecycle filter: 20 iterations × 5 tests, all passed;
- identity order: 10 iterations × 2 tests, all passed;
- lifecycle test class also contains bounded 32-cycle concurrent dispose stress.

Package gates:

- package matrix: 9 packages verified;
- Wist public package surface: 1 compile DLL, 64 runtime DLLs, ceiling 64;
- template consumer and cross-package consumer: restore/build/run passed; both printed `42`.

## 8. Diff review

- `git diff --check` — passed.
- Изменения локализованы в trust boundary, lifecycle, conversion, module contracts, dialect overlay, migration docs и regression tests; массового rename/formatting нет.
- Поиск не обнаружил public constructor `LanguagePlan`, `GetConstructors().First`/`[0]` или Wist runtime execution из `wist.moduleAlias`.
- Runtime и Wist provider вызывают `LanguagePlanVerifier.Verify`.
- Mutable static lookup tables, затронутые finding-ом, заменены frozen/immutable structures.
- Host-level module-contract settings не потеряны: execution override имеет приоритет, иначе используется validated host default.
- Adversarial exact descriptor clone отклоняется благодаря registry-issued identity, а не сравнению самодекларируемых строк.

## 9. Remaining risks / deferred work

1. **F-10:** generic pack не покрывает legacy-only modules, семь optimizers и шесть shipped presets с `Partial` status. Legacy-first runtime остаётся обязательным для них.
2. Plain `dotnet test` testhost для Modules/Dialects/PlanFuzz Integration в этой container-среде иногда зависал без test failure. Изолированные последовательные запуски с `--blame-hang --blame-hang-timeout 45s` завершились успешно. Это отражено как инфраструктурная нестабильность, а не проигнорированный failure.
3. `ExtensibleEnum` migration может требовать изменения downstream кода, который использовал insertion indexes как внешний идентификатор; такой контракт признан небезопасным и не сохранён.
4. Downstream usage assessment legacy API должен быть выполнен перед warning-as-error и removal; registry прямо блокирует преждевременное удаление.

## 10. Artifact verification

Source release сформирован без `.git`, `bin`, `obj`, `artifacts`, `TestResults`, caches, logs, secrets, runtime databases и `CHANGELOG.md`. Предварительный release candidate прошёл ZIP path-safety, проверку единственного top-level directory, рекурсивный manifest (1683 payload-файла), `diff -qr` staging/unpack и полный clean-unpack test gate 1504/1504. Из чистого дерева независимо собраны owner-графы всех шести test projects, `PlanFuzz.sln` и оба sample-проекта — 0 warnings, 0 errors. Aggregate cold `Wist.sln` несколько раз превышал ограничение длительности процесса до вывода summary и поэтому не выдан за clean-build success; production/test source при финализации отчётов не менялся. Детали: `REPRO_RESULTS.txt`.
