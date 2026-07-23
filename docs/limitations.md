---
title: Current Limitations
description: Accurate limits of Wist and the external Language Authoring alpha.
audience: all-technical-users
status: current
lastVerifiedAgainst: language-authoring-p0-p1-hardening-2026-07-23.1
---

# Current limitations

## Generic language authoring is low-level

Implemented: typed package/contribution planning, artifact routing, runtime assembly, policy and lifecycle.

Not implemented as a high-level authoring product:

- grammar or parser generation;
- binder/type-system DSL;
- automatic derivation of verifier and backend contracts from operations;
- IDE/editor workbench;
- incremental parsing protocols;
- stable 1.0 compatibility.

The current SDK helps compose implementations; it does not generate a language implementation.

## Backend-neutral runtime status

Backend-neutral artifact/session contracts and generic typed route execution are implemented. The remaining limitations are different:

- Wist legacy surfaces still require compatibility adapters;
- the generic API is alpha and may evolve;
- a third independent production-scale backend has not yet validated every abstraction boundary;
- high-level backend authoring ergonomics remain sparse.

Do not describe backend-neutral artifacts as future-only work.

## Wist-specific boundaries

- Wist remains the most mature language path;
- Wist compiler module authoring still contains hidden token, parser-priority, visitor and Bytecode-tag contracts;
- Bytecode tag taxonomy and verification are incomplete;
- the callable-first SSA route supports a bounded subset and may round-trip opaque managed calls without optimizing them;
- `Prefer` falls back only for known unsupported-route diagnostics.

## Security

Restricted dialects and runtime policies constrain selected components. They are not hardened sandboxing. Hostile input or hostile extension packages require external process/environment isolation, resource controls and a trust model.

## Performance

Only tied, reproducible benchmark scenarios support performance claims. Compilation, `Evaluate`, interpreter execution and prepared typed-delegate invocation are separate workloads.

## Documentation and ecosystem

- external package authoring is new and has limited third-party use;
- generic API reference is not yet generated from a stable public API baseline;
- version negotiation and migration tooling are early;
- documentation snippets are protected by repository checks but do not replace consumer-level compatibility tests.
