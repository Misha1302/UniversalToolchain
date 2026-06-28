# Typed Module Contracts and Verifiers

Status: design proposal

Owner area: UniversalToolchain frontend/module architecture

Primary goal: make module behavior explicit, typed and verifiable without removing the modularity that makes Wist useful as a reference language.

## Summary

UniversalToolchain already has a strong modular compiler idea:

```text
source
-> lexer/parser
-> AST
-> bytecode
-> AIR
-> optimization/lowering
-> backend execution
```

The current weakness is not that modules cannot be composed. The weakness is that too much composition is protected by convention:

- token names and semantic tags can be copied as strings;
- parser priorities can be copied without proving grammar ownership;
- AST visitors are self-filtering and can silently emit for the wrong node;
- bytecode tags have producer/consumer meaning, but not a complete typed registry;
- stack effects and backend constraints are mostly inferred from tests and runtime behavior;
- optimizer legality depends on capability checks that are not yet uniformly enforced by a verifier.

The desired direction is to move from convention-only module composition to contract-backed module composition:

```text
module declares syntax, AST ownership, bytecode/AIR output and backend support
-> pipeline validates declarations
-> visitors/lowerers emit through typed contracts
-> verifiers reject invalid bytecode/AIR before backend execution
```

This document is not an implementation claim. It defines the target architecture, rollout path, guardrails and acceptance criteria for a future feature.

## Existing evidence and constraints

This proposal extends the current documentation instead of replacing it.

Relevant current documents:

| Document | Current role | Design implication |
|---|---|---|
| `docs/contracts/module-contracts.md` | Makes implicit module conventions visible. | Keep these contracts, but make them machine-checkable. |
| `docs/reference/module-contracts.md` | Public module-contract reference page. | Future public docs should expose stable parts of this design. |
| `docs/architecture/bytecode-and-air.md` | Defines bytecode as semantic merge layer and AIR as explicit semantic/execution representation. | Verifiers must preserve the AST -> Bytecode -> AIR boundary. |
| `docs/write-modules/semantic-tags.md` | Explains tag ownership and producer/consumer expectations. | Replace repeated string tags with typed descriptors and registries. |
| `docs/ARCHITECTURE_RULES.md` | Defines framework boundary, syntax ownership, interpreter policy and controlled reflection. | New contracts must not hardcode Wist, modules, backends or profile names in generic layers. |
| `docs/SYNTAX_OWNERSHIP_RULES.md` | Forbids raw-source syntax recognition outside owning syntax layers. | The contract model must make syntax ownership explicit. |
| `docs/CURRENT_ARCHITECTURE_STATUS.md` | Current supported runtime truth. | This design must not restore removed rules surfaces or interpreter-specific intrinsics. |

Current design constraints:

- UniversalToolchain is the framework; Wist is the reference language.
- Frontend modules must not depend on backend implementation.
- Runtime truth must flow through dialect definition, compiled dialect slice, build plan, selected runtime plan, runtime configuration and host/executor.
- Interpreter/CIL parity is mandatory for shared language behavior.
- Backend-specific intrinsics must be capability-gated and must not leak into the interpreter.
- Reflection is allowed only as bounded deterministic discovery over selected module/provider boundaries.

## Problem statement

The current module architecture is flexible, but it allows several failure modes to survive until integration tests or runtime execution.

### Failure mode 1: stringly typed semantic names

A token, AST node type or bytecode tag can become a cross-stage contract while still being represented as a plain string.

Examples of risk:

- a visitor checks a tag by raw string;
- an optimizer depends on a tag that is undocumented;
- two modules accidentally use the same tag for different meanings;
- a tag is produced but never consumed;
- a consumer exists but no selected module can produce the tag.

The compiler may still build, but language behavior changes.

### Failure mode 2: implicit visitor ownership

The AST-to-bytecode translator can give every configured visitor a chance to inspect a node. This is flexible, but it means every visitor must self-filter correctly.

Risks:

- two visitors emit output for the same AST node;
- no visitor emits output for a valid AST node;
- a visitor emits based on child count or loose token matching;
- intentional cooperation between visitors is not documented;
- stack shape changes without a declared stack effect.

