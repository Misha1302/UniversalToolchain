# Technical Due Diligence Review

> This is an external-style technical review of UniversalToolchain/Wist.
> It is intentionally evaluative: it records strengths, risks, hidden contracts, and roadmap candidates.
> Canonical project facts should still live in the focused documentation files linked below.

## How this document fits into the docs

This document belongs under `docs/reviews/` because it is a review and due-diligence artifact, not the main project entry point and not a normative architecture contract.

Use it together with these canonical documents:

- Project entry point: [Documentation index](../index.md)
- Public positioning and wording: [Project positioning](../project-positioning.md)
- Known limitations and wording boundaries: [Current limitations](../limitations.md)
- Runtime pipeline: [Current canonical runtime pipeline](../current-canonical-runtime-pipeline.md)
- Runtime activation: [Runtime manifest activation model](../runtime-manifest-activation-model.md)
- Manifest schema: [Runtime manifest format](../runtime-manifest-format.md)
- Bytecode/AIR design: [Bytecode and AIR architecture](../architecture/bytecode-and-air.md)
- Backend/parity contracts: [Backends and semantic parity](../architecture/backends-and-parity.md)
- Module extension surface: [Module authoring guide](../guides/module-authoring.md)
- Module hidden contracts: [Module contracts](../contracts/module-contracts.md)
- Alternatives and market positioning: [UniversalToolchain vs nearby alternatives](../alternatives.md)
- Security and trust boundaries: [Security policy](../SECURITY.md)
- Coding and contribution rules: [Project rules](../PROJECT_RULES.md) and [Contributing](../CONTRIBUTING.md)

## Review status

- Scope: architecture, extensibility, module authoring, runtime composition, bytecode/AIR, backend story, docs, and public readiness.
- Nature: review and due diligence, not a replacement for architecture contracts.
- Audience: maintainers, reviewers, contest/jury readers, potential module authors, and future AI-assisted contributors.

## Executive summary

UniversalToolchain is not just a language and not just a parser toolkit. It is best understood as a Wist-first .NET DSL/runtime framework: Wist is the reference language, while the framework tries to provide reusable infrastructure for dialect definitions, runtime manifests, selected runtime plans, module composition, and interpreter/CIL execution paths.

The strongest engineering idea is the separation between language feature composition and runtime selection. Language features are meant to participate through modules across the frontend pipeline, while concrete runtime composition is selected from dialect manifests rather than one hardcoded backend. This matches the story in [Current canonical runtime pipeline](../current-canonical-runtime-pipeline.md) and [Runtime manifest activation model](../runtime-manifest-activation-model.md).

The second strong idea is the bytecode/AIR layer. The bytecode model behaves less like a conventional closed opcode list and more like a semantic merge space where tags and layered operations can collect meaning from multiple visitors. That is unusual and potentially powerful, but it needs strong contracts and validation. See [Bytecode and AIR architecture](../architecture/bytecode-and-air.md) for the canonical architecture document.

The biggest weakness is that the system is still more compositional in spirit than in enforcement. Some important extension points remain partially concrete or Wist-shaped: dialect directive handling, Wist convenience APIs, backend artifact handling, and bytecode tag validation. These are not fatal issues, but they are exactly the areas that should not be oversold in public material. See [Current limitations](../limitations.md) for the public wording boundary.

Bottom line: the project is architecturally promising, technically interesting, and already has a serious documentation posture. It is suitable for a public demo if positioned as a serious framework prototype with a strong reference language. It should not yet be marketed as a polished general-purpose language workbench, a production-grade sandbox, or a frictionless third-party DSL authoring platform.

## What the project actually is

The most accurate one-sentence description is:

> UniversalToolchain is a Wist-first modular .NET DSL/runtime framework prototype with manifest-backed runtime selection, explicit architecture guardrails, and a reference language used to validate the framework.

That wording is consistent with [Project positioning](../project-positioning.md). It avoids two common mistakes:

- calling the repository only a Wist compiler;
- calling it a finished universal language workbench.

The practical split is:

- **UniversalToolchain** owns the reusable framework story: dialect definitions, build plans, runtime manifests, selected runtime plans, deterministic activation, backend/intrinsic contracts, and composition rules.
- **Wist** owns one concrete language and its convenience experience: shipped profiles, syntax/modules/examples, CLI behavior, facade usage, and Wist-specific backend declarations.

This split is strong enough to present publicly, but it is not yet fully enforced everywhere in the codebase. The Wist convenience surface is useful, but it must remain a wrapper over the selected-runtime pipeline rather than becoming the real framework API.

## Strong architectural assets

### 1. Manifest-backed runtime selection

The canonical runtime story is one of the project’s strongest parts:

```text
dialect source -> dialect compilation -> build plan -> manifest-backed runtime selection -> host creation -> execution
```

This gives the project a better story than a normal expression evaluator. The runtime surface is not just “whatever the parser supports”; it can be constrained by dialect selection and manifest-backed activation.

### 2. Real module-oriented frontend pattern

