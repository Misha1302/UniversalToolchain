# Отчёт о реализации и трёх независимых аудитах: Wist без compatibility legacy

> Исторический snapshot до ремедиации 2026-07-27. Текущий статус и проверяемые результаты находятся в `REMEDIATION_REPORT_RU.md`; числа и release-оценки ниже не являются authority для исправленного дерева.


## 1. Итог

Compatibility-слои удалены из публичной alpha-поверхности и активных serialized contracts. Typed `LanguageDefinition`/`LanguagePlan` больше не генерирует промежуточный текстовый `.wistdialect`: он материализуется в `DialectDefinitionSlice`, после чего исполняется через канонический Wist dialect composer/runtime.

Формулировка намеренно точная: удалён compatibility-legacy и текстовый round-trip, но базовый dialect composition engine остаётся каноническим владельцем исполнения, а не дублируется новой реализацией.

F-01—F-14 закрыты. F-10 подтверждён в следующем объёме:

- 21 выбираемый typed runtime-модуль;
- 1 обязательный инфраструктурный модуль `ProgramStructure`, принадлежащий composer-у и не выдаваемый за отдельную typed feature;
- 7 выбираемых оптимизаторов;
- 2 canonical backend ID: `cil`, `interpreter`;
- 7 shipped presets с executable parity на репрезентативных программах.

После независимого повторного аудита исправлены дополнительные дефекты релиз-кандидата:

1. restricted typed plan мог выбрать `CSharpInterop` при `AllowHostInterop=false`;
2. shipped interop presets теряли capability `unsafe-interop` при typed materialization;
3. регистронезависимый поиск preset возвращал неканонические definition ID/metadata;
4. публичный низкоуровневый `CreateSession` позволял пропустить policy validation;
5. parity matrix содержала несуществующую typed feature для `ProgramStructure` и завышала доказательство модульной эквивалентности;
6. installer сохранял старые `bin/obj` и мог проверить DLL предыдущей версии;
7. package metadata всё ещё называла LanguagePack legacy adapter-ом;
8. installer принимал unmatched filtered VSTest run с нулём tests как успех;
9. current docs противоречили удалённым schemas/adapters/manifest fallbacks;
10. PlanFuzz сохранял current evidence IDs с префиксом `compiler.*`;
11. parity evidence references не проверялись на существование NUnit tests;
12. canonical verification docs и checker фиксировали устаревшие 1465 tests.

## 2. Baseline и среда

- Baseline commit: `b2058f803a678d673398e834eab88f152f37c4c7`.
- Отозванные candidates: `Wist-no-legacy-2026-07-26.zip` и `Wist-no-legacy-audited-2026-07-26.zip`.
- Debian GNU/Linux 13, linux-x64.
- .NET SDK 10.0.301; runtime 10.0.9.
- Restore/package smoke выполнены через локальный NuGet sidecar; consumer-smoke также подтвердил восстановление из локального package output.

## 3. Удалённые compatibility-поверхности

Удалены:

- `LanguageRuntimePackReference`, `ILanguageRuntimePack`, `UseRuntimePack(...)` и старый runtime overload;
- `WistLanguageRuntimePack` и `WistLegacyDialectAdapter`;
- untyped artifact routes;
- `WistPreset`, `WistBackend`, mapper и alias `compiler`;
- `EnumGenerator`, при сохранении безопасной замены `ExtensibleEnumCatalog` и `SetAndList`;
- `EnableLegacyDirectiveDefinitions`, `StrictLegacyCompatible`, `VerifyLegacyBytecodeOperationNames`;
- assembly-scanning activation и CLR-name module identity fallback;
- historical DI bootstrap и facade message-shims;
- чтение Feature Manifest schema 1—4 и PlanFuzz observation schema 1—3;
- obsolete overload-ы runtime component registration и untyped artifact construction;
- deprecation registry и активные migration-инструкции для удалённых API.

## 4. Исполняемая архитектура

```text
LanguageDefinition
    -> LanguageCompiler
    -> verified LanguagePlan
    -> WistLanguageRuntimeProvider
    -> typed DialectDefinitionSlice
    -> canonical Wist dialect composition/runtime
    -> exact modules / optimizers / backend selection
    -> execution
```

Инварианты:

- исполняемая истина — verified `LanguagePlan`;
- policy проверяется и общим `LanguageRuntime.Create`, и Wist provider-ом при прямом `CreateSession`;
- `CSharpInterop` требует `AllowHostInterop=true`; contradiction с `unsafe-interop=false` отклоняется;
- manifest задаёт точные component/assembly/type identities;
- module identity берётся из явного contract descriptor/export;
- backend выбирается только canonical ID;
- capabilities материализуются в детерминированном ordinal-порядке;
- historical serialized schemas отклоняются fail-fast;
- exact runtime selection сверяется с verified plan после composition.

