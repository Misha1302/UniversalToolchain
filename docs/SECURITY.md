---
title: Security Policy and Trust Boundaries
description: Vulnerability reporting, Wist host controls, generic runtime policy and privacy limits.
audience: security-platform-reviewer
status: current
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# Security policy and trust boundaries

## Report a vulnerability

Use a private GitHub Security Advisory for the repository or contact the maintainer directly. Do not publish exploitable details before triage and a fix.

## Core trust statement

UniversalToolchain executes code and extension components inside the host process. Restricted composition, deterministic plans and explicit capability policy are **not** equivalent to a hardened sandbox.

For hostile input or hostile packages, isolate execution in a constrained process/container and enforce wall-clock, CPU, memory, filesystem, network and identity limits outside the framework.

## Wist host controls

The Wist facade provides bounded defense-in-depth controls:

- explicit `AllowedAssemblies` for host CLR resolution;
- source-length and external-parameter preflight limits;
- restricted presets that omit broader language/runtime features;
- structured diagnostics before execution where possible.

These controls do not interrupt a long-running delegate, bound heap allocation, revoke an already allowed assembly or isolate native calls.

## Generic Language Runtime policy

`LanguageRuntimePolicy` can require deterministic components, forbid host interop and bound source length/external parameter count. The generic route provider validates declared `LanguageRuntimeComponentTraits` before session creation.

Traits are package attestations. A malicious package can lie in its implementation; policy validation is not a cryptographic proof of behavior.

Exact package-manifest binding prevents accidental or unauthorized drift between the package graph used for planning and the package descriptors supplied for runtime assembly. It does not make the selected implementation trustworthy.


## Threat-scenario matrix

| Scenario | Framework control | Required host control |
|---|---|---|
| untrusted formula text | restricted Wist surface, source/parameter preflight limits, structured diagnostics | process-level CPU/time/memory limits; approval and rollback policy |
| untrusted language package | exact package/manifest binding and declared runtime traits | do not load into a trusted host process; isolate and review package code |
| excessive or non-terminating execution | no universal interruption guarantee | execute in a killable process/container with wall-clock and resource quotas |
| host interop abuse | allowlisted assemblies and profile/capability selection | expose only reviewed host APIs; avoid broad reflection or ambient credentials |
| trace or diagnostic disclosure | raw source/arguments/result are not directly serialized by default | treat exception messages and trace files as sensitive; apply storage/redaction policy |
| source retention | source identity is visible in program/artifact metadata | minimize retention, control memory dumps and avoid logging metadata source text |
| mutable component state leakage | `PerSession` ownership and singleton trait checks | validate custom component thread safety and avoid cross-tenant runtime reuse |
| package graph drift | plan/manifest identity and runtime assembly checks | pin package versions, retain release artifacts and regenerate plans through an approved migration |

The table describes defense in depth, not a formal sandbox or proof against malicious in-process code.

## Component lifecycle

`PerSession` components are isolated by construction and owned by the session. Explicit stateless singletons must be non-disposable and implement `IStatelessLanguageRuntimeComponent`. Reusing a `PerSession` instance across sessions is rejected.

This reduces accidental state leakage; it is not a general memory-isolation mechanism.

## Source and trace privacy

- `WistProgramMetadata.SourceText` and low-level `ICompiledArtifact.SourceText` retain full source text in memory;
- generic `LanguageExecutionRequest` retains its typed input artifact and argument references for the call;
- CLI trace defaults avoid direct serialization of raw source, arguments and result values;
- trace error fields may still contain lower-level `exception.Message`; current length sanitization does not prove secret removal.

Do not treat trace files as automatically safe for unrestricted distribution. Apply normal secret handling, storage access and retention controls.

## Supported branch

The project currently develops security fixes on the default branch and targets .NET 10 (`net10.0`).
