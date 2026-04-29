# Global Project Overview

This document gives a high-level map of UniversalToolchain and Wist: what the repository is, how the main parts fit together, who it is for, and where deeper documentation lives.

For evaluative architectural feedback, risks, and roadmap scoring, see [Technical due diligence review](reviews/technical-due-diligence.md).

## Short description

UniversalToolchain is a modular .NET DSL/runtime framework for building restricted, embeddable mini-languages from composable features.

Wist is the reference language in this repository. It demonstrates the framework through shipped dialect profiles, a CLI, examples, manifest-backed runtime composition, interpreter execution, and CIL execution.

The repository should be read as two connected layers:

- **UniversalToolchain**: reusable framework infrastructure.
- **Wist**: reference language and proving ground for that infrastructure.

## Why the project exists

Many applications eventually need configurable formulas, rules, or workflows. A plain expression evaluator can be too small, while a full custom compiler can be too expensive to maintain.

UniversalToolchain explores a middle layer:

- language features are implemented as modules;
- dialects select which features and runtime components are allowed;
- runtime activation is driven by manifests and selected runtime plans;
- the same language can be executed through interpreter and compiled/CIL paths;
- intermediate representations preserve enough semantic information for validation, diagnostics, and optimization.

See [Why this exists](why-this-exists.md) and [UniversalToolchain vs nearby alternatives](alternatives.md) for the product rationale.

## Main architectural idea

The project is organized around a staged pipeline:

```text
Source -> Lexer/Parser -> AST -> Bytecode/AIR -> Optimization -> Backend -> Execution
```

The important point is that this pipeline is meant to be **composed**, not hardcoded as one monolithic compiler.

A language feature can participate in several stages:

- text preprocessing;
- lexer initialization;
- parser initialization;
- AST post-processing;
- AST-to-bytecode translation;
- bytecode or AIR processing;
- backend-specific lowering or execution support.

The canonical Wist dialect runtime path is:

```text
dialect source -> dialect compilation -> build plan -> manifest-backed runtime selection -> host creation -> execution
```

See [Current canonical runtime pipeline](current-canonical-runtime-pipeline.md), [Runtime manifest activation model](runtime-manifest-activation-model.md), and [Runtime manifest format](runtime-manifest-format.md).

## Core concepts

### Dialect

A dialect is a selected language/runtime profile. It can enable only a subset of modules, backends, optimizers, intrinsics, security policies, and capabilities.

Dialect files are used to describe constrained profiles such as minimal arithmetic, native arithmetic, pricing-restricted execution, or fuller Wist configurations.

### Module

A module is a composable language feature. It may contribute syntax, parser behavior, AST translation, capabilities, and runtime behavior.

Module authoring is powerful but convention-heavy. New modules should follow [Module authoring guide](guides/module-authoring.md) and [Module contracts](contracts/module-contracts.md).

### Runtime manifest

Runtime manifests describe available components and activation metadata. They make runtime selection more explicit and less dependent on broad reflection or hardcoded wiring.

### Build plan and selected runtime plan

The dialect source is normalized into a deterministic build plan. That build plan is resolved against available runtime components into a selected runtime plan, which is then used to create the execution host.

### Bytecode and AIR

Bytecode and AIR are semantic intermediate layers. They are used to carry meaning between frontend modules, optimizers, and backends.

This is one of the project’s most important technical ideas. It also requires strong contracts around tags, stack effects, metadata, and backend expectations.

See [Bytecode and AIR architecture](architecture/bytecode-and-air.md).

### Backend

A backend is an execution strategy selected by the dialect/runtime plan. Current Wist-facing concepts include interpreter and CIL execution.

The interpreter should be treated as the semantic/reference path, while CIL is the compiled execution path. Public performance claims should be backed by reproducible benchmarks and parity tests.

See [Backends and semantic parity](architecture/backends-and-parity.md).

## Repository audiences

### Product user

A product user wants configurable logic inside a .NET application without hardcoding every formula or rule.

For this audience, the most useful entry points are:

- [readme.md](../readme.md);
- CLI examples in the README;
- shipped dialect examples under `UniversalToolchain/Dialects/examples/wist`;
- pricing demo under `UniversalToolchain/Example`.

