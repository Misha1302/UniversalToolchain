---
status: archive
last_verified: 2026-07-04
current_truth: ../../CURRENT_ARCHITECTURE_STATUS.md
---

# Technical Due Diligence on Wist2

This is a historical broad project snapshot. Use
`docs/CURRENT_ARCHITECTURE_STATUS.md` for current implementation truth before
acting on any claim here.

## Executive summary

This repository is not “just a language” and not “just a parser toolkit.” It is trying to be an embeddable .NET DSL/runtime framework, with Wist as the proving language, and with a canonical dialect pipeline that goes from dialect source to a build plan, then to manifest-backed runtime selection, then to a Wist host. The docs and AI-agent rules are explicit about that distinction, and they are one of the project’s strongest assets.

The strongest engineering idea in the code I verified is this: language features are meant to be composed from modules that participate across the frontend pipeline, while runtime composition is selected by dialect manifests rather than one hardcoded “mode.” That idea is real in the code, not just in README prose. The dialect runtime path, manifest format, selected-runtime plan, and module classifier all exist and are wired together.

The second strong idea is the bytecode model itself. `BytecodeInstruction` is not a single opcode record; it is a pair of `HashSet<string> Tags` and layered operations (`LevelCollection<float, IAbstractMethodConvertable>`). Combined with an AST translator that simply gives every visitor a chance to act on every node, this makes bytecode a semantic merge space rather than a closed instruction stream. That is unusual and potentially powerful. It also creates many hidden contracts.

The biggest weakness is that the system is more compositional in spirit than in enforcement. Important extension points are still partially hardcoded: dialect directive handling is a static in-process registry, the Wist runtime always injects a required `ProgramStructureFrontendModule`, and the convenience facade hardcodes backend branching into “interpreter => AIR artifact” and “cil => DynamicMethod artifact.” Those are defendable shortcuts for a reference language, but they weaken the story that “any language can be assembled purely by composition.”

For a product user, there is a usable Wist-first happy path. For a generic framework user who wants to build new DSLs without reading internals, the story is not there yet. For a module author, the pattern is consistent and learnable, but fragile: token names, parser priorities, visitor ordering, and ad hoc shared state are doing a lot of work. For an AI code generator, the project is not yet safe enough: the patterns exist, but the contracts are mostly implicit.

My bottom-line judgment is: architecturally promising, technically interesting, honestly documented, but not yet a clean “UniversalToolchain product” for third-party DSL authors. It is ready for a public demo if the demo is positioned as a serious framework prototype with a strong reference language and explicit limitations. It is not ready to be oversold as a polished, general-purpose language workbench or a production-grade compiler platform.

## What the project actually is

The repository itself describes **UniversalToolchain** as the primary product and **Wist** as the reference language. The README positions it as an embeddable .NET DSL/runtime framework for scenarios where an expression evaluator is no longer enough, and the AI-agent rules reinforce that Wist is supposed to be the proving ground, not the architectural truth. That high-level framing is consistent between the public docs and the internal architecture rules.

The core practical boundary is this. UniversalToolchain owns the framework story: dialect definitions, build plans, runtime manifests, selected runtime plans, backend/runtime activation, and general composition rules. Wist owns the orchestration and convenience surface for one concrete language: Wist workflow composition, Wist execution configuration, Wist host creation, Wist facade, shipped Wist presets, and Wist backend declarations. The canonical runtime-pipeline and activation docs say that explicitly, and the code follows that split in `UniversalToolchain.Dialects.Integration` versus `UniversalToolchain.Dialects.Wist`.

Compared with NCalc, Dynamic Expresso, RulesEngine, ANTLR-style parser-first tools, or a richer platform like MPS, the project is aiming at a middle layer: a restricted, embeddable runtime stack where the language surface, selected capabilities, and runtime surface all matter. The alternatives doc is actually quite honest here and is one of the strongest public-positioning artifacts in the repo. It does not pretend to beat smaller evaluators on convenience or larger workbenches on tooling.

The most accurate description is therefore: **a modular, Wist-first compiler/runtime framework prototype that already has a real dialect-selection architecture, but still exposes several Wist-specific and implementation-specific seams.** That is stronger and more honest than calling it merely a DSL, merely a compiler, or a finished “universal toolchain.”

