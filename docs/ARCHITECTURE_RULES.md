# ARCHITECTURE_RULES

This document defines mandatory architecture guardrails for UniversalToolchain.

`docs/PROJECT_RULES.md` remains the coding standard. This file makes the non-negotiable architecture rules explicit so agents and contributors cannot accidentally narrow the framework into a Wist-specific or product-specific implementation.

## 1. Product boundary

UniversalToolchain is the framework.

Wist is the reference language and proving ground.

Product profiles, demos, SafeMath functions, pricing rules, validation rules, and policy rules are examples or modules. They are not framework truth.

## 2. Absolute laws

Breaking these rules is a release-blocking architecture defect.

1. Generic framework layers must not hardcode dialect names, profile names, module names, function names, rule names, backend names, or demo names.
2. Framework/core/runtime layers must not branch by shipped profile names such as `pricing-rules`, `validation-rules`, or `policy-rules`.
3. Runtime truth must flow only through dialect definition, compiled dialect slice, build plan, selected runtime plan, runtime configuration, and host/executor.
4. Capability/feature systems are projection and explanation layers. They report selected composition; they do not activate runtime behavior.
5. BasicCore must not reference Wist, Rules, SafeMath, concrete feature modules, product profiles, or demo scenarios.
6. Function names belong to function providers/modules. They do not belong to parser, resolver, framework, CLI, or facade code.
7. Product profiles are ordinary dialect preset/configuration files. They are not runtime modes.
8. Convenience APIs must remain thin wrappers over existing composition/runtime paths. They must not become second runtimes.
9. All discovery, catalog building, diagnostics, schema output, feature reports, CLI output, and overload resolution must be deterministic.
10. Architecture shortcuts are worse than incomplete features.

## 3. Single Responsibility Doctrine

Single Responsibility is mandatory.

- Parser parses syntax.
- Extractor extracts neutral declarations.
- Resolver resolves names, types, functions, and overloads.
- Projector projects capabilities/features for reports.
- Catalog describes selected providers/descriptors/bindings.
- Runtime executes selected plans.
- Formatter formats diagnostics/output.
- Facade orchestrates existing workflows and stays thin.
- Module owns its own descriptors, runtime bindings, and capability declarations.

Forbidden SRP violations:

- parser deciding runtime availability;
- resolver owning concrete module function names;
- capability projector activating runtime behavior;
- CLI implementing a separate runtime path;
- facade becoming a product-specific engine;
- catalog becoming a hidden source of composition truth;
- generic framework code branching on concrete modules/profiles;
- one type acting as parser, resolver, executor, and formatter at once.

## 4. Controlled reflection policy

Reflection is allowed and recommended when it reduces compile-time coupling between generic framework layers and concrete modules.

Use reflection when it helps:

- avoid direct framework dependencies on concrete modules;
- discover module-owned providers through explicit composition boundaries;
- keep BasicCore independent from Wist, SafeMath, Rules, and product profiles;
- reduce manual registration boilerplate;
- preserve modular extensibility for future DSLs, modules, providers, and backends.

Reflection must be bounded, deterministic, cached where appropriate, and kept out of hot execution paths.

Allowed reflection boundaries:

- explicitly selected assemblies;
- explicitly selected dialect modules;
- explicitly selected provider marker interfaces/contracts;
- selected compiled dialect slices;
- selected runtime composition plans;
- known provider contracts such as function, type, capability, rule, backend, or diagnostic providers.

Forbidden reflection patterns:

- scanning all loaded assemblies blindly;
- scanning the whole AppDomain without explicit boundaries;
- resolving behavior by concrete type names or shipped profile names;
- repeated reflection scans during hot execution;
- reflection-based branching such as checking a concrete module type name;
- keeping unnecessary Assembly/Type/MemberInfo graphs alive when immutable descriptors are enough.

Recommended flow:

1. Composition selects dialect modules.
2. Discovery scans only selected module/provider boundaries.
3. Providers are discovered through stable contracts.
4. Providers produce immutable descriptors and runtime bindings.
5. Catalogs/build plans are built deterministically.
6. Execution uses resolved descriptors, delegates, bindings, or compiled plans without repeated reflection.

Reflection is a decoupling mechanism, not a dynamic behavior shortcut.

## 5. Source of truth rule

There must be no hidden runtime source of truth.

The only valid runtime activation chain is:

```text
dialect definition
-> compiled dialect slice
-> build plan
-> selected runtime plan
-> runtime configuration
-> host/executor
```

Feature reports, capability reports, docs, CLI schema views, and catalogs may explain that chain, but they must not replace it.

## 6. Provider ownership rule

Providers own domain-specific names and descriptors.

Examples:

- SafeMathFunctionsModule may own `min`, `max`, `abs`, `clamp`, and `round`.
- FunctionCallsModule owns generic `identifier(args...)` syntax and mechanics, not SafeMath names.
- Product profile preset files may contain `pricing-rules`, `validation-rules`, and `policy-rules` names.
- Generic framework layers may not branch by those names.

## 7. Required guardrails

When adding convenience layers, catalogs, discovery paths, facades, or product profiles, add or update tests where practical to protect:

- BasicCore independence from Wist/Rules/SafeMath/concrete feature modules;
- absence of hardcoded product profile branching in framework layers;
- absence of SafeMath function names in parser/resolver/framework code;
- deterministic provider discovery and report ordering;
- facade reuse of the existing runtime pipeline;
- absence of repeated reflection scans in hot execution paths.