### DSL author

A DSL author wants to build or adapt a small language. The current project is strongest when the author can study Wist as the reference language and reuse existing composition patterns.

Relevant docs:

- [Project positioning](project-positioning.md);
- [Current canonical runtime pipeline](current-canonical-runtime-pipeline.md);
- [Runtime manifest activation model](runtime-manifest-activation-model.md);
- [Current limitations](limitations.md).

### Module author

A module author wants to add a feature such as arithmetic, variables, loops, labels, conditions, or function calls.

Relevant docs:

- [Module authoring guide](guides/module-authoring.md);
- [Module contracts](contracts/module-contracts.md);
- [Architecture guardrails](ARCHITECTURE_RULES.md);
- [Project rules](PROJECT_RULES.md).

### Backend author

A backend author wants to add or maintain an execution strategy.

Relevant docs:

- [Backends and semantic parity](architecture/backends-and-parity.md);
- [Runtime manifest activation model](runtime-manifest-activation-model.md);
- [Current limitations](limitations.md).

### Reviewer or jury member

A reviewer should focus on the project as an architectural prototype with a working reference language and explicit limitations.

Useful reading order:

1. [readme.md](../readme.md)
2. [Project positioning](project-positioning.md)
3. [Current canonical runtime pipeline](current-canonical-runtime-pipeline.md)
4. [Technical due diligence review](reviews/technical-due-diligence.md)
5. [Current limitations](limitations.md)

## Strengths

- Clear framework/reference-language split: UniversalToolchain versus Wist.
- Manifest-backed runtime selection instead of one hardcoded runtime mode.
- Composable module model for frontend features.
- Interpreter and CIL execution paths for the same language family.
- Bytecode/AIR layers that can carry semantic information across the pipeline.
- Honest documentation around positioning, limitations, alternatives, and security boundaries.

## Current design boundaries

The project is actively evolving. These areas should be treated as design-in-progress:

- generic third-party DSL authoring experience;
- dialect directive extensibility;
- backend-agnostic executable artifact handling;
- bytecode tag validation and verifier coverage;
- AI-safe module generation;
- benchmark-backed performance claims;
- hardened sandboxing for untrusted execution.

See [Current limitations](limitations.md) for the authoritative wording guide.

## What to show in a demo

A strong demo should show:

- a pricing or formula scenario that would be awkward to hardcode repeatedly;
- a full Wist profile and a restricted dialect profile;
- successful execution through compiler/CIL and interpreter paths;
- rejection of unsupported syntax in a restricted dialect;
- manifest-backed runtime selection;
- how a module plugs into the frontend pipeline.

Avoid presenting the project as a finished universal language workbench or hardened sandbox.

## Documentation map

Start here:

- [README](../readme.md)
- [Project positioning](project-positioning.md)
- [Current limitations](limitations.md)
- [Technical due diligence review](reviews/technical-due-diligence.md)

Architecture:

- [Current canonical runtime pipeline](current-canonical-runtime-pipeline.md)
- [Runtime manifest activation model](runtime-manifest-activation-model.md)
- [Runtime manifest format](runtime-manifest-format.md)
- [Bytecode and AIR architecture](architecture/bytecode-and-air.md)
- [Backends and semantic parity](architecture/backends-and-parity.md)
- [Architecture guardrails](ARCHITECTURE_RULES.md)

Authoring:

- [Module authoring guide](guides/module-authoring.md)
- [Module contracts](contracts/module-contracts.md)
- [Project rules](PROJECT_RULES.md)
- [Contributing](CONTRIBUTING.md)

Positioning and trust boundaries:

- [Why this exists](why-this-exists.md)
- [UniversalToolchain vs nearby alternatives](alternatives.md)
- [Security policy](SECURITY.md)

## Relationship to the due diligence review

This global overview is the stable map of the project.

The [Technical due diligence review](reviews/technical-due-diligence.md) is more evaluative. It discusses readiness scores, hidden contracts, risks, roadmap candidates, and suggested implementation prompts.

Both documents are useful, but they should not serve the same purpose:

- use this overview to understand the project structure;
- use the due diligence review to evaluate architectural maturity and improvement priorities.
