# UniversalToolchain.Wist 0.1.0-alpha.7 — architecture & production hardening candidate

Дата candidate source: 2026-08-15.

`0.1.0-alpha.7` — новая **неопубликованная** candidate identity поверх канонической LanguagePlan/LanguageRuntime архитектуры из migration #332. Эта работа не выполняет publish, release promotion или merge в `master`.

## Основные изменения

- `LanguageCompiler.Compile(...)` остаётся единственным public semantic planner; внутренние deterministic phases вынесены без второго planner/DI topology;
- `LanguageRuntime` стал execution-only API, а artifact-build capability представлена типом `LanguageBuildRuntime`;
- construction rollback сохраняет primary exception и stack, а cleanup failures — отдельно и после primary;
- `Validate`/`TryCompile` различают `UserInput`, `Policy`, `Unsupported`, `Infrastructure`, `Internal`; infrastructure/internal faults fail-fast и не маскируются как invalid formula;
- low-level binding сохраняет reviewed `InvalidOperationException` family, но несёт typed `BindingException` marker для Wist taxonomy;
- один `WistEngine` не объявлен concurrent-reentrant: overlapping operations fail-fast, для параллельных streams используются отдельные instances;
- добавлены source-retention policies `Full`, `HashAndIdentity`, `None` и отдельные developer/safe consumer diagnostics;
- required CI workflow set вынесен в единый machine-readable owner и aggregate работает fail-closed;
- physical Wist runtime closure классифицирован по фактическому package artifact; package graph не дробился без доказанной выгоды;
- Wist feature ownership разделён по фактическим syntax / semantic-binding / lowering обязанностям, runtime materializes эти роли только из exact `LanguagePlan`, а DSL ordering распространяется на соответствующие phase-owned contributions;
- generic Dialects/CIL/runtime implementation tests перенесены из mixed Wist-owned suites в `UniversalToolchain.LanguageSdk.Generic.Tests`, поэтому UT internal friend edges остаются только `UNIVERSAL -> UNIVERSAL`, а Wist-free proof стал содержательнее;
- flaky percentage performance gate не добавлялся: сохранён benchmark smoke и добавлен artifact/trend collection.

## Матрица candidate packages

<!-- package-matrix:begin -->
| Package ID | Version |
|---|---|
| `UniversalToolchain.Language.Abstractions` | `0.3.0-alpha.4` |
| `UniversalToolchain.FeatureSdk` | `0.3.0-alpha.4` |
| `UniversalToolchain.LanguageSdk` | `0.3.0-alpha.5` |
| `UniversalToolchain.Runtime` | `0.3.0-alpha.5` |
| `UniversalToolchain.LanguageAuthoring` | `0.3.0-alpha.5` |
| `UniversalToolchain.Testing` | `0.3.0-alpha.5` |
| `UniversalToolchain.Templates` | `0.3.0-alpha.5` |
| `UniversalToolchain.Wist.LanguagePack` | `0.3.0-alpha.6` |
| `UniversalToolchain.Wist` | `0.1.0-alpha.7` |
<!-- package-matrix:end -->

Новые версии зарезервированы потому, что соответствующие package payloads изменились относительно merge #332. Для `LanguageAuthoring`/`Testing` меняется generated dependency metadata из-за нового `Runtime`, а template package содержит новые candidate package references. Повторное использование прежних alpha identities для иного payload не допускается. Это versioning metadata для candidate build, а не публикация.

## Проверка

Exact test manifest текущей ветки: **1,303 теста**: Core 500, Modules 292, Dialects 233, LanguageSdk.Generic 62, LanguageSdk 165, PlanFuzz 41 и 10 isolated integration cases. Перенос 58 generic implementation tests между owner-specific suites не уменьшает покрытие; 4 ранее существовавших Wist-free generic tests вместе с ними образуют 62-test canonical generic suite в Linux/Windows contract. Дополнительные regressions фиксируют реальную `Syntax -> Semantic -> Bytecode` границу, stage-local lifetime, plan-owned lowering/fail-closed activation, phase-owned module ordering и скрытые `UNIVERSAL -> WIST_PRODUCT` friend edges. Canonical build/test gate обязан подтвердить 0 failed и 0 skipped на exact candidate revision; более старые workflow receipts не подменяют exact-head verification.

Baseline-bearing package gate проверяет девять package identities, exact `.nuspec` metadata, monotonic version/content provenance, Wist public API delta, physical package surface, clean external consumers и detached integrity manifest. GitHub aggregate CI остаётся отдельным обязательным gate.

## Не является частью candidate

- publish packages;
- merge в `master`;
- release promotion;
- sandboxing claim;
- secure secret scrubbing claim;
- thread-safety claim для одного `WistEngine`;
- preset-specific package splitting без отдельного доказанного ownership boundary.
