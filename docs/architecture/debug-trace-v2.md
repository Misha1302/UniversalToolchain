---
title: Debug Trace v2
description: Decision record for replacing legacy text logs with structured compiler traces.
---

# Debug Trace v2

Status: decision recorded, implementation not complete.

Date: 2026-07-04

## Decision

The legacy text-log debugging surface is not part of the supported release surface.

`LogsViewer`, `ExecutorLoggerModule`, and the `logs.txt` format were removed from the release package because they described only an older partial frontend pipeline: source text, lexemes, AST and bytecode. They did not represent the current compiler boundaries such as AIR, optimized AIR, explicit SSA route stages, verifier diagnostics, backend compilation, facts, capabilities or failure ownership.

The replacement direction is a structured, versioned trace contract that observes stable compiler boundaries without changing semantics.

## What Is Kept

The following infrastructure remains valid:

- pipeline observer concepts used for validation and contracts;
- module contract observers;
- verifier diagnostics;
- dialect inspection and runtime component listing commands;
- interpreter/compiler parity tests.

These are not the complete trace system. They are inputs and guardrails for a future trace recorder.

## What Is Removed

The release no longer contains:

- `LogsViewer/`;
- `ExecutorLoggerModule`;
- tests that assert `logs.txt` is created;
- public project references that expose the logger as a supported module.

Historical archive material may mention older logging experiments, but current documentation must not advertise them as supported tooling.

## Target Shape

The intended future user-facing shape is:

```text
wistc run --trace trace.json --eval "2+3"
```

That command is a target, not a current release guarantee. Until the trace CLI is implemented, users should rely on existing tests, diagnostics, dialect inspection commands and backend parity checks.

## Trace Contract Principles

A future trace must:

- be optional;
- be observer-only and not alter compilation or execution semantics;
- write deterministic JSON with a `schemaVersion`;
- represent stage ownership and failure location;
- keep Bytecode, AIR and SSA as distinct artifact kinds;
- attach diagnostics to the stage that produced them;
- omit source text and runtime values by default unless an explicit policy enables them;
- avoid Wist-specific branches in generic tracing infrastructure.

## Initial Stage Vocabulary

The first structured schema should be able to represent:

```text
TextInput
TextProcessing
Lexing
Parsing
AstProcessing
AstToBytecode
BytecodeProcessing
BytecodeToAir
AirVerification
IrOptimizationPass
SsaLowering
SsaVerification
SsaOptimizationPass
SsaEmission
BackendCompile
ExecutorSetup
```

## Initial Artifact Vocabulary

The first structured schema should be able to represent:

```text
SourceTextSummary
LexemeList
AstTree
Bytecode
Air
Ssa
BackendSummary
DiagnosticList
FactSet
CapabilitySet
```

## Non-Goals

Do not:

- restore `logs.txt` as the primary trace format;
- make tracing a frontend module;
- make a viewer the source of truth for the schema;
- claim stable CIL listings until the backend emits a real stable backend artifact;
- serialize private source text, runtime values or method arguments by default.

## Acceptance Criteria For Future Implementation

The trace feature is ready only when:

- trace-disabled and trace-enabled execution produce the same observable result;
- failed compilation can flush a partial trace;
- golden JSON tests protect deterministic schema and stage order;
- privacy tests prove source/runtime values are omitted by default;
- SSA route traces show lowering, verification, optimization and emission as separate stages;
- a viewer consumes real `trace.json`, not legacy sample logs.