The frontend module pattern is recognizable and learnable. A typical feature module contributes lexemes, parser node creators, AST visitors, capabilities, and sometimes runtime behavior. This is a strong base for extensibility, and it should be documented as the official module-authoring recipe.

The risk is that much of the contract is still convention-heavy: token names, parser priorities, visitor ownership, shared state, and backend capability expectations. That is why [Module authoring guide](../guides/module-authoring.md) and [Module contracts](../contracts/module-contracts.md) should be treated as mandatory reading for contributors.

### 3. Bytecode/AIR as semantic layers

The bytecode/AIR idea is not decorative. It is one of the project’s most distinctive technical ideas. The system is trying to preserve semantic structure through intermediate layers, which can later support optimization, validation, backend lowering, and diagnostics.

The risk is that powerful semantic tags can become undocumented string contracts. The next maturity step is not to remove tags, but to formalize and verify them.

### 4. Honest public positioning

The current docs already avoid a lot of overclaiming. [Project positioning](../project-positioning.md), [Current limitations](../limitations.md), [Security policy](../SECURITY.md), and [UniversalToolchain vs nearby alternatives](../alternatives.md) give the project a much stronger public posture than a typical prototype repository.

That honesty is valuable: it lets the project look serious without pretending to be production-complete.

## Main risks and hidden contracts

### 1. Dialect DSL extensibility is weaker than runtime composition

Runtime selection is becoming manifest-driven, but the dialect-definition language itself is not equally open yet. If new directive families require editing central framework code, the system is not fully compositional at the dialect-DSL level.

Recommended direction: make dialect directive handling DI-composable, deterministic, and testable.

### 2. Backend abstraction leaks through convenience APIs

Interpreter and CIL are both important, but Wist-facing code should not need to know concrete artifact shapes such as interpreter IR versus CIL/DynamicMethod artifacts. If a third backend would require facade changes, the backend abstraction is not mature enough.

Recommended direction: introduce a backend-agnostic executable/compiled artifact contract and keep backend selection inside the selected runtime plan.

### 3. Module authoring is learnable but fragile

A module author can follow existing examples, but the examples do not fully explain the hidden contracts. AI-generated modules are especially risky because a model can copy the surface structure while missing parser priority, token ownership, visitor filtering, bytecode tags, or shared-state constraints.

Recommended direction: add sample modules, golden tests, and machine-checkable contracts.

### 4. Bytecode tags need validation

Bytecode tags are a strong idea, but raw string tags without a taxonomy or verifier create long-term correctness risks. Tags should become a documented contract with validation and producer/consumer expectations.

Recommended direction: centralize tag declarations and add a verifier for unknown tags, invalid instruction shapes, and stack-affect invariants where possible.

### 5. Performance claims need stronger evidence

The architecture is designed to support optimized compiled execution, but public “near C# speed” claims should not be made without current reproducible benchmarks. Interpreter correctness and CIL performance are separate claims and should be measured separately.

Recommended direction: keep benchmarks reproducible, separate compile time from execution time, and publish methodology before making performance claims.

## Audience-specific readiness

### Product user

There is a usable Wist-first path through CLI examples, shipped dialects, and the Wist facade. This is good enough for demos and controlled internal experiments.

Current readiness: moderate.

Missing: clearer product-level examples, stronger stability expectations, and a sharper distinction between Wist convenience APIs and generic framework APIs.

### DSL author

The project is plausible as a foundation for new DSLs, but the generic authoring path is not yet as polished as the Wist path.

Current readiness: promising but not frictionless.

Missing: generic runtime builder, more sample DSLs, and a guide that separates framework-level APIs from Wist-only APIs.

### Module author

The module pattern is real and consistent, but convention-heavy.

Current readiness: usable for an experienced contributor.

Missing: formal module templates, explicit token/priority/visitor contracts, and tests that catch broken module ownership.

### Backend author

The interpreter/CIL split is conceptually strong, but backend artifact handling is not abstract enough yet.

Current readiness: early.

Missing: backend-agnostic artifact contracts, parity test matrix, and clearer supported-intrinsics contracts.

### AI-assisted contributor

The project has enough structure for AI to help with focused tasks, but not enough machine-checkable contracts to trust broad generated changes.

Current readiness: risky.

Missing: AI-safe templates, guardrail tests, and stricter architecture checks.

## Scores

| Area | Score | Notes |
| --- | ---: | --- |
| Architectural idea | 8/10 | Build-plan plus manifest-selected runtime is strong. |
| Modularity implementation | 6/10 | Frontend modules are composable; dialect directives and Wist seams are still not fully open. |
| API clarity | 5/10 | Wist facade is approachable; generic framework surface is thinner. |
| Documentation quality | 7/10 | Strategy docs are strong; authoring docs still need to become more operational. |
| Backend architecture | 6/10 | Interpreter/CIL split is clear, but facade/backend artifact leakage remains. |
| Performance architecture | 5/10 | Good intent, insufficient proof, possible representation/dispatch overhead. |
| Tests | 7/10 | Architecture-aware test naming and structure are promising. |
| Extensibility | 6/10 | Good for classic frontend modules, weaker for dialect DSL and backend extension. |
| Suitability for new DSLs | 6/10 | Plausible foundation, still Wist-first in key areas. |
| Public convincingness | 7/10 | Strong if positioned honestly; weak if oversold. |
| AI-friendliness | 4/10 | Too much still depends on hidden conventions. |
| Module-author readiness | 5/10 | Learnable, but not yet safe for casual extension. |
| Product-user readiness | 5/10 | Wist usage path exists; framework-as-product path is still thin. |