**Three concise formulations**

For a programmer: **A Wist-first .NET framework for embedding controlled DSL runtimes with selectable modules, backends, and policies.**

For a jury or teacher: **A modular compiler/runtime architecture experiment that uses dialect build plans and manifest-backed runtime selection to separate language design from execution wiring.**

For a potential DSL user: **A way to package a restricted mini-language inside a .NET app, run it via interpreter or CIL, and control which language features exist.**

## Architecture and hidden contracts

The canonical dialect runtime pipeline is clearly stated in the docs and matches the concrete Wist workflow class: dialect source is compiled, converted into a `DialectBuildPlan`, resolved against runtime manifests into a `SelectedRuntimePlan`, mapped into Wist execution configuration, and finally turned into a Wist execution host. This is the cleanest architectural spine in the repo.

The nice part is that runtime selection is genuinely **manifest-backed and selection-driven**. `FileBasedRuntimeComponentCatalog` loads manifest files, normalizes alias maps and activation metadata, and `SelectedRuntimePlanResolver` resolves the selected modules, backends, and optimizers from the build plan. `DefaultRuntimeComponentResolver` then uses exact activation metadata when present, with legacy assembly scanning retained as fallback. That means the docs’ claim that reflection still exists but is supposed to be bounded and exact in the canonical path is backed by code.

The less nice part is where extension points stop being truly open. `DialectDefinitionSemanticBinder` hardcodes directive handling through one static registry containing six handlers: module, backend, intrinsic, optimizer, security, capability. That means if you want to extend the **dialect DSL itself**, you are not just adding a provider or descriptor; you are editing framework code. This is one of the main reasons I would not yet call the dialect-definition layer a fully general DSL framework. It is compositional for runtime components, but not yet equally compositional for the dialect language that describes them.

Another hidden contract is the builder completeness requirement. `DialectDefinitionBuilder.Build()` throws if any policy section was not set beforehand, including “empty by design” sections such as security or order rules. That creates a non-obvious requirement on every binding source and directive pipeline: every policy bucket must be initialized even when nothing was declared. That is not wrong, but it is a contract currently enforced as a runtime invariant rather than expressed in a higher-level API.

A third hidden contract is the category model for modules. `SelectedRuntimeModuleClassifier` classifies selected runtime “modules” by checking whether the activation type implements `IFrontendCoreModule` and/or `IIRProcessingModule`, but those objects are still coming from runtime entries of kind `FrontendModule`. In practice, that means the project is allowing one selected runtime component to participate in more than one layer, which is flexible, but it also means the manifest kind system is not a strict architecture boundary. This is an elegant shortcut and a leaky abstraction at the same time.

The last major hidden contract sits in the AST-to-bytecode translator. `BasicAstToBytecodeTranslatorImpl` does not do node dispatch by ownership; it creates a request translator and then calls `TryVisit` on **every configured visitor** for the node. There is no visible arbitration layer. Therefore every visitor must self-filter correctly, must not double-emit accidentally, and must rely on node types, tags, or internal state to know whether it owns the node. This pattern is very flexible, but it is one of the biggest reasons AI-generated modules can break the system without immediate compile-time feedback.

## Modules and dialects

The frontend-module authoring pattern is consistent across the language feature modules I inspected. A module class typically carries `DialectModuleAlias`, `DialectCapabilityProvider`, `DialectRuntimeExport`, and `AutoRegisterService` attributes, implements `IFrontendCoreModule`, registers lexemes in `InitLexer`, parser node creators in `InitParser`, and AST visitors in `InitAstTranslator`. Arithmetic, numbers, variables, conditions, loops, labels, and scopes all follow that template closely. This is the best “accidental framework” in the repo: there is already a recognizable grammar for how a feature gets added.

That pattern also exposes the main hidden contracts a module author must know. Token identifiers are strings such as `"Addition"`, `"Number"`, `"Let"`, `"If"`, and `"Goto"`. Precedence is partially encoded as floating-point registration priorities for node creators. Ownership between modules is not enforced by the type system; it is stabilized by lexical names, parser priorities, visitor ordering, and local conventions. Labels are a good example: `LabelsModuleImpl` uses a `LabelsSharedData` instance passed manually into both `LabelsVisitor` and `GotoVisitor`, which means cross-feature coordination still sometimes depends on hand-rolled shared state rather than a formal framework service.

