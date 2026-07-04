---
title: Debug Trace Schema
description: Current status and intended contract for structured compiler traces.
---

# Debug Trace Schema

Status: planned contract, not implemented in the current release.

The supported release no longer includes legacy text-log debugging through `logs.txt`. The next debugging surface is planned as a versioned JSON trace over stable compiler stages.

## Current Status

There is no supported `--trace` CLI option yet.

Current debugging and validation tools are:

- normal CLI diagnostics;
- tests around interpreter/compiler parity;
- verifier diagnostics;
- dialect and runtime component inspection commands;
- architecture documentation for pipeline boundaries.

## Planned Top-Level Shape

The future JSON document should contain:

| Field | Purpose |
|---|---|
| `schemaVersion` | version the trace format |
| `createdAtUtc` | record trace creation time |
| `stages` | ordered list of compilation stages |
| `artifacts` | artifact summaries or references |
| `diagnostics` | diagnostic index |
| `metadata` | implementation and environment metadata safe to disclose |

## Planned Stage Shape

Each stage should contain:

| Field | Purpose |
|---|---|
| `id` | stable stage instance id within the trace |
| `parentId` | parent stage id, if nested |
| `kind` | stage kind such as `Parsing`, `BytecodeToAir`, `SsaLowering` |
| `owner` | module, pass, backend or framework component that owns the stage |
| `displayName` | human-readable stage name |
| `status` | success, failed or skipped |
| `startedAtUtc` | stage start timestamp |
| `duration` | elapsed time |
| `inputArtifactRefs` | artifacts consumed by the stage |
| `outputArtifactRefs` | artifacts produced by the stage |
| `diagnostics` | diagnostics owned by the stage |
| `facts` | fact snapshot or summary |
| `capabilities` | capability snapshot or summary |
| `metadata` | small backend-neutral metadata |

## Privacy Defaults

The default trace must not include full source text, runtime parameter values, private method arguments or secrets. Source should default to a summary or hash. Full source capture should require an explicit option.

## Viewer Policy

A future viewer must read real structured traces. It must not recreate or depend on the removed `logs.txt` format, and it must not invent CIL listings unless a backend emits a stable listing artifact.
