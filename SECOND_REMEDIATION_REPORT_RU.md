# Вторая ремедиация Wist после независимого повторного ревью

Дата: 2026-07-27.

## Baseline и scope

Исправления выполнены относительно ранее выданного source-архива:

- файл: `Wist-no-legacy-remediated-2026-07-27.zip`;
- SHA-256: `cf176cc0999e52886e18ecb56b8ec27a6ef318949b7b8295477be8508c89c780`;
- package baseline: `UniversalToolchain.Wist` `0.1.0-alpha.2`;
- immutable public API snapshot SHA-256: `0690f0d3d665e849319b0661db815cceccba1dc4f7b7ae4f0db81f53d8687fc6`.

Новая package version — `UniversalToolchain.Wist` `0.1.0-alpha.3`. Версия увеличена, потому что изменились runtime/package bytes и наблюдаемые boundary-контракты. `UniversalToolchain.Wist.LanguagePack` увеличен до `0.3.0-alpha.2` из-за изменения package contents и добавления README.

## Закрытые findings

### WIST-RR-001 — exact test contract

`build.sh` и `build.ps1` исполняют `eng/test-counts.json` через `Tools/run-test-contract.py`. Для каждого entry применяются отдельный process timeout, отдельный TRX и точная проверка passed/failed/skipped. Gate дополнительно запускает четыре отрицательных mutants: count drift, удалённый result, skipped outcome и реальный timeout.

### WIST-RR-002 — managed assembly substitution

`Tools/check-wist-package-surface.py` больше не проверяет только путь и CLR metadata. Runtime DLL должны побайтно совпадать с trusted runtime build output, а `ref/UniversalToolchain.Wist.dll` — с compiler-generated reference assembly. Identity-swap mutant отклоняется до consumer execution.

### WIST-RR-003 — preset semantic substitution

Все девять preset-файлов привязаны к reviewed SHA-256 в `eng/wist-package-surface.json`. Clean consumer выполняет отдельные semantic vectors: variables, grouping, safe math, parameter bridge, restricted/trusted CLR interop, supported/default backends и отрицательные capability checks. Подмена `ssa` на `full-default` отклоняется structural gate и дополнительно различается runtime smoke.

### WIST-RR-004 — реальный API diff

Текущий `PublicAPI.Shipped.txt` сравнивается с snapshot предыдущего релиза. Snapshot привязан к baseline source SHA и собственному SHA через `eng/wist-api-baseline.json`; каждый exact added/removed symbol обязан иметь строку в `eng/wist-api-deltas.csv`. Gate убивает как одиночное изменение current API, так и согласованную подмену current+baseline без изменения предыдущего release provenance.

### WIST-RR-005 — collectible ALC lifecycle

Одноразовый `BasicCoreImpl.Run` больше не сохраняет `PreparedExecution` в `AsyncLocal`. `WistEngine.Dispose` до disposal host удаляет собственные ссылки на host и runtime-boundary. Regression coverage проверяет пять ранее leaking presets, engine, остающийся достижимым после Dispose, и восемь последовательных isolated contexts.

### WIST-RR-006 — CLR numeric parameters

`WistRuntimeBoundary` преобразует CLR numeric values и delegate declared types в numeric representation выбранного runtime до binding/compilation. `Evaluate`, `Validate`, `Compile` и `TryCompile` проверяются на CIL и interpreter для NumbersModule presets; package smoke повторяет параметризованные контракты для всех подходящих shipped presets.

### WIST-RR-007 — implementation value leakage

`WistResultConverter` сначала нормализует implementation-owned значение и только затем выполняет generic cast. Проверяются `object` и `IConvertible` return targets, Evaluate/Compile, оба backend-а и точный `System.Double` для NumbersModule profiles. Ни одно возвращённое значение не принадлежит collectible runtime assembly.

### WIST-RR-008 — LanguagePack README

`UniversalToolchain.Wist.LanguagePack` содержит reviewed README и `PackageReadmeFile`; package-matrix проверяет его наличие в финальном `.nupkg`.

## Дополнительный дефект, обнаруженный во время ремедиации

Усиленный semantic package smoke показал, что trusted `full-default` декларировал CLR interop, но `System.Math.Sqrt(16.0)` не принимал `RealNumberImpl`. Исправление помещено в canonical conversion/type-resolution layers:

- exact implicit user-defined conversion resolver;
- runtime argument conversion;
- intrinsic type compatibility;
- deterministic CLR method resolution;
- двунаправленные implicit conversions `RealNumberImpl ↔ double`.

Контракт проверяет `System.Math.Sqrt(16.0) + 1.0 == 5.0` на CIL и interpreter, включая переход internal → CLR → internal.

## Каноническая definition of done

Релиз считается проверенным только при одновременном выполнении:

1. clean restore/build с `0 warnings / 0 errors`;
2. точной матрицы **1545 passed, 0 failed, 0 skipped**;
3. четырёх test-contract mutants, двух API mutants и шести package mutants;
4. exact 9-package matrix;
5. Wist consumer всех presets/backends, semantic vectors и incompatible-checkout rejection;
6. Language SDK template и cross-package consumers;
7. detached release-integrity verification и integrity mutants;
8. повторения всего canonical pipeline из чистой распаковки финального source ZIP с пустыми caches/build outputs.

Фактические результаты финальной поставки фиксируются в `VERIFICATION.md`, `SECOND_COMPLETION_LEDGER.csv` и delivery evidence bundle.

## Ограничения

- публикация на NuGet.org не выполнялась;
- `build.ps1` статически синхронизирован с Bash entrypoint, но Windows/PowerShell execution требует отдельного Windows host;
- production VitePress build зависит от полного npm cache/network; status/link/navigation checks выполняются локально независимо.