So, for a module author, writing a **classic frontend feature** is moderately straightforward once you learn the house style. Writing a **new dialect DSL directive**, a **new backend**, or a **cross-cutting optimization** is not equally straightforward. That is a real asymmetry in the current system. “Module authoring” exists as a pattern; “framework extension authoring” is only partly lifted into formal APIs.

The dialect layer is stronger than the raw module layer in one specific way: its data model is explicit and mostly immutable. `DialectDefinition`, `ModulePolicy`, `OrderRule`, and `DialectBuildPlan` are all normalized boundary models, and `DialectSemanticNormalization.ResolveOrder` does deterministic ordering with cycle diagnostics. That is good architecture. It means dialects are not just “bags of modules”; they are normalized configurations with explicit backend, intrinsic, optimizer, security, capability, and ordering semantics.

However, groups are still thin. `DialectGroupExpander` expands only `use`-module aliases and capability flags, and `DialectGroupDescriptor` currently contains only `IncludedModules` plus `Capabilities`. So a hypothetical `ArithmeticGroup` that also bundles native-math intrinsics, optimizer directives, backend defaults, and validation policy does **not** exist yet as a first-class abstraction. The repository has the beginning of group composition, not the full thing.

For a product user, the Wist facade gives a usable happy path: `WistRuntimeFacadeBuilder.CreateDefault()`, optional shipped preset or file-based dialect choice, then `Run`/`TryCompile`. But the facade is Wist-specific, builds its own DI container, and defaults to shipped Wist presets or a dialect file path. That is good enough for demos and first contact, not yet a generic framework-level product API.

For an AI code generator, the danger is not “too much boilerplate”; the danger is **implicit semantics**. A model can imitate the surface template of a module class, but it is much harder for it to infer correct priority numbers, token-name conventions, visitor emission discipline, ordering requirements, and whether state sharing is needed. Until those contracts are formalized or generated, AI-written modules will be fragile.

## Bytecode, AIR, and backends

The bytecode model is the most technically interesting part of the older pipeline. `Bytecode` is a list of `BytecodeInstruction`, and each instruction contains two things: a `HashSet<string>` of **tags** and a **priority-layered set of operations** (`LevelCollection<float, IAbstractMethodConvertable>`). This is not a conventional “one opcode per instruction” design. It suggests that bytecode is functioning as a composition-normalization layer where multiple visitors can contribute semantics to the same logical point before later translation. That is a strong idea.

The translator implementation reinforces that reading. `BasicAstToBytecodeTranslatorImpl` creates a bytecode object and then, for every AST node, runs every visitor against that node through `TryVisit`. Since there is no visible exclusive dispatch, the translator is effectively a controlled merge process. That makes tags and priority-layered ops meaningful: they are how multiple feature visitors can coexist without one monolithic lowering pass. This is the strongest “not obvious from README” architectural pattern I found.

But the same design is also the source of fragility. Tags are raw strings, not typed descriptors. I did not verify a bytecode-tag verifier, a tag taxonomy, or a full producer/consumer matrix for tags in the Wist runtime. So my best high-confidence conclusion is: **the storage model for bytecode tags is real and central, but the safety model around tags is currently weak or at least not obvious from the slice I inspected.** Treat the conceptual idea as strong and the current enforceability as incomplete.

AIR is not decorative in the repository. In the dialect-definition frontend, `DialectDefinitionSliceCompiler` compiles AIR into `DialectDefinitionSlice`, and `DialectDefinitionSliceAirReader` reconstructs the dialect model by reading AIR instruction metadata annotations, collecting dialect name, version, modules, backends, intrinsics, optimizers, security profile, and capabilities into a `DialectDefinitionAggregation`. That is hard evidence that metadata-bearing intermediate representations are already used as real semantic carriers in at least one subsystem.