### Failure mode 3: parser priority as hidden grammar contract

Parser creator priority is effectively part of grammar semantics. A priority value can decide ownership between overlapping syntax forms.

Risks:

- priority copied from another module without grammar analysis;
- new syntax steals nodes from an existing creator;
- priority changes pass simple examples but break nested syntax;
- dialect-specific module combinations produce different AST ownership.

### Failure mode 4: bytecode and AIR validation gap

Bytecode and AIR are semantic boundaries. They should be valid before later stages consume them.

Risks:

- unknown tags continue into optimization;
- backend-specific AIR reaches a backend that does not support it;
- interpreter receives optimized intrinsic forms that it must reject;
- stack effects are only discovered by backend execution;
- optimizer output is not checked against selected backend capabilities.

### Failure mode 5: AI-assisted module authoring is fragile

The repository has clear patterns, but a generated module can imitate the shape of a module without respecting hidden contracts.

Risks:

- plausible-looking code with wrong priorities;
- raw strings copied into unrelated layers;
- missing parity tests;
- visitor emits output for non-owned nodes;
- module works in one backend and fails in another.

## Target design principles

1. Contracts before behavior.
   A module declares what it owns and what it may emit before the pipeline executes it.

2. Typed names before string names.
   Cross-stage names use typed identifiers or descriptors. Raw strings remain only at external boundaries and serialization edges.

3. Ownership before visitor dispatch.
   A node must have an explicit owner or an explicit cooperation model. Accidental multi-emission is a defect.

4. Verification before lowering and execution.
   Bytecode and AIR are checked before optimizers and backends rely on them.

5. Capability-gated backend-specific behavior.
   Backend-specific output is legal only when selected backend capabilities support it.

6. Wist as reference, not framework truth.
   Wist modules may provide examples, but generic contracts must not hardcode Wist-specific names.

7. Incremental rollout.
   The first implementation should observe and report contract violations before enforcing every rule.

## Conceptual architecture

```text
Module descriptor
  -> declares syntax, AST ownership, tags, bytecode patterns, AIR patterns, backend support
Selected dialect/build plan
  -> selects module descriptors and backend capabilities
Pipeline registry
  -> builds deterministic ownership and contract tables
AST lowering
  -> dispatches through explicit ownership
Bytecode verifier
  -> checks tags, ownership, stack effects and emitted operations
AIR verifier
  -> checks instruction legality, intrinsic capability and backend support
Backend
  -> consumes verified semantic representation
```

## Core concepts

### Stable identifiers

Introduce typed identifiers for semantic names that cross file or layer boundaries.

Sketch:

```csharp
public readonly record struct ModuleId(string Value);
public readonly record struct AstNodeKind(string Value);
public readonly record struct BytecodeTagId(string Value);
public readonly record struct BytecodePatternId(string Value);
public readonly record struct AirPatternId(string Value);
public readonly record struct BackendCapabilityId(string Value);
```

Rules:

- identifiers must be non-empty;
- identifiers compare ordinally;
- framework-owned identifiers live in framework registries;
- module-owned identifiers live near the owning module or its descriptor;
- serialization may use strings, but runtime APIs use typed identifiers.

Rejected alternative: replace every string immediately.

Reason: too disruptive. The first migration should add typed constants and adapters while keeping old strings at compatibility boundaries.

### Known registries

Create central or feature-scoped registries for names that are already shared across stages.

Examples:

```csharp
public static class KnownAstNodeKinds
{
    public static AstNodeKind Variable { get; } = new("Variable");
    public static AstNodeKind Label { get; } = new("Label");
}

public static class KnownBytecodeTags
{
    public static BytecodeTagId ExpectingWriteTypeInference { get; } = new("ExpectingWriteTypeInference");
}
```

Rules:

- generic framework registries must not list Wist-only features unless they are framework concepts;
- Wist-specific registries belong to Wist or module projects;
- module-specific registries can be discovered through descriptors;
- public descriptors should expose ids, not raw strings.

## Module descriptor model

A module should declare a machine-checkable contract.

