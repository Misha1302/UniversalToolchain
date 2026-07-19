# REBASE 2026 submission

Status: proposal.

## Title

Build the Language, Then Make the Abstractions Disappear: From Feature Modules to Typed CIL

## Abstract

UniversalToolchain/Wist2 lets a .NET host assemble a restricted DSL from independent feature modules. Programs are lowered through Bytecode and Abstract IR; the resulting AIR can be interpreted directly or compiled to typed CIL. Modules define the language before execution, but once a compiled artifact is prepared, its operations should not dispatch through the module system.

The talk builds a small pricing DSL and follows it through dialect selection, deterministic runtime planning, Bytecode, AIR, and a DynamicMethod-based CIL backend. The pricing example uses external bindings and arithmetic. A separate regression case adds lexical locals and shadowing, while the restricted dialect demonstrates rejection of syntax for a feature it did not select.

The demonstrated native-load specialization is gated by declared intrinsic capabilities. Frontend visitors do not branch on a concrete backend type; when a required intrinsic is unavailable, the optimizer keeps the corresponding portable AIR sequence.

An earlier implementation got the storage boundary wrong. External bindings and lexical locals were mapped through incompatible assumptions, so the interpreter and CIL backend could disagree on the same program. A backend patch would have hidden the ownership error. Instead, bindings and locals received distinct semantic identities, physical storage mapping moved behind backend contracts, and shared regression tests covered shadowing, nested scopes, unused inputs, and repeated reads and writes.

The code and tests are public and the demonstration is reproducible. UniversalToolchain/Wist2 is still an alpha project, so the talk states the current trade-offs and limits instead of presenting a production deployment or hardened sandbox.

## Short bio

Mikhail Razakov created UniversalToolchain/Wist2, an independent open-source .NET framework for embeddable domain-specific languages. He works across its module system, intermediate representations, interpreter, typed CIL backend, and cross-backend verification, with a focus on runtime specialization and compiler correctness. In summer 2026, he is a compiler engineering intern at MCST and an incoming Software Engineering student at HSE University. He also teaches programming and writes about compilers, virtual machines, and .NET runtime internals.

## Form material URL

https://github.com/Misha1302/Wist2/tree/master/docs/talks/rebase-2026