What I cannot honestly claim from the inspected slice is a full, verified story for Wist AIR instruction semantics, IR verifier coverage, or a complete taxonomy of backend intrinsics. The facade and backend declarations do show the intended architecture: interpreter is exposed as one backend, CIL as another, and the README explicitly frames compiler/interpreter duality as a core feature. The façade branches on backend identity and artifact type, though, which means backend polymorphism has not been fully lifted into a common artifact story. Adding a third serious backend would likely force changes in Wist-facing code today.

The project’s own philosophy that the interpreter should be a semantic oracle and CIL should be the performance backend is architecturally plausible, but not yet fully demonstrated by the evidence I collected. The repository has the right object boundaries for that story; it does not yet prove the parity/performance claim to the standard I would require before a public “near C# speed” claim.

## Quality, tests, performance, and readiness

The documentation is better than the average prototype repository. The README is candid about scope, the alternatives doc avoids category confusion, the architecture docs describe the *current* canonical execution path rather than a fantasy roadmap, and `AGENTS.md` plus `PROJECT_RULES.md` make architectural intent unusually explicit. This is a real strength for public review and for future contributors.

The weakness is that the repo itself admits the rules are partly aspirational. `PROJECT_RULES.md` explicitly says the repository still contains mixed-language comments and mixed null-check styles and should be treated as a target standard for cleanup rather than as a claim of current compliance. So if you present the rulebook publicly, you should also present it as a cleanup policy, not as a proven invariant.

The test surface looks serious, especially around dialect architecture, runtime selection, and guardrails. The solution includes dedicated test projects for the base system, modules, and dialects, and the dialect test search results show focused test files such as `DialectBuildPlanBuilderConsistencyTests`, `DiagnosticsAndPlanTests`, `MinimalRuntimeSelectionTests`, `SelectedRuntimePlanResolverContractTests`, `SemanticValidationTests`, `WistRuntimeManifestMetadataValidationTests`, and `WistRuntimePathGuardrailTests`. Even without reading every file, that naming pattern is meaningful: architecture contracts are being tested, not just syntax examples.

What I did **not** verify sufficiently is direct semantic parity coverage between interpreter and CIL for the full Wist language, detailed optimizer-correctness coverage, or CI workflow depth. The README shows multiple test projects and build commands, but I did not complete a workflow-file audit, so my readiness judgment on CI is conservative.

On performance, my high-confidence findings are architectural rather than benchmark-based. The bytecode representation allocates a tag set and layered-op structure per instruction. The AST-to-bytecode translator runs every visitor for every node. The Wist facade re-discovers backend mode at runtime and compiles through generic artifact paths. Reflection is bounded to runtime infrastructure, but still present. All of these are plausible overhead sources. I did not inspect a benchmark corpus deeply enough to quantify them, so claims about absolute speed would be premature.

**High-confidence problems to fix before a public demo**

The first is the mismatch between the “UniversalToolchain” story and the still-hardcoded Wist seams: static dialect directive registry, Wist-required infrastructure module injection, shipped-preset bias in the facade builder, and backend-type branching in the facade. Those are architectural debts, not cosmetic issues.

The second is the hidden-contract density in module authoring: string token names, float priorities, self-filtering visitors, and manual shared state. These make the codebase learnable for the author, but risky for outsiders and for AI-assisted extension.

The third is the lack of a clearly explained, validated bytecode-tag contract. The design is promising enough that it should be documented and formalized, not left as an internal convention.

## Roadmap, scores, and Codex prompts

**What to show publicly now**

Show the project as a **modular DSL/runtime framework prototype** with a serious Wist reference language, a dialect build-plan system, manifest-backed runtime activation, and a real split between interpreter and CIL backends. Show the alternatives document and the current canonical runtime pipeline early. Show module composition and constrained dialects. Do **not** oversell generic third-party language authoring, backend authoring, or performance parity with hand-written C# until those are proven more directly.

**What to hide, downplay, or explicitly label as design-in-progress**

Downplay “universal” claims that imply a frictionless general language workbench. Explicitly label the dialect-definition DSL as partially hardcoded. Explicitly label bytecode tags as an internal semantic mechanism that still needs a formal contract. Explicitly label the Wist facade as a Wist convenience layer, not the final framework API.

**Very fast wins**

