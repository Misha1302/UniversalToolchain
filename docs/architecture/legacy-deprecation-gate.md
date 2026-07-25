# Legacy deprecation/removal gate

`LEGACY_DEPRECATION_REGISTRY.json` — canonical owner программы удаления legacy API.

CI/architecture tests обязаны отклонять:

1. запись без owner, replacement, версии, migration guide, usage assessment или exit criteria;
2. duplicate ID/symbol;
3. `Removed`, если shipped preset parity matrix не закрыта;
4. legacy `[Obsolete]`, входящий в программу, без стабильного gate ID;
5. replacement claim для generic Wist LanguagePack, пока `replacementClaimAllowed=false`.

Матрица `WIST_PARITY_MATRIX.json` является evidence gate, а не маркетинговым описанием. Markdown-файл `WIST_PARITY_MATRIX_RU.md` — публичная проекция того же состояния.