## Public positioning guidance

### Show publicly

- Manifest-backed dialect/runtime selection.
- Wist as a reference implementation, not the whole architectural truth.
- Interpreter and CIL as two execution paths for the selected language.
- Restricted dialect profiles and controlled runtime surfaces.
- Architecture guardrails and honest limitations.

### Avoid claiming too strongly

- Fully universal language workbench.
- Production-grade sandboxing.
- Near-C# performance without current benchmark evidence.
- Arbitrary backend extensibility with no Wist-facing changes.
- AI-safe module generation by convention alone.
- Fully formal bytecode tag validation before verifier coverage exists.

## Roadmap candidates

### Fast wins

- Keep the README concise and point deeper readers to focused docs.
- Make this review discoverable from the documentation map or a docs index.
- Strengthen module authoring examples with a minimal feature module and a cross-cutting feature module.
- Add a bytecode/AIR guide that explains tags, layered operations, and verifier expectations.
- Add explicit “Wist convenience versus framework API” wording where needed.

### Medium effort

- Replace static dialect directive handling with DI-registered directive handlers.
- Extend dialect groups beyond modules/capabilities to include optimizers, intrinsic policies, and backend defaults.
- Add bytecode tag validation and a producer/consumer matrix.
- Add architecture tests that prevent Wist convenience APIs from becoming framework truth.
- Add public semantic parity tests that compare interpreter and CIL on shipped dialects.

### Hard but strategically important

- Introduce backend-agnostic compiled/executable artifact contracts.
- Separate semantic IR from backend-ready lowered IR more explicitly if that boundary is currently blurred.
- Make runtime component kinds stricter, or explicitly formalize multi-layer components.
- Add machine-checkable contracts for AI-generated module changes.
- Build a generic runtime-builder path and make the Wist facade wrap it.

## Candidate issues or PRs

1. Make dialect directive handling DI-composable instead of static.
2. Add a bytecode contract document and bytecode verifier.
3. Add a backend-agnostic executable artifact abstraction.
4. Generalize dialect groups to include optimizers, backend defaults, and intrinsic policies.
5. Publish an official module-authoring guide with tested sample modules.
6. Add AI-safe golden tests for new module creation.
7. Separate Wist convenience API from generic runtime-builder API.
8. Add public semantic-parity tests for interpreter and CIL backends.

## Suggested implementation prompts

### Prompt one — make dialect directives composable

Goal: remove the static hidden registry from dialect semantic binding.

Problem: dialect directive handling should be extensible without editing central framework code.

Change: introduce an `IDialectDirectiveHandler` registration model through DI, ordered deterministically, and make the binder consume the registered handlers rather than a static field. Preserve current behavior for existing directive families.

Tests to add:

- deterministic handler-order contract test;
- extension test proving a new handler can be added by service registration only;
- regression tests preserving current diagnostics.

### Prompt two — formalize bytecode tags and add a verifier

Goal: turn bytecode tags from raw convention into a checked contract.

Change: introduce a typed tag wrapper or centralized tag registry, then add a verifier that checks unknown tags and basic instruction-shape invariants.

Tests to add:

- valid-bytecode passes;
- unknown-tag bytecode fails;
- representative feature visitors produce allowed tags only.

### Prompt three — remove backend leakage from the Wist facade

Goal: make the facade stop branching on concrete backend IDs and artifact types.

Change: design a backend-agnostic executable artifact interface returned by the host/compiler path so the facade can request execution by backend alias without knowing concrete artifact types.

Tests to add:

- facade smoke tests for interpreter and CIL;
- regression tests for unknown backends;
- architecture guardrail test ensuring the facade no longer mentions concrete backend artifact classes.

### Prompt four — upgrade dialect groups into real composition bundles

Goal: make groups useful for product-level language composition.

Change: extend group descriptors and expansion so groups can contribute optimizer directives, intrinsic directives, and optional backend defaults while preserving deterministic conflict diagnostics.

Tests to add:

- group expansion for modules plus optimizers plus intrinsics;
- conflict diagnostics;
- regression tests for existing groups.

### Prompt five — ship official module-authoring templates

Goal: make module creation safe for humans and AI.

Change: add a minimal sample module and a cross-feature sample module, both compiled and tested. The guide should explain attributes, lexemes, node creators, visitor ownership, bytecode emission, and backend contracts.

Tests to add:

- sample module registration tests;
- parser/translator golden tests;
- guardrail tests for expected module attribute/interface pattern.

## Review limitation

This review is a technical assessment, not a formal proof. It focuses on the strongest architectural seams, representative modules, dialect/build-plan/runtime-selection, bytecode/AIR design, and public readiness. It does not replace exhaustive backend, optimizer, benchmark, or CI audits.
