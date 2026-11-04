# Changelog — 2026-10-20 library-first architecture revision

- ACCEPTED: independent `.NET SemanticEvidence.Core` plus optional `SemanticEvidence.Rules.Numeric`; UT is a consumer, not the definition of core domain identity.
- MOVED: selected-plan authority and host-owned guarded rewrite/commit are exclusively `UniversalToolchain.SemanticEvidence` responsibilities.
- DEFERRED: any publishing/NuGet/repository split until a second non-UT consumer and actual UT-native regression demonstrate reuse and value.
- ADDED: explicit package boundaries, migration mapping, independent consumer and API tests, revised implementation brief.
- PRESERVED: original 2026-10-16 model-only prototype and logs exactly byte-for-byte inside the CI-safe evidence ZIP (without placing extra `*.csproj` in the repo).
- NOT CLAIMED: standalone library implemented, UT adapter integrated, production correctness established, GitHub changed.