Document the canonical “module feature pattern” as an official recipe: attributes, lexemes, node creators, AST visitors, tests, and guardrails. Document the hidden contracts around token names, priorities, visitor ownership, and self-filtering. Add a `dotnet new`-style internal template or sample module. Add a “bytecode and AIR” guide. Add a “how to create your own dialect” guide that distinguishes Wist-only APIs from generic framework APIs.

**Medium effort but important**

Replace the static dialect directive registry with DI-registered directive handlers. Formalize module groups so they can expand not only modules and capabilities but also optimizers, backend defaults, and possibly intrinsic policies. Add a bytecode verifier that checks tag validity, instruction shape, and stack-affect expectations where possible. Add architecture tests that explicitly assert the Wist facade is a thin wrapper and does not become framework truth.

**Hard but strategically important**

Unify backend artifact handling so Wist-facing code does not branch on concrete backend IDs and concrete artifact types. Split “semantic IR” from “backend-ready lowered IR” more explicitly if that split is currently blurred. Make runtime-component kinds stricter so “frontend module that also acts as IR module” is either formalized or separated. Add machine-checkable contracts for AI-generated modules.

**Readiness scores**

- Architectural idea: **8/10**. The build-plan plus manifest-selected runtime story is strong and real.  
- Modularity implementation: **6/10**. Frontend features are composable, but dialect directives and some Wist seams are still hardcoded.  
- API clarity: **5/10**. The Wist facade is approachable; the generic framework surface is not yet equally approachable.  
- Documentation quality: **7/10**. Strategy and positioning docs are strong; authoring guides are missing.  
- Backend architecture: **6/10**. Clear split exists, but Wist-facing backend leakage remains.  
- Performance architecture: **5/10**. Good intention, insufficient proof, probable representation/dispatch overhead.  
- Tests: **7/10**. The suite appears architecture-aware and well-partitioned, though I did not verify all parity coverage.  
- Extensibility: **6/10**. Good for classic frontend modules, weaker for dialect-DSL and backend extension.  
- Suitability for new DSLs: **6/10**. Plausible foundation, but still Wist-first in too many key places.  
- Public convincingness: **7/10**. Strong story if positioned honestly; weak if oversold.  
- AI-friendliness: **4/10**. Patterns exist, but too much depends on hidden conventions.  
- Module-author readiness: **5/10**. Learnable for an experienced contributor, not yet safe for casual extension.  
- Product-user readiness: **5/10**. Wist usage path exists; framework-as-product path is still thin.

**Roadmap**

In one day: document module authoring, document bytecode/AIR semantics as currently implemented, add explicit limitations to README, and remove any public wording that implies frictionless universal extension.

In one week: replace the static dialect directive registry with DI composition, add module-template tests, formalize group expansion beyond modules/capabilities, and add architecture tests for backend leakage in the facade.

In one month: add verifier layers for bytecode and AIR, unify backend artifact abstraction, create a truly generic runtime builder alongside the Wist facade, and make AI-safe module templates with golden tests.

**Strongest README sentence**

UniversalToolchain is a modular .NET DSL/runtime framework that lets you compose a restricted language from modules, compile dialects into deterministic build plans, and run the same language through interpreter or CIL execution paths.

**Most honest limitations sentence**

Today the project proves a strong Wist-first architecture for modular language composition and manifest-backed runtime selection, but generic dialect-DSL extension, backend extensibility, bytecode-tag validation, and outsider-friendly module authoring are still incomplete.

**Files and classes to study first**

Start with `readme.md`, `docs/current-canonical-runtime-pipeline.md`, `docs/runtime-manifest-activation-model.md`, `AGENTS.md`, `UniversalToolchain.Dialects.Wist/WistDialectExecutionWorkflow.cs`, `UniversalToolchain.Dialects.Core/DialectDefinitionSemanticBinder.cs`, `UniversalToolchain.Dialects.Integration/SelectedRuntimePlanResolver.cs`, `UniversalToolchain.Dialects.Integration/DefaultRuntimeComponentResolver.cs`, `BasicCore/TranslatorWrapper/BytecodeInstruction.cs`, `BasicCodeTranslator/BasicAstToBytecodeTranslatorImpl.cs`, and one representative feature chain such as `ArithmeticModuleImpl`, `NumbersModuleImpl`, `VariablesModuleImpl`, and `ConditionsModuleImpl`.

