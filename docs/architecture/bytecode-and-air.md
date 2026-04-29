# Bytecode and AIR

This document explains the intended role of bytecode and AIR in the current architecture.

It is intentionally conservative: it documents the architecture contracts that contributors must preserve, not a future verifier that does not exist yet.

## Why there are multiple intermediate layers

UniversalToolchain separates language syntax, modular feature translation, semantic representation, optimization, and execution.

The rough shape is:

```text
source
-> lexer/parser
-> AST
-> bytecode
-> AIR
-> optimization/lowering
-> backend execution
```

The exact shape can vary by pipeline, but the principle is stable: each layer should carry a clearer and more structured form of meaning than the previous layer.

## Bytecode is not just an opcode list

Bytecode should be treated as a semantic merge layer.

A bytecode instruction can carry metadata tags and layered operations. This means the bytecode layer can represent meaning contributed by multiple modules before the program is fully lowered into a backend-ready form.

This is different from a classic single-opcode instruction stream.

## Semantic merge space

A modular compiler cannot assume that one monolithic lowering pass owns all meaning.

Frontend modules can contribute:

- syntax;
- AST node shapes;
- translation visitors;
- metadata tags;
- operations;
- later optimization opportunities.

The bytecode layer is the place where those contributions can be normalized before AIR and backend lowering.

## Bytecode tags

Tags should describe semantic facts that later stages may need.

Good tag categories include:

- feature ownership;
- purity and side-effect information;
- arithmetic or comparison semantics;
- control-flow markers;
- backend-lowering candidates;
- optimization safety markers;
- diagnostics and source mapping markers.

Tags must not become undocumented magic strings.

Every tag that affects lowering, optimization, safety, or backend behavior should have:

- a stable name;
- a producer;
- a consumer;
- a documented meaning;
- tests or verifier coverage when practical.

## Layered operations

Layered operations allow more than one semantic contribution to exist at one bytecode instruction point.

Rules:

- Layers must be deterministic.
- Layer priorities must be documented when they affect behavior.
- A module must not depend on accidental insertion order.
- If two layers conflict, the conflict should be diagnosed instead of silently resolved by registration order.

## Bytecode invariants to protect

The project should preserve these invariants:

- bytecode is produced from structured syntax, not raw-source scans;
- bytecode tags are semantic metadata, not hidden backend switches;
- bytecode operations must have deterministic ordering;
- bytecode must not depend on mutable global module state;
- bytecode produced by one compilation must not leak into another;
- bytecode must be valid before AIR/backend lowering.

## AIR role

AIR should be treated as a more explicit semantic or executable intermediate representation.

AIR can be used for:

- interpreter execution;
- CIL/backend lowering;
- metadata annotations;
- optimizer input/output;
- selected runtime diagnostics;
- dialect-definition slices where metadata is represented as IR annotations.

AIR is not only a transport structure. In some subsystems it also acts as a semantic metadata carrier.

## Bytecode versus AIR

Use this distinction when designing changes:

| Layer | Responsibility |
|---|---|
| AST | Structured syntax owned by parser/modules |
| Bytecode | Modular semantic merge and normalized feature contribution |
| AIR | Explicit semantic/execution representation for optimization and backend work |
| Backend artifact | Concrete executable form such as interpreter-ready IR or CIL-compiled artifact |

Do not skip directly from raw syntax to backend-specific behavior outside the owning pipeline.

## Optimizer expectations

Optimizers should rely on semantic facts, not accidental syntax shapes.

Prefer optimizer decisions based on:

- AIR instruction kind;
- documented bytecode tags;
- capability selections;
- backend-supported intrinsics;
- explicit purity/side-effect metadata.

Avoid optimizer decisions based on:

- raw source text;
- concrete Wist module names in generic layers;
- undocumented tag strings;
- backend-specific assumptions in generic passes.

## Backend expectations

Backends should consume validated semantic representations.

A backend should not need to rediscover language syntax or guess whether a feature is enabled. That information should flow through:

```text
dialect definition
-> build plan
-> selected runtime plan
-> runtime configuration
-> bytecode/AIR/backend input
```

## Known design debt

The current architecture has a strong bytecode/AIR idea, but the safety surface should be strengthened.

Important future improvements:

- central bytecode tag registry;
- bytecode verifier;
- documented tag producer/consumer matrix;
- stack-effect validation where possible;
- clearer split between semantic AIR and backend-lowered AIR if those roles diverge;
- backend-agnostic compiled artifact abstraction.
