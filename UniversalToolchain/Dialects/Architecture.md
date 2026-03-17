# Dialect Definition DSL subsystem overview

This folder documents the Dialect Definition DSL inside **UniversalToolchain**.

## What this subsystem does

The subsystem converts dialect text into deterministic, reviewable composition intent.

High-level outputs:

1. `DialectSyntaxDocument` (parser output)
2. `DialectBuildPlan` (semantic output)
3. `DialectRuntimeComposition` (runtime descriptor resolution output)
4. `DialectApplyDescription` (explicit apply-mode projection)

## Layering (intended dependency direction)

- **UniversalToolchain.Dialects.Parsing**
  - Lexes/parses dialect text.
  - Produces syntax-level objects only.
- **UniversalToolchain.Dialects.Core**
  - Performs semantic normalization/validation.
  - Produces deterministic build plan + diagnostics.
- **UniversalToolchain.Dialects.Integration**
  - Resolves build plans against explicit runtime descriptors.
  - Produces runtime composition and optional apply description.
- **UniversalToolchain.Dialects.Abstractions**
  - Shared immutable contracts and models used by all layers.
- **UniversalToolchain.Dialects.Tests**
  - Covers parser/core/integration behavior and deterministic contracts.

## Architectural guardrails

- No hidden assembly-scan driven behavior in semantic or resolution stages.
- No forced dialect activation in existing runtime flows.
- Determinism is a product requirement, not a best-effort behavior.

## Why this lives in UniversalToolchain

Dialect DSL directly configures UniversalToolchain composition policies (modules/backends/optimizers/intrinsics). Keeping it in the same solution preserves contract parity and lets contributors evolve it together with the host pipeline.
