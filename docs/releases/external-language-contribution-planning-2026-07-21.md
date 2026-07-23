# External language contribution planning — 2026-07-21

## Scope

This release iteration replaces the profile-alpha assumption that one selected runtime pack owns the whole external language path.

Implemented contracts:

- typed language slots and artifact kinds;
- contribution dependencies, capabilities, conflicts and backend scope;
- explicit capability-provider preference and single-owner slot replacement;
- runtime-provider contributions independent from features and frontend implementations;
- deterministic minimum-cost artifact routes per backend;
- schema-v4 package manifests, plan hashes and lock files containing effective contributions and routes;
- exact runtime-provider registry with side-by-side versions;
- executable generic routes through registered transformers and backend executors;
- Wist-owned legacy dialect adapter with no Wist references in generic SDK projects;
- obsolete compatibility adapters for the previous `ILanguageRuntimePack` alpha surface.

## Evidence required before release

The final verification record must contain the exact full build, four test-assembly totals, Wist interpreter/CIL sample output, package inspection, clean NuGet consumer, template consumer, documentation checks and clean-unpack results.

## Honest boundary

The generic route runtime can execute independently supplied transformations. The shipped Wist runtime provider still translates the resolved contribution plan into a generated legacy Wist dialect and executes the existing verified Wist host. High-level grammar, binder, type-system and operation authoring are not claimed by this release.
