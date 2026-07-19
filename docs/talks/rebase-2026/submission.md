# REBASE 2026 submission

Authority: proposal.

## Title

**Build the Language, Then Make the Abstractions Disappear: Engineering an Extensible .NET DSL Runtime**

## Abstract

UniversalToolchain/Wist2 lets a .NET host assemble a restricted DSL from independent feature modules, then lower the selected semantics to an AIR interpreter or typed CIL. Language composition belongs in the build phase; a prepared compiled delegate should not dispatch through the module system for every operation.

The talk builds a small pricing DSL and follows it through dialect selection, deterministic runtime planning, Bytecode, Abstract IR, capability-gated lowering, and execution through an AIR interpreter and a `DynamicMethod`-based CIL backend. The demo uses external bindings, lexical locals, and arithmetic, and shows the restricted dialect rejecting syntax for a feature it did not select.

Specialization is gated by declared backend capabilities. Frontend modules never branch on a backend implementation. If a capability is missing, the portable representation remains unchanged.

The first implementation got this boundary wrong. External bindings and lexical locals were mapped through incompatible storage assumptions, so the interpreter and CIL backend could disagree on the same program. The fix was not a backend patch: bindings and locals received distinct semantic identities, storage mapping moved behind backend contracts, composition remained deterministic, and shared regression tests covered shadowing, nested scopes, unused inputs, and repeated reads and writes.

UniversalToolchain/Wist2 is an independent open-source alpha project. This is an implementation case study with public code, reproducible tests, explicit trade-offs, and known limits; it is not a production-deployment or hardened-sandbox claim.

## Short bio

Mikhail Razakov is the creator and primary developer of UniversalToolchain/Wist2, an independent open-source .NET framework for embeddable domain-specific languages. He works on modular language composition, intermediate representations, interpreters, typed CIL generation, runtime specialization, compiler verification, and semantic consistency between execution backends. In summer 2026, he is a compiler engineering intern at MCST and an incoming Software Engineering student at HSE University. He also teaches programming and writes about compilers, virtual machines, and .NET runtime internals.

## Form material URL

`https://github.com/Misha1302/Wist2/tree/master/docs/talks/rebase-2026`
