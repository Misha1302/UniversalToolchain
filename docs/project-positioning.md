# Project Positioning

This document defines how to describe this repository without overpromising what exists today.

## One-sentence description

UniversalToolchain is a modular .NET DSL/runtime framework for composing restricted, embeddable languages from modules, compiling dialect definitions into deterministic runtime plans, and executing selected dialects through interpreter and CIL paths.

## Product boundary

UniversalToolchain is the framework.

Wist is the reference language, proving ground, and integration surface used to validate the framework architecture.

The repository should not be described as only a Wist compiler. It should also not be described as a finished universal language workbench. The accurate current description is:

> A Wist-first .NET DSL/runtime framework prototype with real dialect composition, manifest-backed runtime selection, and explicit architecture guardrails.

## What belongs to UniversalToolchain

UniversalToolchain owns the reusable architecture:

- dialect definition and validation;
- dialect build-plan projection;
- runtime manifests and selected runtime plans;
- deterministic runtime activation infrastructure;
- generic composition rules;
- backend and intrinsic contracts;
- architecture guardrails that prevent Wist-specific decisions from becoming framework truth.

## What belongs to Wist

Wist owns one concrete language and its convenience experience:

- shipped Wist dialect profiles;
- Wist syntax, modules, and examples;
- `WistRuntimeFacadeBuilder` and Wist facade usage;
- Wist CLI behavior;
- Wist-specific backend declarations and host creation.

The Wist facade is intentionally convenient, but it must remain a thin wrapper over the selected runtime pipeline. It must not become a second framework runtime.

## Public demo positioning

A strong public demo should emphasize:

- restricted DSL profiles instead of one hardcoded language mode;
- manifest-backed runtime selection;
- interpreter/CIL execution paths for the same selected language;
- Wist as a reference implementation;
- architecture guardrails that keep framework layers independent from concrete product profiles.

A public demo should not claim:

- polished third-party DSL authoring with no internal knowledge required;
- production-grade sandboxing for untrusted code;
- near-C# performance without current benchmark evidence;
- arbitrary backend extensibility with no Wist-facing changes;
- fully formal bytecode tag validation unless verifier coverage exists.

## Audience-specific framing

### Product user

Use UniversalToolchain when an application needs configurable formulas or rules that are too structured for a plain expression evaluator, but still need a controlled runtime surface.

### DSL author

Treat the current framework as a foundation with a strong Wist reference path. New DSLs are possible, but the generic authoring surface is still being stabilized.

### Module author

A module is a package of syntax, parsing, translation, capabilities, and optional runtime behavior. Follow the module contracts instead of copying implementation details blindly.

### Backend author

A backend is an execution strategy selected by a dialect runtime plan. It must declare supported intrinsics and preserve semantics proven against the interpreter/reference path.

### AI code generator

Do not infer architecture from nearby code alone. Follow the documented contracts for module ownership, token names, parser priorities, bytecode tags, intrinsic policies, and backend boundaries.

## Honest limitation statement

Today the project proves a strong Wist-first architecture for modular language composition and manifest-backed runtime selection. Generic dialect-DSL extension, backend-agnostic compiled artifacts, bytecode tag verification, and outsider-friendly module authoring are still design-in-progress areas.
