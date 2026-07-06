---
title: Debug Trace Schema
description: Current schema contract for structured compiler traces.
---

# Debug Trace Schema

Status: first CLI artifact phase implemented.

The supported release no longer includes legacy text-log debugging through `logs.txt`. The replacement surface is an opt-in versioned JSON trace emitted by `wistc run --trace`.

## Current Status

`wistc run --trace trace.json ...` writes a redacted JSON artifact for the run. Version `wist-debug-trace/2` records source summary, dialect/backend selection, coarse runtime stages and result/error summary with bounded metadata. It is intentionally not a full viewer and does not dump raw AST, bytecode, AIR or SSA yet.

Current debugging and validation tools are:

- `wistc run --trace trace.json`;
- normal CLI diagnostics;
- tests around interpreter/compiler parity;
- verifier diagnostics;
- dialect and runtime component inspection commands;
- architecture documentation for pipeline boundaries.

## Top-Level Shape

The future JSON document should contain:

| Field | Purpose |
|---|---|
| `schemaVersion` | version the trace format; currently `wist-debug-trace/2` |
| `createdAtUtc` | record trace creation time |
| `stages` | ordered list of compilation stages |
| `metadata` | implementation and environment metadata safe to disclose |
| `source` | redacted source summary, including length and SHA-256 |
| `result` | success/error summary |

## Stage Shape

Each stage should contain:

| Field | Purpose |
|---|---|
| `id` | stable stage instance id within the trace |
| `kind` | stage kind such as `TextInput`, `RuntimeSelection`, `BackendExecution` |
| `owner` | module, pass, backend or framework component that owns the stage |
| `status` | success, failed or skipped |
| `metadata` | small backend-neutral metadata |

## Current Stage Catalog

The CLI writer emits these stage ids in order:

| id | kind | owner |
|---|---|---|
| `input` | `TextInput` | `Wistc` |
| `dialect-composition` | `DialectComposition` | `UniversalToolchain.Dialects` |
| `runtime-selection` | `RuntimeSelection` | `UniversalToolchain.Dialects` |
| `bytecode-translation` | `BytecodeTranslation` | `BasicCore` |
| `backend-artifact` | `BackendArtifact` | `UniversalToolchain.Runtime` |
| `backend-execution` | `BackendExecution` | `UniversalToolchain.Runtime` |

Supported status values are `success`, `failed` and `skipped`.

## Privacy Defaults

The default trace does not include full source text, runtime parameter values, private method arguments or secrets. Source is represented by length and SHA-256. Full source capture is not implemented.

## Viewer Policy

A future viewer must read real structured traces. It must not recreate or depend on the removed `logs.txt` format, and it must not invent CIL listings unless a backend emits a stable listing artifact.