**Concrete PRs or issues worth creating**

- Make dialect directive handling DI-composable instead of static.  
- Add a bytecode contract document and bytecode verifier.  
- Add a backend-agnostic compiled-artifact abstraction to remove `DynamicMethod`/`IAbstractIR` branching from the facade.  
- Generalize dialect groups to include optimizers, backend defaults, and intrinsic policies.  
- Publish an official module-authoring guide with one minimal feature template and one cross-cutting feature template.  
- Add AI-safe golden tests for “new module should not break parser or bytecode semantics.”  
- Separate Wist convenience API from generic runtime-builder API more sharply.  
- Add public semantic-parity tests that explicitly compare interpreter and CIL on shipped dialects.

**Open questions and limitations of this review**

I verified the strongest architectural seams, representative modules, the dialect/build-plan/runtime-selection pipeline, and the bytecode storage model. I did **not** complete a full audit of every backend implementation file, optimizer implementation, bytecode-tag producer/consumer, benchmark suite, or CI workflow. Where I discuss full parity, optimizer maturity, or performance relative to hand-written C#, treat that as a conservative judgment based on architecture and available public evidence, not as a completed formal proof.

**Prompts for Codex**

**Prompt one — make dialect directives composable**  
Goal: remove the static hidden registry from dialect semantic binding.  
Problem: `DialectDefinitionSemanticBinder` hardcodes directive handlers, so extending the dialect DSL requires editing framework code.  
Study: `UniversalToolchain.Dialects.Core/DialectDefinitionSemanticBinder.cs`, `UniversalToolchain.Dialects.Core/Binding/DialectDefinitionBuilder.cs`, existing directive handlers under `UniversalToolchain.Dialects.Core.Binding.Handlers`, and `WistDialectCoreServiceCollectionExtensions.cs`.  
Change: introduce an `IDialectDirectiveHandler` registration model through DI, ordered deterministically, and make the binder consume the registered set rather than a static field. Preserve current behavior for the existing six handler types.  
Tests to add: a contract test proving deterministic handler order; an extension test showing a new handler can be added without editing the binder; a regression test preserving current diagnostics.  
Run: dialect test projects and any architecture guardrail tests touching dialect binding.  
Done when: the static registry is gone, existing behavior is preserved, and adding a new directive handler is possible by service registration only.  
Do not: break public APIs unless necessary, add placeholder handlers, bypass tests, or violate `PROJECT_RULES`.

**Prompt two — formalize bytecode tags and add a verifier**  
Goal: turn bytecode tags from raw convention into a checked contract.  
Problem: `BytecodeInstruction` stores `HashSet<string> Tags`, but there is no visible formal schema or verifier in the reviewed slice.  
Study: `BasicCore/TranslatorWrapper/BytecodeInstruction.cs`, `BasicCore/TranslatorWrapper/Bytecode.cs`, `BasicCodeTranslator/BasicAstToBytecodeTranslatorImpl.cs`, representative AST visitors in Arithmetic/Numbers/Variables/Conditions/Loops/Labels/Scopes.  
Change: introduce a typed tag wrapper or at least a centralized tag registry, then add a verifier that checks duplicate/unknown tags and optionally basic instruction-shape invariants.  
Tests to add: valid-bytecode passes, invalid/unknown-tag bytecode fails, representative feature visitors produce allowed tags only.  
Run: core tests, module tests, and any translator-state isolation tests.  
Done when: tags are centrally declared, validated, and documented.  
Do not: silently ignore invalid tags or weaken failing tests.

**Prompt three — remove backend leakage from the Wist facade**  
Goal: make the facade stop branching on concrete backend IDs and artifact types.  
Problem: `WistRuntimeFacade` currently maps interpreter to `IAbstractIR` and CIL to `DynamicMethod`, which will not scale to more backends.  
Study: `UniversalToolchain.Dialects.Wist/Facade/WistRuntimeFacade.cs`, `WistDialectBackendIds.cs`, `WistCilBackendDeclaration.cs`, `WistInterpreterBackendDeclaration.cs`, Wist execution configuration types.  
Change: design a backend-agnostic compiled-artifact interface returned by the host/compiler path so the facade can request execution by backend alias without knowing concrete artifact types.  
Tests to add: facade smoke tests for both current backends, regression tests for unknown backends, and a design guardrail test ensuring the facade no longer mentions `DynamicMethod` or `IAbstractIR`.  
Run: Wist facade and Wist runtime path tests.  
Done when: backend selection remains functional and the facade no longer hardcodes backend artifact classes.  
Do not: remove current backends, add fake adapters that bypass real behavior, or weaken public exceptions.