Sketch:

```csharp
public interface IFeatureModuleDescriptorProvider
{
    FeatureModuleDescriptor GetDescriptor();
}

public sealed record FeatureModuleDescriptor
{
    public required ModuleId ModuleId { get; init; }
    public required IReadOnlyList<LexemeContract> Lexemes { get; init; }
    public required IReadOnlyList<ParserNodeContract> ParserNodes { get; init; }
    public required IReadOnlyList<AstOwnershipContract> AstOwnership { get; init; }
    public required IReadOnlyList<BytecodeEmissionContract> BytecodeEmissions { get; init; }
    public required IReadOnlyList<AirEmissionContract> AirEmissions { get; init; }
    public required IReadOnlyList<ModuleDependencyContract> Dependencies { get; init; }
    public required IReadOnlyList<BackendSupportContract> BackendSupport { get; init; }
}
```

The descriptor is descriptive, not activating. Dialect selection still decides which modules are active.

### Lexeme contract

Purpose: make token names and lexical ownership visible.

Sketch:

```csharp
public sealed record LexemeContract(
    LexemeTypeId Id,
    string PatternDescription,
    SyntaxOwnershipScope OwnershipScope);
```

Rules:

- token names consumed by parser nodes or visitors must be declared;
- token names shared across files should use typed constants;
- lexer regex remains inside lexer/module syntax ownership.

### Parser node contract

Purpose: make parser priorities and node ownership visible.

Sketch:

```csharp
public sealed record ParserNodeContract(
    AstNodeKind Produces,
    ParserPriority Priority,
    IReadOnlyList<AstNodeKind> MayConsume,
    ParserOverlapPolicy OverlapPolicy);
```

Rules:

- overlapping syntax must declare overlap policy;
- priority conflicts should be reported by a parser contract validator;
- priority changes require parser regression tests.

### AST ownership contract

Purpose: make visitor dispatch deterministic.

Sketch:

```csharp
public sealed record AstOwnershipContract(
    AstNodeKind NodeKind,
    AstOwnershipMode Mode,
    ModuleId OwnerModule,
    IReadOnlyList<ModuleId> CooperatingModules);

public enum AstOwnershipMode
{
    Exclusive,
    Cooperative,
    ObserverOnly,
    ValidatorOnly
}
```

Rules:

- `Exclusive` means exactly one lowerer may emit semantic output;
- `Cooperative` requires an explicit ordered cooperation contract;
- `ObserverOnly` may inspect but not emit;
- `ValidatorOnly` may report diagnostics but not mutate or emit.

### Bytecode emission contract

Purpose: make bytecode output visible before AIR conversion.

Sketch:

```csharp
public sealed record BytecodeEmissionContract(
    AstNodeKind SourceNode,
    IReadOnlyList<BytecodeTagId> MayEmitTags,
    IReadOnlyList<BytecodePatternId> MayEmitPatterns,
    StackEffect DeclaredStackEffect,
    SideEffectPolicy SideEffects);
```

Rules:

- emitted tags must be declared;
- stack effects must be declared when statically knowable;
- side effects must be explicit;
- extension tags must use a namespaced extension policy.

### AIR emission contract

Purpose: make AIR legality and backend constraints visible.

Sketch:

```csharp
public sealed record AirEmissionContract(
    BytecodePatternId SourcePattern,
    IReadOnlyList<AirPatternId> MayEmitPatterns,
    IReadOnlyList<IntrinsicSymbol> MayEmitIntrinsics,
    BackendCapabilityRequirement CapabilityRequirement);
```

Rules:

- universal AIR may be emitted for all selected backends that support the shared contract;
- backend-specific intrinsics require selected backend capability support;
- interpreter-forbidden intrinsics must never reach interpreter-selected plans.

## Lowering API target

The current visitor interface can remain during migration, but the target model should separate lowerers, observers, validators and annotators.

### Lowerer

```csharp
public interface IAstNodeLowerer
{
    AstNodeKind NodeKind { get; }
    LoweringResult Lower(AstNode node, LoweringContext context);
}
```

Rules:

- lowerers emit bytecode or diagnostics;
- a lowerer must match a declared AST ownership contract;
- a lowerer cannot emit tags or patterns outside its module descriptor.

### Observer

```csharp
public interface IAstNodeObserver
{
    IReadOnlyList<AstNodeKind> ObservedNodeKinds { get; }
    void Observe(AstNode node, ObservationContext context);
}
```

Rules:

- observers cannot emit bytecode;
- observers cannot mutate AST unless they are explicitly declared as annotators;
- observers are for diagnostics, metrics or non-semantic metadata.

### Validator

```csharp
public interface IAstNodeValidator
{
    IReadOnlyList<AstNodeKind> ValidatedNodeKinds { get; }
    void Validate(AstNode node, ValidationContext context);
}
```

Rules:

- validators report diagnostics;
- validators must consume structured AST/declaration data;
- validators must not parse raw source.

### Compatibility adapter

Existing `IAstVisitor` implementations can be wrapped:

```text
legacy IAstVisitor
-> inferred compatibility descriptor
-> observe-mode validation
-> later explicit descriptor
```

The adapter should initially produce warnings, not hard failures, for legacy modules that do not yet declare full contracts.

## Pipeline registry

The pipeline should build a deterministic selected-module contract table.

Inputs:

- selected dialect/build plan;
- selected runtime plan;
- module descriptor providers;
- backend capability descriptors;
- optimizer descriptors;
- compatibility adapters for legacy visitors.

Outputs:

- AST ownership map;
- parser priority map;
- bytecode tag registry;
- bytecode pattern registry;
- AIR pattern registry;
- backend support map;
- validation diagnostics.

Important rule:

The registry explains and validates selected composition. It must not become a hidden source of runtime activation. Runtime truth still flows through the dialect/build-plan/runtime-selection chain.

## Bytecode verifier

The bytecode verifier checks bytecode before AIR conversion.

Inputs:

- bytecode;
- selected module contract table;
- source map when available;
- expected backend support when known;
- diagnostics sink.

Checks:

| Check | Failure example | Severity in first rollout | Final severity |
|---|---|---|---|
| Unknown tag | Tag not declared by any selected module. | Warning | Error |
| Undeclared producer | Module emits tag it did not declare. | Warning | Error |
| Missing consumer | Safety-relevant tag has no consumer. | Warning | Warning/Error by policy |
| Conflicting tags | Instruction has mutually exclusive semantic tags. | Warning | Error |
| Unknown bytecode pattern | Operation shape not declared. | Warning | Error |
| Stack effect mismatch | Declared stack effect does not match produced op shape. | Warning | Error |
| Backend-specific metadata leak | Backend-only tag appears in backend-neutral phase. | Error | Error |
| Non-deterministic layer conflict | Same layer priority has conflicting operations. | Warning | Error |

Output:

```csharp
public sealed record BytecodeVerificationResult(
    bool IsValid,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);
```

## AIR verifier

The AIR verifier checks AIR after bytecode conversion and after each optimizer that can change AIR shape.

Inputs:

- AIR;
- selected backend or backend set;
- selected optimizer set;
- intrinsic catalog;
- backend capability catalog;
- interpreter policy.

Checks:

| Check | Failure example | Severity |
|---|---|---|
| Unknown opcode | AIR contains opcode not recognized by current AIR version. | Error |
| Invalid operands | Instruction operands do not match opcode schema. | Error |
| Stack underflow | Instruction consumes more values than available. | Error |
| Invalid branch target | Jump target label is missing or duplicate. | Error |
| Unsupported intrinsic | Backend does not support emitted intrinsic. | Error |
| Interpreter intrinsic leak | Interpreter-selected path receives `load_*`, `add_*`, local or backend-specific intrinsic. | Error |
| Optimizer legality gap | Optimizer produced shape without declaring capability requirement. | Error |
| Side-effect reorder risk | Optimizer reordered instructions without purity evidence. | Error |

The AIR verifier should be runnable:

- after initial AIR conversion;
- after each optimizer in debug/contract mode;
- before backend execution in release-gate mode.

## Diagnostics model

