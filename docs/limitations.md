# Current Limitations

This document records known limitations so the project can be presented honestly and improved deliberately.

A limitation is not a failure. It is a boundary that must not be hidden behind marketing language or convenience APIs.

## Wist-first framework maturity

UniversalToolchain is the framework, and Wist is the reference language.

Current limitation:

- The strongest runnable path is still Wist-first.
- The generic third-party DSL authoring surface is not yet as polished as the Wist convenience path.
- Some examples and facades naturally point users toward Wist-specific APIs.

Expected direction:

- Keep Wist as a proving ground.
- Make generic framework APIs clearer.
- Prevent Wist convenience layers from becoming framework truth.

## Dialect DSL extensibility

Current limitation:

- Runtime composition is more modular than the dialect DSL directive layer.
- Some directive handling may still be implemented through concrete registries or framework-owned handlers.

Expected direction:

- Make dialect directive handling composable through explicit registration.
- Preserve deterministic ordering and diagnostics.
- Allow new directive families without editing central framework code when possible.

## Backend abstraction

Current limitation:

- Current Wist-facing execution paths may know about concrete backend artifact shapes.
- Interpreter and CIL are supported concepts, but backend-agnostic artifact handling is not fully generalized.
- Adding a third serious backend may require Wist-facing changes.

Expected direction:

- Introduce backend-agnostic compiled/executable artifact contracts.
- Keep backend selection in the selected runtime plan.
- Keep facades thin and free from concrete backend artifact branching.

## Callable-first SSA pre-release boundary

Current limitation:

- SSA is a pre-release intermediate layer, not the default production route.
- Arithmetic and managed calls are represented as ordinary callables with
  semantic descriptors, but the bridge still covers a deliberately small
  subset.
- The no-optimization `AIR -> SSA -> AIR` route is available behind explicit
  route policies (`Off`, `Prefer`, `Require`, `Debug`), but dialect syntax for
  those policies is not final.
- Managed callable descriptors support bool, int32, float64 and managed object
  references. Unsupported CLR value types, unresolved generic methods and
  execution-scoped provider descriptors are rejected rather than guessed.
- `SSA -> AIR` emission still requires AIR-compatible stack shape for the
  supported subset. It is not yet a general SSA scheduler.
- Callable lowering can select AIR intrinsic and managed-call targets for the
  current AIR route. CIL opcode and interpreter-primitive targets are explicit
  diagnostics until those routes are implemented.
- Constant folding is descriptor-driven and trust-gated, but only local
  constant folding is implemented.

Expected direction:

- Keep SSA structural and callable-first.
- Move more runtime/library operations into descriptor packages instead of SSA
  core opcodes.
- Add backend capability planning before lowering callables to AIR intrinsics,
  CIL opcodes, managed calls or interpreter primitives.
- Add differential interpreter/CIL tests before making broad performance or
  parity claims for the SSA route.

## Bytecode tags and verifier coverage

Current limitation:

- Bytecode tags are powerful but can behave like undocumented string contracts.
- A full tag taxonomy, producer/consumer matrix, and verifier layer should be strengthened.

Expected direction:

- Centralize tag declarations.
- Add bytecode validation.
- Document stack effects, safety metadata, and backend-lowering requirements where possible.

## Module authoring safety

Current limitation:

- Module authoring follows a recognizable pattern, but many contracts are easy to miss.
- Token names, parser priorities, visitor ownership, shared state, and backend capability requirements can be fragile.

Expected direction:

- Treat `docs/guides/module-authoring.md` and `docs/contracts/module-contracts.md` as mandatory guidance.
- Add sample modules and template tests.
- Add architecture guardrail tests for new extension surfaces.

## AI-generated changes

Current limitation:

- AI agents can imitate nearby module code while missing hidden contracts.
- This is risky for parser priority, visitor ownership, bytecode tags, shared state, and backend-specific intrinsics.

Expected direction:

- Use explicit prompts and architecture docs.
- Require tests for generated modules.
- Prefer machine-checkable contracts over convention-only rules.

## Performance claims

Current limitation:

- The architecture is designed to support optimized compiled execution.
- Near-C# performance should not be claimed publicly without current, reproducible benchmark evidence.
- Compile time, execution time, interpreter time, and CIL execution time must be measured separately.

Expected direction:

- Maintain reproducible benchmarks.
- Document benchmark methodology.
- Keep interpreter correctness and CIL performance as separate claims.

## Sandboxing and security

Current limitation:

- Restricted dialect composition is not the same as hardened sandboxing.
- Reflection and interop require careful trust boundaries.
- Untrusted execution needs process/environment isolation.

Expected direction:

- Keep `docs/SECURITY.md` authoritative for trust boundaries.
- Keep interop restrictions explicit.
- Do not market restricted dialects as a complete security sandbox.

## Documentation completeness

Current limitation:

- The repository has strong architecture rules and runtime pipeline docs, but some contributor-facing contracts are still being formalized.

Expected direction:

- Keep README concise.
- Move deep contracts into focused docs.
- Update docs in the same change as architectural behavior changes.

## Public wording to avoid

Avoid these claims unless they are backed by implementation and tests:

- fully universal language workbench;
- production-grade sandbox;
- arbitrary backend support with no framework changes;
- near-C# speed in general;
- AI-safe module generation by convention alone;
- all bytecode tags are formally verified.

## Public wording to prefer

Prefer this wording:

> UniversalToolchain is a Wist-first modular .NET DSL/runtime framework prototype with manifest-backed runtime selection, explicit architecture guardrails, and a reference language used to validate the framework.

Prefer this limitation statement:

> Generic dialect-DSL extension, backend-agnostic compiled artifacts, bytecode tag verification, and outsider-friendly module authoring are active design areas rather than finished product promises.
