# AGENTS

This file is the mandatory behavior guide for AI agents working in this repository.

Read this file before editing code. If a local task conflicts with this file, this file wins unless the user explicitly updates the architecture rules.

## Project identity

- **UniversalToolchain** is the primary product: a reusable, modular toolchain/framework for building and composing language runtimes.
- **Wist** is the reference language and proving ground in this repository, not the only architectural truth.
- Treat Wist-specific code and docs as examples of framework usage unless a file explicitly defines a Wist-only contract.

## Absolute architecture laws

Breaking these rules is a release-blocking architectural defect.

1. Generic framework layers must not hardcode dialect, profile, module, function, rule, backend, or demo names.
2. Framework/core/runtime layers must not branch by shipped product profile names such as `pricing-rules`, `validation-rules`, or `policy-rules`.
3. Runtime truth must flow only through dialect definition, compiled dialect slice, build plan, selected runtime plan, runtime configuration, and host/executor.
4. Capabilities/features are projection and explanation layers. They describe selected composition; they do not activate runtime behavior.
5. BasicCore must not depend on Wist, Rules, SafeMath, concrete feature modules, product profiles, or demo scenarios.
6. Function names belong to function providers/modules, not to parser, resolver, framework, or facade code.
7. Convenience APIs must be thin wrappers over existing composition/runtime paths. They must not create second parsers, second evaluators, second registries, or product-specific runtimes.
8. Product profiles must be ordinary dialect preset/configuration files, not framework runtime modes.
9. All provider discovery, catalogs, diagnostics, feature reports, schemas, CLI output, and overload resolution must be deterministic.
10. Architecture shortcuts are worse than missing features. If a feature cannot be implemented without a shortcut, leave it incomplete and document the limitation.
11. Syntax ownership is mandatory. Language syntax must not be recognized through ad hoc raw-source parsing outside the owning lexer/parser/AST/extractor pipeline.

## Syntax ownership law

`docs/SYNTAX_OWNERSHIP_RULES.md` is mandatory for all agents.

Language syntax must be recognized only by the owning lexer, parser, AST node creators, AST visitors, or syntax-specific extractors built from parser output.

Production validators, facades, resolvers, runtime wrappers, catalogs, CLI commands, optimizers, and convenience layers must consume structured syntax output. They must not rediscover language syntax from raw source text.

Forbidden production patterns include regular expressions, line splitting, substring checks, manual source scans, and one-off scanners used to recognize language constructs outside the owning syntax pipeline.

A missing parser, AST, or declaration model is not permission to parse raw source text locally. If the structured model does not exist yet, implement that model or leave the feature incomplete and document the limitation. A string-based syntax workaround is not an acceptable MVP.

## Single Responsibility Doctrine

Single Responsibility is mandatory, not a style preference.

- Parser parses syntax.
- Extractor extracts neutral declarations.
- Resolver resolves names, types, functions, and overloads.
- Projector projects capabilities/features for reports.
- Catalog describes available providers/descriptors/bindings.
- Runtime executes selected plans.
- Formatter formats diagnostics/output.
- Facade orchestrates existing workflows and remains thin.
- Module owns its own descriptors, bindings, and capability declarations.

Forbidden SRP violations:

- parser deciding runtime availability;
- resolver owning concrete module function names;
- capability projector activating runtime behavior;
- CLI implementing a separate runtime path;
- facade becoming a product-specific engine;
- catalog becoming a hidden source of composition truth;
- generic framework code branching on concrete modules/profiles;
- validators, facades, resolvers, runtimes, CLI commands, optimizers, or catalogs rediscovering language syntax from raw source text;
- one type acting as parser + resolver + executor + formatter.

## Controlled reflection policy

Reflection is not forbidden.

Controlled reflection is recommended when it reduces compile-time coupling between generic framework layers and concrete modules.

Use reflection when it helps:

- avoid direct compile-time dependencies from framework/core layers to concrete modules;
- discover module-owned providers through explicit composition boundaries;
- keep BasicCore and generic runtime infrastructure independent from Wist, SafeMath, Rules, and product profiles;
- reduce manual registration boilerplate;
- preserve modular extensibility for future DSLs, modules, providers, and backends.

Reflection must be bounded, deterministic, cached where appropriate, and kept out of hot execution paths.

Allowed reflection boundaries:

- explicitly selected assemblies;
- explicitly selected dialect modules;
- explicitly selected provider marker interfaces/contracts;
- selected compiled dialect slices;
- selected runtime composition plans;
- known provider contracts such as function, type, capability, rule, backend, and diagnostic providers.