Diagnostics should be designed for module authors, not only framework maintainers.

Example:

```text
UT-MOD-OWNERSHIP-002
AST node 'Variable' is emitted by 'VariablesVisitor', but module 'Variables'
does not declare exclusive ownership for node kind 'Variable'.
Declare AstOwnershipContract or mark the visitor as ObserverOnly.
```

Example:

```text
UT-BYTECODE-TAG-004
Bytecode tag 'ExpectingWriteTypeInference' is consumed by VariablesVisitor,
but no selected module declares it as a produced tag.
```

Example:

```text
UT-AIR-BACKEND-007
Optimizer 'NativeCilOptimizer' produced intrinsic 'add_i32' for backend
'interpreter'. The interpreter policy allows only core AIR opcodes, 'call C#',
and 'call C# ctor'.
```

Diagnostics should include:

- code;
- severity;
- owning layer;
- module id when known;
- source span when available;
- selected dialect/build plan context when relevant;
- actionable remediation text.

## Rollout plan

### Phase 0: documentation and inventory

Goal: make the design visible and identify existing implicit contracts.

Deliverables:

- this design document;
- inventory of current token names, AST node kinds and bytecode tags;
- list of modules with explicit/implicit contracts;
- list of known legacy visitors and cooperation patterns.

Acceptance criteria:

- every current module is classified as `explicit`, `implicit`, or `legacy-compatible`;
- no runtime behavior changes.

### Phase 1: typed identifiers and compatibility constants

Goal: reduce raw string spread without changing semantics.

Deliverables:

- typed id value objects;
- `KnownAstNodeKinds` and `KnownBytecodeTags` for existing shared names;
- compatibility conversion helpers from/to existing string names;
- guardrail tests that reject new repeated cross-layer raw strings where practical.

Acceptance criteria:

- existing tests still pass;
- new code uses typed identifiers for shared cross-stage names;
- old strings remain supported at boundaries.

### Phase 2: module descriptor providers

Goal: let modules declare their contract.

Deliverables:

- `IFeatureModuleDescriptorProvider`;
- descriptor records;
- descriptor validation;
- initial descriptors for 2-3 representative modules:
  - a simple module, for example Numbers;
  - a stateful module, for example Variables;
  - a control-flow module, for example Labels or Loops.

Acceptance criteria:

- descriptor validation runs in tests;
- selected modules can produce a deterministic contract table;
- missing descriptors produce warnings for legacy modules.

### Phase 3: ownership-aware lowering

Goal: separate emitters from observers and validators.

Deliverables:

- `IAstNodeLowerer`;
- `IAstNodeObserver`;
- `IAstNodeValidator`;
- compatibility adapter for current `IAstVisitor`;
- ownership registry;
- diagnostics for zero-owner and multi-owner cases in observe mode.

Acceptance criteria:

- existing visitors can still run through compatibility mode;
- explicit lowerers are checked against declared ownership;
- accidental multi-owner cases become visible.

### Phase 4: bytecode verifier

Goal: validate bytecode before AIR conversion.

Deliverables:

- `IBytecodeVerifier`;
- bytecode tag and pattern registries;
- producer/consumer checks;
- stack-effect checks for simple operations;
- diagnostics with module and source context where possible.

Acceptance criteria:

- verifier catches unknown tags in tests;
- verifier catches undeclared emitted patterns;
- verifier can run in warning mode for existing legacy modules;
- verifier is part of focused module tests.

### Phase 5: AIR verifier and optimizer legality checks

Goal: prevent backend-specific or optimizer-specific output from leaking into unsupported paths.

Deliverables:

- `IAirVerifier`;
- opcode schema checks;
- intrinsic support checks;
- interpreter policy checks;
- optimizer output validation hook;
- tests for CIL-supported and interpreter-forbidden optimized forms.

Acceptance criteria:

- interpreter-selected plans reject backend-specific intrinsics before execution;
- optimizer legality is checked against selected backend capabilities;
- parity tests can require verifier success for both interpreter and compiler paths.

### Phase 6: enforcement and public module authoring path

Goal: move from warnings to enforced contracts for new modules.

