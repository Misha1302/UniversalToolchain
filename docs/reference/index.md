---
title: Reference Overview
description: Index strict reference pages for advanced users.
---

# Reference Overview

Reference pages are for readers who need compact contracts, tables and implementation-facing facts.

They are not tutorials. If you are learning the project for the first time, start with [Getting Started](/start/) and [Internals](/internals/) before using this section.

## How to use reference pages

Use reference pages when you need to answer questions such as:

- which AIR opcodes exist;
- what operands an instruction expects;
- which backend modes exist;
- what a backend must preserve;
- which dialect/module names are intended as stable aliases;
- which intrinsic symbols or capability families need documentation;
- which project and documentation rules constrain changes.

## Current reference map

| Page | Purpose |
|---|---|
| [Dialect Reference](/reference/dialect-reference) | dialect file syntax, directive families and shipped profiles |
| [Module Reference](/reference/module-reference) | module aliases, feature ownership and runtime exports |
| [Module Contracts](/reference/module-contracts) | token, parser priority, visitor ownership, bytecode tag, state and backend-capability contracts |
| [Bytecode Reference](/reference/bytecode-reference) | bytecode concepts and lowering responsibilities |
| [AIR Reference](/reference/air-reference) | AIR opcode table and instruction contracts |
| [Intrinsics Reference](/reference/intrinsics-reference) | intrinsic symbols, type arguments and capability expectations |
| [Backend Contracts](/reference/backend-contracts) | interpreter/CIL/shared backend obligations |
| [Project Rules](/reference/project-rules) | coding and contribution rules relevant to documentation and internals |
| [Documentation Rules](/reference/documentation-rules) | documentation synchronization and executable example rules |

## Stability levels

Not every internal detail has the same stability.

| Stability level | Meaning |
|---|---|
| Public usage contract | safe to show in user-facing docs |
| Developer contract | expected by module/backend authors and protected by tests |
| Current implementation | true today, but should not be treated as public API without tests |
| Review note | useful for architecture review, not normative |

Reference pages should label current implementation details when they are not yet stable API.

## What belongs here

Reference pages should include:

- tables;
- aliases;
- opcode/operand shapes;
- supported modes;
- explicit constraints;
- links to internals pages for deeper explanation.

## What does not belong here

Reference pages should not become:

- marketing copy;
- long tutorials;
- speculative roadmap;
- undocumented guarantees;
- replacements for tests.

## Documentation rule

If a reference page lists a contract, it should be backed by one of:

- current source code;
- tests;
- public project rules;
- architecture docs.

If the source of truth is uncertain, document the item as current implementation rather than stable contract.

## Next

Start with [Dialect Reference](/reference/dialect-reference), [Module Contracts](/reference/module-contracts), [AIR Reference](/reference/air-reference) or [Backend Contracts](/reference/backend-contracts).