**Prompt four — upgrade dialect groups into real composition bundles**  
Goal: make groups useful for product-level language composition.  
Problem: `DialectGroupDescriptor` currently expands only modules and capabilities.  
Study: `DialectGroupDescriptor.cs`, `DialectGroupExpander.cs`, `DialectDefinition.cs`, `DialectBuildPlan.cs`, and existing Wist group providers.  
Change: extend group descriptors and expansion so they can also contribute optimizer directives, intrinsic directives, and optional backend defaults, while preserving deterministic conflict diagnostics.  
Tests to add: group-expansion tests for modules plus optimizers plus intrinsics; conflict tests; regression tests for existing groups.  
Run: dialect build-plan and explainability tests.  
Done when: a group can model a real “ArithmeticGroup”-style bundle instead of only module aliases.  
Do not: introduce special cases for shipped groups or let groups become a second hidden source of truth.

**Prompt five — ship an official module-authoring guide and template tests**  
Goal: make module creation safe for humans and AI.  
Problem: module authoring follows a real pattern, but the pattern is undocumented and enforced mostly by convention.  
Study: `ArithmeticModuleImpl.cs`, `NumbersModuleImpl.cs`, `VariablesModuleImpl.cs`, `ConditionsModuleImpl.cs`, `LoopsModuleImpl.cs`, `LabelsModuleImpl.cs`, `ScopesModuleImpl.cs`, and `PROJECT_RULES.md`.  
Change: add a Markdown authoring guide plus a minimal sample module and a cross-feature sample module; add tests that assert the sample modules register lexemes, parser node creators, and visitors correctly.  
Tests to add: template-instantiation tests and a guardrail test that module files keep the expected attribute/interface pattern.  
Run: module tests and dialect composition tests.  
Done when: a new contributor can follow one guide from zero to a working module without needing to infer hidden contracts.  
Do not: document fantasy APIs that do not exist or create placeholder sample code that is not compiled and tested.

**Prompt six — separate Wist convenience from framework truth**  
Goal: keep Wist builder/facade optional and thin.  
Problem: `WistRuntimeFacadeBuilder` creates its own hosted composition path, resolves shipped presets directly, and risks becoming the practical source of truth for framework usage.  
Study: `WistRuntimeFacadeBuilder.cs`, `WistDialectExecutionWorkflow.cs`, runtime manifest docs, and `AGENTS.md`.  
Change: introduce a generic runtime-builder path and make the Wist facade/builder wrap that path rather than owning separate composition choices. Keep shipped preset usage optional and explicit.  
Tests to add: parity tests showing generic-builder and Wist-builder produce equivalent hosts for the same dialect selection; guardrail tests preventing direct preset-specific branching in framework layers.  
Run: Wist runtime path tests and dialect runtime bootstrap tests.  
Done when: Wist convenience remains thin and replaceable.  
Do not: delete Wist convenience APIs outright or move Wist specifics into generic framework packages.

**Prompt seven — add public semantic-parity regression tests**  
Goal: prove the interpreter/reference-backend story instead of just implying it.  
Problem: the repository clearly has multiple backend concepts, but a public, obvious parity matrix is not yet the strongest visible story from the audited slice.  
Study: README backend examples, Wist backend declarations, existing dialect and Wist test suites, and runtime path guardrail tests.  
Change: add a focused parity test suite that runs the same expressions/programs across interpreter and CIL on shipped dialect presets, including arithmetic, variables, conditions, loops, and restricted dialect rejection cases.  
Tests to add: parity baseline tests for happy paths plus negative tests for expected restriction failures.  
Run: dialect tests and Wist tests.  
Done when: a reviewer can point to one obvious test suite proving “interpreter as oracle, CIL as execution backend.”  
Do not: skip failing cases, compare only one toy expression, or weaken restricted-dialect failures.
