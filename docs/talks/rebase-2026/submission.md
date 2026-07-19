# REBASE 2026 submission

## Title

**Build the Language, Then Make the Abstractions Disappear: Engineering an Extensible .NET DSL Runtime**

## Abstract

Extensible language architectures are often modular while they are being built but remain abstract while they execute. UniversalToolchain/Wist2 explores a different boundary: compose a language from independent feature modules, then progressively lower the selected semantics into a deterministic runtime plan and concrete interpreter or typed CIL operations.

In this talk, I will build a restricted pricing DSL from independently selected language modules and trace one program through dialect composition, Bytecode, Abstract IR, and two execution paths: an AIR interpreter and a `DynamicMethod`-based CIL backend. The demonstration covers external bindings, lexical local variables, arithmetic, backend capabilities, and the rejection of language features that were not included in the selected dialect.

The central engineering question is how to preserve extensibility during language construction without keeping language-construction machinery in prepared execution paths. I will show how capability-gated lowering allows supported operations to become concrete runtime or CIL operations while keeping frontend semantics independent from a particular backend.

Specialization also creates a dangerous failure mode: different backends can quietly turn one DSL into two languages. A real regression involving external bindings and local-variable shadowing will serve as a case study. I will explain how explicit semantic identities, storage contracts, deterministic composition, and cross-backend regression tests prevent this divergence.

The audience will leave with a practical architecture for building DSL runtimes that remain open to extension during construction, become concrete before execution, and preserve one language semantics across supported backends.

UniversalToolchain/Wist2 is an independent open-source alpha project. The talk presents a reproducible architecture, implementation experience, design trade-offs, and current limitations rather than claiming production deployment.

## Short bio

Mikhail Razakov is the creator and primary developer of UniversalToolchain/Wist2, an independent open-source .NET framework for building and executing embeddable domain-specific languages. His work focuses on modular language composition, intermediate representations, interpreters, typed CIL generation, runtime specialization, compiler verification, and semantic consistency across execution backends. In summer 2026, he is a compiler engineering intern at MCST and an incoming Software Engineering student at HSE University. He also teaches programming and writes about compilers, virtual machines, and .NET runtime internals.

## Form material URL

`https://github.com/Misha1302/Wist2/tree/master/docs/talks/rebase-2026`
