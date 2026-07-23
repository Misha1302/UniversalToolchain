---
title: Public Project Rules
description: Public summary of architecture and contribution invariants.
---

# Public project rules

The public architecture invariants are:

- UniversalToolchain is the framework; Wist is a reference language and packaged facade.
- Frontend language features must not depend directly on backend implementations.
- Wist Bytecode and AIR are distinct semantic boundaries.
- Generic external languages use explicit typed artifact contracts and deterministic `LanguagePlan` routes.
- Runtime selection must be explicit and deterministic; enumeration order is not ownership.
- Backend-specific rewrites require capability/contract support.
- Interpreter/compiler parity is required where both Wist paths claim the same behavior.
- External components must declare lifecycle and runtime-policy traits.
- Restricted composition is not a hardened sandbox.
- New public claims require implementation, tests and a tied evidence record.

Maintainer-only coding policy and historical migration rules live under `internal-docs/` and are not part of the VitePress public contract.