Deliverables:

- module author checklist;
- updated `docs/write-modules/*`;
- template/sample module with descriptors and tests;
- CI guardrails for new modules;
- compatibility policy for old modules.

Acceptance criteria:

- new modules require descriptors;
- old modules have explicit migration status;
- docs explain how to add syntax, ownership, lowering, verification and parity tests.

## Test strategy

### Unit tests

Add focused tests for:

- typed identifier equality and validation;
- descriptor validation;
- duplicate module ids;
- duplicate AST ownership;
- invalid parser priority overlap;
- undeclared tag emission;
- unknown bytecode tag;
- stack-effect mismatch;
- unsupported backend intrinsic.

### Architecture guardrail tests

Add guardrails for:

- no Wist-specific ids in generic framework registries;
- no concrete backend names in generic frontend layers;
- no raw-source syntax recognition outside syntax owners;
- no new cross-layer raw string tags without constants/descriptors;
- deterministic descriptor ordering.

### Module contract tests

Each migrated module should have tests for:

- selected dialect includes the module and syntax works;
- restricted dialect excludes the module and syntax is rejected;
- parser ownership for positive and negative examples;
- lowerer emits only declared tags/patterns;
- bytecode verifier passes;
- AIR verifier passes for supported backends;
- interpreter/compiler parity for shared behavior.

### Optimizer tests

Optimizers that rewrite AIR must test:

- supported backend path;
- unsupported backend path;
- no semantic change;
- no side-effect reorder without purity evidence;
- verifier success after optimization.

## Example: Variables module target contract

The Variables module is a good migration candidate because it touches syntax, binding, bytecode, runtime calls and backend parity.

Target declaration sketch:

```csharp
public sealed class VariablesModuleDescriptorProvider : IFeatureModuleDescriptorProvider
{
    public FeatureModuleDescriptor GetDescriptor() =>
        FeatureModuleDescriptorBuilder
            .Create(KnownModules.Variables)
            .OwnsAstNode(KnownAstNodeKinds.Variable, AstOwnershipMode.Exclusive)
            .OwnsAstNode(KnownAstNodeKinds.VariableDefinition, AstOwnershipMode.Exclusive)
            .MayEmitTag(KnownBytecodeTags.ExpectWriteTypeInference)
            .MayEmitPattern(KnownBytecodePatterns.LocalRead)
            .MayEmitPattern(KnownBytecodePatterns.LocalWrite)
            .MayEmitPattern(KnownBytecodePatterns.ExternalRead)
            .MayEmitPattern(KnownBytecodePatterns.ExternalWrite)
            .MayEmitAirPattern(KnownAirPatterns.RuntimeCall)
            .RequiresBackendCapability(KnownBackendCapabilities.UniversalCall)
            .Build();
}
```

Expected verifier behavior:

- a local-variable read must declare a fixed storage type before read;
- local-variable lowering for interpreter must become universal C# runtime calls;
- optimized local intrinsics may only appear for backends that declare support;
- external constants must reject assignment before backend execution;
- variable type inference tags must have documented producer and consumer.

## Example: Labels module target contract

The Labels module is a good control-flow candidate.

Target declaration sketch:

```csharp
public sealed class LabelsModuleDescriptorProvider : IFeatureModuleDescriptorProvider
{
    public FeatureModuleDescriptor GetDescriptor() =>
        FeatureModuleDescriptorBuilder
            .Create(KnownModules.Labels)
            .OwnsAstNode(KnownAstNodeKinds.Label, AstOwnershipMode.Exclusive)
            .OwnsAstNode(KnownAstNodeKinds.Goto, AstOwnershipMode.Exclusive)
            .MayEmitAirPattern(KnownAirPatterns.Label)
            .MayEmitAirPattern(KnownAirPatterns.Jump)
            .RequiresVerifierRule(KnownVerifierRules.UniqueLabels)
            .Build();
}
```

Expected verifier behavior:

- duplicate labels are rejected deterministically;
- jumps to missing labels are rejected;
- label/goto shared state is scoped to one compilation;
- compiler and interpreter resolve labels consistently.

## Compatibility policy

