# UniversalToolchain.Wist 0.1.0-alpha.4 — runtime-boundary hardening release notes

Дата source/release artifact: 2026-07-27.

`0.1.0-alpha.4` supersedes the locally built alpha.2 artifact and closes the second independent review findings. Он не обещает source/binary compatibility с `0.1.0-preview.1`, `0.1.0-alpha.1` или runtime assemblies поколения `1.0.0.0`.

## Основные изменения

- единый backend contract для `Validate`, `Evaluate`, `Compile` и `TryCompile`;
- ранняя проверка preset/backend в `WistEngine.Create`;
- exact catalog всех 9 поставляемых presets;
- root-authoritative collectible runtime load context;
- assembly generation `2.0.0.0` для несовместимой runtime chain;
- internalization низкоуровневых Wist compatibility/bypass APIs;
- fail-closed identifier/type binding;
- deterministic observable identities и public value normalization;
- exact package closure/manifests/hash gate;
- machine-readable compatibility ledger из 74 записей;
- detached release-integrity root;
- clean consumers и seeded package/runtime/integrity mutants в canonical release orchestration;
- гарантированная выгрузка collectible runtime-контекста после `Dispose`;
- нормализация implementation-owned чисел до стабильных CLR-типов, включая `object`;
- CLR numeric parameter adaptation для NumbersModule presets;
- exact timeout-bounded TRX contract и semantic preset/package mutants.

## Verification

Канонический контракт: **1544 passed, 0 failed, 0 skipped**. Точные counts принадлежат `eng/test-counts.json` и зеркалятся в `VERIFICATION.md`.

Локально собран `UniversalToolchain.Wist.0.1.0-alpha.4.nupkg`. Публикация на NuGet.org в рамках этой ремедиации не выполнялась.

Исторические audit/remediation-материалы вынесены из product source archive в отдельный evidence/history bundle.
