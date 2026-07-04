---
status: proposal
implementation_status: not-started
last_verified: 2026-07-04
current_truth: ../../CURRENT_ARCHITECTURE_STATUS.md
---

# Optional Flame SSA optimizing backend

Status: **Proposed**  
Implementation status: **Not started**  
Document type: architecture and product design proposal  
Primary owners: UniversalToolchain runtime, AIR, backend, capability, and managed interop layers  
Reference implementation candidate: [Flame](https://github.com/jonathanvdc/Flame)  
Design date: 2026-06-14

> This document describes a possible future integration. Nothing in this document is current runtime behavior unless another current-state document says so explicitly.

## Design dossier contents

This proposal is split into focused chapters so that implementation work can reference stable design areas without treating the proposal as current behavior.

1. [Context, motivation, goals, and architecture laws](01-context-motivation-and-laws.md)
2. [Target architecture, alternatives, identity, and packaging](02-target-architecture-and-packaging.md)
3. [AIR CFG, verification, stack-to-SSA, types, and storage](03-air-cfg-and-stack-to-ssa.md)
4. [Intrinsics, capabilities, managed interop, and effects](04-intrinsics-capabilities-interop-and-effects.md)
5. [Optimization, artifacts, explainability, and caching](05-optimization-artifacts-reports-and-caching.md)
6. [Security, parity, testing, diagnostics, and licensing](06-security-parity-testing-and-licensing.md)
7. [Rollout, implementation plan, acceptance, and success criteria](07-rollout-implementation-and-acceptance.md)

## 1. Executive summary

UniversalToolchain should consider adding an optional optimizing backend that lowers verified AIR into an SSA control-flow graph, applies compiler analyses and optimization passes, and emits managed CIL. Flame is a strong technical candidate for that role because it already provides:

- an SSA-based intermediate representation;
- immutable control-flow graphs with mutable builders;
- block parameters that model phi-like value flow;
- analyses for dominators, predecessors, uses, liveness, reachability, nullability, and memory;
- optimization passes such as constant propagation, global value numbering, dead-value elimination, control-flow simplification, inlining, scalar replacement, and tail-recursion elimination;
- a CLR-aware type system;
- CIL import and emission through Mono.Cecil;
- partial LLVM-oriented infrastructure.

The integration must not replace UniversalToolchain AIR, redefine Wist semantics, or make Flame a mandatory dependency of the framework. The correct architectural relationship is:

```text
UniversalToolchain owns:
  language composition
  dialect selection
  syntax and semantic lowering
  Bytecode and AIR contracts
  intrinsic policy
  capability policy
  trust boundaries
  semantic parity

Optional Flame backend owns:
  AIR-to-SSA projection
  backend-local SSA optimization
  CIL artifact emission
  backend-local optimization diagnostics
```

The intended public execution model is:

```text
one selected language and one semantic contract
    -> interpreter       for reference semantics and diagnostics
    -> cil               for low compilation latency and fast dynamic invocation
    -> optimized-cil     for deeper optimization of larger compile-once/run-many workloads
```

The user should select an execution characteristic, not a third-party implementation name. Therefore the proposed canonical backend id is `optimized-cil`; `flame` may exist only as an implementation-oriented alias for diagnostics or advanced configuration.

The largest prerequisite is not the Flame package itself. It is a shared, backend-neutral AIR verification and control-flow analysis layer. A correct SSA backend requires complete handling of labels, predecessors, fallthrough, loops, fixed-point stack-state propagation, merge validation, and typed block inputs. That functionality should become part of UniversalToolchain and should benefit every backend.

The main release blocker is licensing. The inspected Flame repository declares `GPL-3.0-or-later`, while UniversalToolchain is Apache-2.0. A direct same-process package dependency must not be shipped until the project has a documented legal and distribution decision, preferably dual licensing or explicit permissive permission from the Flame copyright holder.

## 2. Decision summary

The proposed decision is:

1. Preserve Bytecode and AIR as UniversalToolchain-owned semantic boundaries.
2. Add a backend-neutral AIR CFG builder and verifier before implementing Flame lowering.
3. Add a backend-neutral value-flow representation only if direct verified-AIR-to-Flame lowering would otherwise mix generic stack analysis with Flame-specific object construction.
4. Add Flame as an optional backend package, never as a dependency of BasicCore, Wist frontend modules, dialect parsing, or the default Wist package.
5. Keep `compiler` mapped to the current `cil` backend.
6. Introduce `optimized-cil` as explicit opt-in until measurements justify any automatic tiering policy.
7. Generalize C#-named runtime call concepts toward CLR-neutral managed calls without breaking existing compatibility contracts.
8. Expand purity metadata into an effect model sufficient for legal SSA transformations.
9. Provide compilation reports, compatibility diagnostics, IR inspection, and artifact provenance as user-facing value.
10. Treat semantic parity and observable-effect parity against the interpreter as release gates.
11. Resolve Flame licensing before distributing the integration.