Existing modules should not be broken immediately.

Suggested compatibility levels:

| Level | Meaning | CI behavior |
|---|---|---|
| `LegacyImplicit` | Module has no descriptor. | Allowed with warning. |
| `PartiallyDeclared` | Module declares ownership but not full bytecode/AIR effects. | Allowed with focused warnings. |
| `Declared` | Module declares syntax, ownership and emissions. | Required for new modules. |
| `Verified` | Module passes bytecode/AIR verifier and parity tests. | Target state. |
| `Enforced` | Violations are build/test failures. | Final state for mature modules. |

New modules should start at `Declared` and reach `Verified` before being considered complete.

## Rejected alternatives

### Alternative 1: keep visitors self-filtering forever

Rejected because it preserves the main hidden-contract problem. It relies on every module author and AI agent remembering invisible rules.

### Alternative 2: replace the whole translator immediately

Rejected because it is too risky. The existing pipeline works and contains useful compatibility behavior. The correct path is descriptor-first, verifier-first and then lowering API migration.

### Alternative 3: make bytecode a classic opcode list

Rejected because bytecode currently acts as a semantic merge layer. Removing that design would reduce the flexibility that makes modular feature composition possible.

### Alternative 4: solve everything with tests only

Rejected because tests are necessary but not enough. The framework needs runtime-visible contracts and diagnostics so invalid compositions fail close to the source of the problem.

### Alternative 5: make capabilities activate behavior

Rejected because project rules say capabilities are projection and explanation layers. Dialect/build-plan/runtime selection remains the activation source of truth.

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Too much API surface too early. | Framework becomes harder to understand. | Start with descriptors and verifier warnings before public API promises. |
| False positives from verifier. | Existing modules become noisy. | Use compatibility levels and warning mode first. |
| Descriptor drift from implementation. | Contracts become decorative. | Descriptor validation must compare declared and observed emissions in tests. |
| Wist-specific constants leak into BasicCore. | Framework universality erodes. | Keep generic ids in framework, Wist ids in Wist/module projects. |
| Optimizer checks slow tests. | CI becomes heavy. | Run full verifier in focused tests and release-gate jobs; use lighter checks by default. |
| AI-generated modules game the descriptor. | Plausible but invalid code passes shallow review. | Require verifier success, parity tests and negative dialect tests. |

## Definition of done

This feature should not be considered complete until:

- shared semantic names have typed identifiers or documented descriptor ownership;
- new modules declare AST ownership and emitted bytecode/AIR patterns;
- verifier diagnostics include actionable module/layer context;
- bytecode verifier catches unknown/undeclared tags;
- AIR verifier catches unsupported backend-specific intrinsics;
- interpreter policy is enforced before execution;
- representative modules are migrated;
- docs and module-authoring guides are updated;
- CI includes focused guardrails for descriptors, ownership and verifier behavior.

## Smallest useful first PR

The first PR should not rewrite the pipeline.

Recommended first PR:

1. Add this design document.
2. Add typed wrappers for `AstNodeKind` and `BytecodeTagId`.
3. Add `KnownAstNodeKinds` and `KnownBytecodeTags` for 3-5 existing shared names.
4. Add a descriptor skeleton with no runtime activation behavior.
5. Add one guardrail test proving that a descriptor can declare ownership and tags.
6. Migrate one small module's constants without changing behavior.

Expected result:

- no behavior change;
- no backend change;
- no dialect activation change;
- clearer direction and first machine-checkable contract.

## Long-term target

The long-term target is that a new module author follows a stable path:

```text
declare module descriptor
-> declare syntax ownership
-> declare AST ownership
-> implement lowerers/validators/observers
-> declare bytecode and AIR emissions
-> run verifier tests
-> run interpreter/compiler parity tests
-> add dialect inclusion/exclusion tests
```

At that point, UniversalToolchain becomes safer for humans and AI agents:

- new modules are harder to add incorrectly;
- invalid backend combinations fail early;
- restricted dialects become more trustworthy;
- public module authoring becomes easier to document;
- Wist remains a reference language instead of a hidden source of framework truth.