## 5. Compatibility impact

Это намеренный breaking release для alpha-поверхности. Downstream-код должен:

- использовать `LanguageRuntimeProviderReference` и `UseRuntimeProvider(...)`;
- создавать runtime через typed package/provider path;
- использовать `cil`, а не `compiler`;
- задавать structured activation metadata полностью;
- сериализовать только текущие Feature Manifest/PlanFuzz schemas;
- использовать factory registrations и `LanguageArtifactKind<T>`.

Compatibility aliases и автоматические migration fallback-и не предоставляются.

## 6. Проверки рабочего дерева после независимого аудита

### Builds

- `UniversalToolchain/Wist.sln`: Build succeeded; 0 warnings; 0 errors.
- `UniversalToolchain/PlanFuzz.sln`: Build succeeded; 0 warnings; 0 errors.
- `samples/Acme.PricingLanguage`: Build succeeded; 0 warnings; 0 errors.
- `samples/Wist.RolloutScoring`: Build succeeded; 0 warnings; 0 errors.

### Tests

| Assembly | Passed | Failed | Skipped |
|---|---:|---:|---:|
| `Tests` | 505 | 0 | 0 |
| `UniversalToolchain.Modules.Tests` | 290 | 0 | 0 |
| `UniversalToolchain.Dialects.Tests` | 584 | 0 | 0 |
| `UniversalToolchain.LanguageSdk.Tests` | 78 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.Tests` | 41 | 0 | 0 |
| `UniversalToolchain.PlanFuzz.IntegrationTests` | 10 | 0 | 0 |
| **Total** | **1508** | **0** | **0** |

Modules и Dialects запускаются выбранным для них project runner с hang diagnostics; Core, Language SDK и PlanFuzz — прямым VSTest. PlanFuzz integration подтверждён девятью процессно-изолированными filter runs, один из которых покрывает два параметризованных cases: суммарно 10/10. Каждый запуск обязан создать TRX с точным числом Passed cases; exit code без TRX или с нулём совпадений отклоняется.

Новые audit-regressions проверяют:

- отказ restricted `CSharpInterop` через оба публичных runtime entry points;
- capability `unsafe-interop` у shipped interop presets;
- канонизацию preset identity;
- typed-vs-shipped execution parity всех семи presets на каждом разрешённом backend.

### Packages

- package matrix: 9/9;
- `UniversalToolchain.Wist` surface: 1 compile DLL, 64 runtime DLLs;
- template consumer: `42`;
- cross-package consumer: `42`.

### Static gates

- parity matrix сопоставляет реальные typed feature IDs с contribution aliases;
- `ProgramStructure` учитывается как infrastructure-owned export, а не вымышленная feature;
- status vocabulary разделяет `SelectionEquivalent`, `InfrastructureEquivalent`, `ExecutableEquivalent`;
- production source не содержит удалённых compatibility symbols;
- JSON parsing, shell syntax и source diff whitespace checks проходят;
- `eng/test-counts.json` является единым test-count contract для installer и current verification docs;
- parity evidence references разрешаются в реальные NUnit methods через Roslyn;
- current documentation checker запрещает подтверждённые stale legacy/schema/backend claims.

## 7. Остаточные риски

- Breaking change требует обновления downstream alpha-потребителей.
- Module/optimizer entries имеют доказанную selection equivalence; полная семантическая эквивалентность каждого модуля по отдельности не заявляется без отдельного behavioral corpus.
- Typed provider использует канонический Wist dialect composition/runtime. Это осознанное единое owner boundary, а не оставленный compatibility adapter.
- Aggregate testhost в container-среде иногда зависает без test failure; все cases подтверждены прямыми/изолированными запусками.
- VitePress render зависит от доступности npm registry; Markdown structure/link gates остаются локально проверяемыми.

## 8. Release artifact

Финальный rechecked source-only artifact проверен как отдельный чистый кандидат:

- один top-level каталог `Wist-no-legacy-rechecked-2026-07-26`;
- 1684 source/report files total, 1683 entries in recursive `MANIFEST.sha256`;
- ZIP path-safety и побайтовое совпадение staging/unpack;
- отсутствуют `.git`, `bin`, `obj`, `artifacts`, `TestResults`, `node_modules`, секреты, БД, логи и NuGet-артефакты;
- из чистой распаковки собраны `Wist.sln`, `PlanFuzz.sln` и оба sample-проекта: 0 warnings / 0 errors;
- из чистой распаковки пройдены 1508/1508 тестов;
- из чистой распаковки проверены 9/9 NuGet-пакетов, surface `1/64` и два consumer-smoke со значением `42`;
- installer smoke подтвердил удаление stale `bin/obj`, успешный коммит и отказ при divergent baseline без явного override.
