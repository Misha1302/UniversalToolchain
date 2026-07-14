# Wist2 architecture hardening v8

Date: 2026-07-12

## Scope

This pass addresses the concrete boundary, lifecycle, stack-safety, namespace,
and stale-configuration defects found after the final legacy-cleanup archive was
reviewed. It is a hardening pass, not a claim that the package graph or generic
runtime surface is ready for stable 1.0.

## Corrected boundaries

### Frontend and Core composition

The generic dialect frontend no longer locates a Core extension method through
assembly, type, and method-name strings. Neutral intrinsic registration lives in
`BasicCore`; built-in frontend defaults live in
`UniversalToolchain.Dialects.Frontend`; the Core convenience bootstrap composes
those APIs directly.

`BasicCore` no longer references `SettableGettableModule`. The local-reference
stack rule represents the neutral stack contract as a CLR by-ref type and leaves
feature implementation ownership in the feature layer.

### Runtime ownership and process-wide hooks

`ToolchainRuntimeHost` now receives an explicit `ServiceProviderOwnership` value.
The constructor used with a caller-supplied provider is borrowed by default, while
the Wist workflow marks its internally built provider as owned.

`DefaultRuntimeAssemblyLoadStrategy` implements a symmetric disposable lifecycle.
Its `AssemblyLoadContext.Default.Resolving` handler is registered lazily and is
removed exactly once on disposal.

### AIR and interpreter safety

`AirVerifier` now runs CFG stack-state verification after instruction schema and
branch-target validation. It reports underflow, non-boolean branch conditions,
and incompatible merge states before backend execution.

The reference interpreter no longer treats empty `Drop`, `JmpIf`, or `JmpIfNot`
as no-ops. Missing operands and stack values produce deterministic runtime
errors with opcode and program-counter context.

### Contract namespaces

`ContractNamespaceOwner` is extensible and supports package-defined reservations.
Descriptor providers expose their namespace reservations, and
`ModuleContractSelectionBuilder` passes them into the production table builder.
The table validates module IDs and module-owned AST, bytecode, compiler-fact,
pipeline-effect, verifier, and backend-capability IDs.

Backend providers may declare more than one namespace: for example, the module ID
uses `backend.*`, while backend-owned capability IDs may use `cil.*` or
`interpreter.*`.

### Intrinsic compatibility identity

Legacy display/capability names are centralized in `IntrinsicCapabilityIds`.
Interpreter, AIR helpers, type processing, CIL registration, AIR analysis, and
SSA emission consume this catalog rather than maintaining parallel literals.
Typed `IntrinsicSymbol` remains the semantic identity; the catalog is the single
compatibility-codec boundary.

### Parser-order configuration

Configuration loading no longer scans loaded assemblies or calls `GetTypes()`.
Entries are resolved only against creators already registered in the selected
parser. Parsing uses invariant culture, validates the complete file before
mutation, rejects stale or duplicate entries, preserves unspecified creators,
and writes through an atomic temporary file.

The remaining limitation is explicit: the file still uses a registered CLR type
and instance index because `IAstNodeCreator` has no stable semantic creator ID.
Unlike the previous implementation, drift fails closed and never selects the
first available creator.

### Dependency declarations and removed aliases

`BasicCodeTranslator` directly references `BasicCore` instead of obtaining its
contracts through `BasicParser`. `BasicInterpreter` directly references the
projects whose public types it uses and no longer depends on the translator or
CIL compiler merely to obtain transitive assemblies.

The obsolete `WistThrower`, `WistIdentifierFacts`, `WistScopesFacts`, variable-tag
alias, old type-catalog factory, and generic artifact-compiler compatibility
method are removed.

## Verification policy

The canonical evidence is recorded in the root `VERIFICATION.md`. Targeted
regression tests are committed beside the affected subsystems. A small external
smoke harness was used only as an early proof of the changed contracts and is not
part of the release archive.

## Deliberately not solved here

- consolidation of the 75-project package graph;
- a semantic parser-creator ID contract and migration format;
- generic stack-effect descriptors for arbitrary third-party intrinsics;
- stable 1.0 compatibility, hostile-code isolation, or production workload proof.
