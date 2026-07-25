# Миграция с legacy Wist API

Версия программы: `0.3.0-alpha.1`. Машиночитаемый реестр: `LEGACY_DEPRECATION_REGISTRY.json`.

## Runtime pack → runtime provider

- `LanguageRuntimePackReference` заменяется на `LanguageRuntimeProviderReference`.
- `UseRuntimePack(...)` заменяется на `UseRuntimeProvider(...)`.
- `ILanguageRuntimePack` и overload `LanguageRuntime.Create(..., ILanguageRuntimePack, ...)` заменяются на `ILanguageRuntimeProvider` и `LanguageRuntimeProviderRegistry`.
- `WistLanguageRuntimePack` заменяется на `WistLanguageRuntimeProvider`.

Удаление этой compatibility family запрещено, пока `WIST_PARITY_MATRIX.json` не подтверждает executable parity всех shipped presets.

## Conditions

`WistContributionIds.ConditionsModule` был неоднозначным alias-ом. Используйте:

- `ComparisonsModule` для сравнений;
- `BooleanLogicModule` для boolean-операций;
- `ConditionalControlFlowModule` для `if/else`;
- feature `wist.conditions` только как совместимый aggregate.

## Typed artifact routes

Untyped constructors `LanguageArtifactRouteStep` и `LanguageArtifactRoute` заменяются overload-ами с `LanguageArtifactContract`. Это сохраняет CLR type identity и исключает wildcard-соединение typed и untyped routes.

## Runtime identity

`EnumGenerator` больше не задаёт identity через process-wide insertion order. Используйте stable name identity `ExtensibleEnum<TTag>` либо instance-scoped `ExtensibleEnumCatalog<TTag>` и вызывайте `Freeze()` перед исполнением.

## Gate удаления

До removal должны быть выполнены все условия записи реестра: repository и downstream usage assessment, migration docs, compatibility tests, warning-as-error phase и соответствующий parity gate. Даты/версии `warningAsErrorNotBefore` и `removalNotBefore` являются нижней границей, а не обещанием автоматического удаления.
