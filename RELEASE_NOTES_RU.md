# UniversalToolchain.Wist 0.1.0-alpha.6 — детерминированная граница shared contracts

Дата candidate source/artifacts: 2026-08-03.

`0.1.0-alpha.6` заменяет runtime-boundary candidate `0.1.0-alpha.5`. Пакет не обещает binary compatibility с ранними runtime assemblies и не опубликован на NuGet.org в рамках этой работы.

## Основные изменения

- process-history-dependent эвристика `TryLoadTrustedDefaultDependency` удалена;
- host явно регистрирует shared contract assemblies через immutable snapshot;
- сопоставление использует полную assembly identity: name, version, normalized culture и public key token;
- configured shared copy проходит fail-closed SHA-256 validation;
- незарегистрированные implementation assemblies остаются в collectible isolated `AssemblyLoadContext`;
- скрытый fallback application assemblies в default context запрещён;
- Wist preload-workaround удалён, contract closure регистрируется через canonical CLR owner types;
- SSA runtime report/options/sink перенесены в abstractions owner без sharing optimizer implementation;
- добавлены fresh-process preload-order, hostile same-name, concurrency, disposal и unload regressions;
- добавлен `Tools/package_metadata.py` и negative mutants для project/docs/nupkg drift;
- public value boundary нормализует implementation-owned numeric values до стабильных CLR categories.

## Матрица candidate packages

<!-- package-matrix:begin -->
| Package ID | Version |
|---|---|
| `UniversalToolchain.Language.Abstractions` | `0.3.0-alpha.4` |
| `UniversalToolchain.FeatureSdk` | `0.3.0-alpha.4` |
| `UniversalToolchain.LanguageSdk` | `0.3.0-alpha.4` |
| `UniversalToolchain.Runtime` | `0.3.0-alpha.4` |
| `UniversalToolchain.LanguageAuthoring` | `0.3.0-alpha.4` |
| `UniversalToolchain.Testing` | `0.3.0-alpha.4` |
| `UniversalToolchain.Templates` | `0.3.0-alpha.4` |
| `UniversalToolchain.Wist.LanguagePack` | `0.3.0-alpha.5` |
| `UniversalToolchain.Wist` | `0.1.0-alpha.6` |
<!-- package-matrix:end -->

## Проверка

Локальный exact test contract: **1,612 passed, 0 failed, 0 skipped**. Обе solution собраны в `Release` с **0 warnings, 0 errors**.

Fresh-process SafeMath receipts подтверждают `DIALECT_INSPECT=PASS`, Interpreter/CIL result `255`, одинаковую CLR value category, negative dialect rejection, hostile preload rejection и отсутствие незарегистрированного default-context fallback.

Baseline-aware package gate проверяет девять package identities, exact `.nuspec` metadata, monotonic version/content provenance, Wist package surface, template/cross-package consumers и detached integrity manifest. GitHub aggregate CI остаётся отдельным обязательным gate; публикация packages не выполнялась.