Forbidden reflection patterns:

- scanning all loaded assemblies blindly;
- scanning the whole AppDomain without explicit boundaries;
- resolving behavior by concrete type names or shipped profile names;
- repeated reflection scans during hot execution;
- reflection-based branching such as `if (type.Name == "SafeMathFunctionsModule")`;
- keeping unnecessary Assembly/Type/MemberInfo graphs alive when immutable descriptors are enough.

Recommended pattern:

1. Composition selects dialect modules.
2. Discovery scans only selected module/provider boundaries.
3. Providers are discovered through stable contracts.
4. Providers produce immutable descriptors and runtime bindings.
5. Catalogs/build plans are built deterministically.
6. Execution uses resolved descriptors, delegates, bindings, or compiled plans without repeated reflection.

Reflection is a decoupling mechanism, not a dynamic behavior shortcut.

## Non-negotiable priorities

1. Universality first.
2. Preserve existing architectural principles instead of locally optimizing around them.
3. Avoid hardcode.
4. Avoid coupling to concrete implementation details.
5. Avoid coupling to concrete dialects.
6. Avoid coupling to concrete modules when reasonably possible.
7. Prefer designs that make universality erosion hard, visible, and testable.
8. Apply DRY, KISS, SOLID, and OOP.
9. Do not introduce technical debt.
10. Do not introduce legacy behavior/patterns.

## Expected change strategy

Prefer:

- extension points over special cases,
- abstractions over ad hoc wiring,
- composition over branching,
- reusable contracts over one-off shortcuts,
- deterministic behavior over hidden magic,
- minimal targeted changes over broad rewrites,
- data-driven descriptors over smart central authorities,
- optional convenience layers over mandatory framework dependencies,
- controlled reflection over direct framework-to-module dependencies when it reduces coupling,
- structured syntax models over raw-source scans,
- designs where narrowing a reusable abstraction requires an explicit structural change rather than a small ad hoc patch.

## Forbidden patterns

Do not introduce:

- hardcoded dialect assumptions,
- hardcoded module assumptions,
- hardcoded function names outside the owning module/tests/examples/docs,
- hardcoded product profile assumptions,
- `if`/`switch` branching on concrete profile ids in framework/core/runtime code,
- raw-source syntax recognition outside the owning lexer/parser/AST/extractor pipeline,
- “just for this case” hacks,
- implementation-detail leakage into public contracts,
- copy-paste extensions,
- architecture bypasses,
- preservation of bad legacy behavior without explicit justification,
- silent behavior changes without test updates,
- convenience registries/catalogs/loaders that become hidden decision-makers for framework-level composition,
- framework entities that are easy to expand by adding concrete-profile, concrete-module, or concrete-backend branching,
- repeated reflection scans in hot paths,
- unbounded assembly/AppDomain scanning.

## Rules for touching code

Before editing:

- read the relevant architecture and module boundaries,
- reuse existing extension points when possible,
- avoid parallel abstractions when one already exists,
- if the change needs to understand language syntax, identify the owning parser/AST/declaration model first; do not parse raw source text locally,
- verify whether the change preserves the project's existing universality and layering principles,
- check whether controlled reflection can reduce coupling without becoming a hidden source of truth.

While editing:

- avoid new global mutable state,
- keep public API naming/shape consistent,
- keep framework-level abstractions independent from Wist-specific details where reasonable,
- keep convenience layers thin and optional,
- prefer designs where built-in or product-specific entities are data-only when reasonably possible,
- convert reflection results into immutable descriptors/catalog entries/runtime bindings,
- keep reflection discovery out of hot execution paths,
- follow `docs/PROJECT_RULES.md` for coding standards.

After editing:

- add or update tests for behavior changes,
- add or update architecture guardrails when a new convenience layer, catalog, registry, discovery path, or facade is introduced,
- update docs when behavior, contracts, or architecture meaningfully change,
- run relevant build/test/validation commands or state exactly why they could not be run.

## Documentation policy

- `readme.md` is the canonical repository overview.
- `docs/PROJECT_RULES.md` is the canonical coding standard and architecture rule set.
- `docs/ARCHITECTURE_RULES.md` is the canonical architecture guardrail document.
- `docs/SYNTAX_OWNERSHIP_RULES.md` is the canonical syntax ownership policy.
- `docs/CONTRIBUTING.md` is the canonical contribution workflow.
- `docs/series-01-to-07-final-task.md` is the current implementation task for finishing PR #206.
- This file (`AGENTS.md`) is the canonical AI-agent behavior guide.
