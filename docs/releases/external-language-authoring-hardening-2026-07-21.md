# External language authoring hardening — 2026-07-21

## Architectural delta

This iteration removes the remaining Wist- and text-shaped assumptions from the public external-language path:

- added `LanguageArtifactKind<T>` and typed artifact contracts;
- moved route compatibility failures from runtime casts to language planning;
- made the pipeline entry artifact configurable;
- allowed planning-only languages without fake backend/runtime selections;
- added `UniversalToolchain.LanguageAuthoring`, which creates descriptors and implementations from one registration source;
- added typed transformer and executor interfaces;
- bound every runtime executor to the exact backend contribution selected by the plan;
- require explicit determinism and host-interop traits instead of inferring safety from arbitrary delegates;
- made runtime determinism and host-interop policies enforceable provider contracts;
- changed manifests and lock snapshots to schema v3 while retaining v1/v2 readers;
- replaced the Wist-based beginner template with a standalone non-Wist template;
- replaced the Acme sample with an independent parser and two independent backends;
- synchronized the canonical `Wist.sln` project graph; the redundant `.slnx` mirror was later retired to avoid drift.

## Compatibility

The old string constructor for `LanguageExecutionRequest`, untyped artifact IDs, schema-v1/v2 manifests and the legacy Wist runtime adapter remain supported. New code should use typed artifact kinds and the authoring builder.

## Honest boundary

This is a typed contribution/route language SDK, not yet a declarative grammar or type-system workbench. Grammar, binding, type rules and operation-definition conveniences remain future layers.
