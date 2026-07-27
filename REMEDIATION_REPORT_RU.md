# Ремедиация независимого ревью Wist

Дата: 2026-07-27

## Baseline

- исходный архив: `Wist-no-legacy-rechecked-2026-07-26(1).zip`;
- SHA-256 исходного архива: `39278b5955c6ecc8c1071cb385f1dcb49c9f1638a23e7ca1ca4eb4ba6a83fc74`;
- исправления выполнялись в отдельном рабочем дереве;
- исходный архив не изменялся.

## Итог

Все 14 finding-ов слепого независимого ревью закрыты кодом, машинно проверяемыми контрактами или явной compatibility-классификацией. Публичная версия Wist в этом дереве — `0.1.0-alpha.2`; generic SDK packages остаются `0.3.0-alpha.1`.

## Закрытие finding-ов

| ID | Статус | Исправление | Проверка |
|---|---|---|---|
| WIST-BR-001 | Closed | `Compile`/`TryCompile` используют тот же выбранный backend, что `Evaluate`/`Validate` | public backend operation matrix |
| WIST-BR-002 | Closed | preset/backend combination проверяется в `WistEngine.Create`; default backend принадлежит preset catalog | 9 presets × supported/unsupported backends |
| WIST-BR-003 | Closed | delegate lowering и public value normalization согласованы для CIL/interpreter | evaluate/validate/compile exact-value parity |
| WIST-BR-004 | Closed | root-authoritative collectible `AssemblyLoadContext`, exact path/full identity checks, no simple-name process preload trust | hostile-preload regression |
| WIST-BR-005 | Closed | raw Wist host/workflow/configuration/compatibility facades internalized | exported API allowlist/regression tests |
| WIST-BR-006 | Closed | exact managed closure, PE/CLR metadata, preset/manifests and per-manifest SHA-256 checked | zero-byte, missing-DLL, unexpected-DLL, alias-drift mutants |
| WIST-BR-007 | Closed | canonical release script executes local Wist clean consumer and incompatible-checkout mutant | continuous release log |
| WIST-BR-008 | Closed | 74 reviewed API/package changes classified in `eng/wist-api-compatibility.csv` | compatibility ledger checker |
| WIST-BR-009 | Closed | incompatible runtime generation uses assembly version `2.0.0.0`; generation-1 substitution rejected | incompatible-checkout smoke |
| WIST-BR-010 | Closed | package release set has detached provenance root and immutable verification input | regenerated/tampered manifest mutants |
| WIST-BR-011 | Closed | runtime manifests have exact reviewed hashes; alias mutation changes hash and is rejected | alias-drift mutant |
| WIST-BR-012 | Closed | unknown identifier/type and assembly-qualified type fail closed; no `object` fallback | negative binding contracts |
| WIST-BR-013 | Closed | observable local/label identities are deterministic; ALC-specific implementation values normalized at public boundary | independent-context determinism tests |
| WIST-BR-014 | Closed | exact catalog contains all 9 packaged presets | package preset set == catalog set |

## Дополнительные дефекты, найденные во время исправления

- lifecycle owner `ServiceProvider` уничтожался до возвращённого runtime host/session;
- `SafeMathFunctions` ошибочно требовал `NativeTypes`, создавая второй arithmetic lexer;
- `UniversalToolchain.Templates` объявлялся release package, но не восстанавливался перед `pack --no-restore`;
- Language SDK package smoke использовал общий заполненный cache вместо чистого временного cache;
- package smoke ожидал старый late exception и использовал RID-зависимые hardcoded output paths;
- test-count contract и verification docs оставались на 1508 после добавления regression coverage;
- active docs смешивали локальный alpha.2 artifact и утверждение о публикации.

Все перечисленные дефекты исправлены и закреплены проверками.

## Каноническая проверка

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 506 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 293 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 596 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 80 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1526** | **0** | **0** |

Дополнительно подтверждены:

- 9/9 public packages собраны;
- Wist exact package surface проходит;
- все 4 package-surface mutants убиты;
- Wist clean consumer проходит 9 presets, supported backend matrix и incompatible-checkout rejection;
- Language SDK clean consumer устанавливает шаблон, создаёт новый язык и выполняет cross-package consumer;
- release-integrity verification покрывает 10 package artifacts;
- integrity mutants отклонены;
- documentation status и link/navigation checks проходят;
- `git diff --check`, Python bytecode compilation и Bash syntax checks проходят.

## Границы заявления

- Изоляция `AssemblyLoadContext` защищает identity/closure boundary, но не является OS/process-security sandbox.
- PowerShell-вариант `build.ps1` синхронизирован с Bash orchestration, но не исполнялся: в Linux-среде проверки отсутствует `pwsh`/Windows host.
- VitePress production build не выполнен: offline npm cache не содержит `zwitch@2.0.4`; Python status/link checks прошли. Это dependency-availability blocker, а не скрытый зелёный результат.
- Публикация `0.1.0-alpha.2` на NuGet.org не выполнялась и не заявляется. Проверялся локально собранный release artifact.

## Authority

Внутренний package manifest не считается самостоятельным доказательством происхождения. Финальная поставка сопровождается внешним `DELIVERY_SHA256SUMS.txt`; release packages дополнительно покрыты `artifacts/RELEASE-INTEGRITY.json` и detached root.
