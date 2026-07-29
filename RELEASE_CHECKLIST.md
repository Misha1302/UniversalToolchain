# Abstraction-fix second-cycle release checklist

Validation date: 2026-07-14.

## Source and behavior

- [x] input source archive SHA-256 and safe ZIP structure verified before editing
- [x] frontend configuration modules remain instance-stateless
- [x] lexer configuration replacement is strict and transactional
- [x] regex and lexeme-type ownership collisions are rejected before publication
- [x] failed preparation invalidates the previous artifact
- [x] AIR instruction payloads remain immutable snapshots
- [x] null constants are explicitly typed and backend-parity tested
- [x] interpreter preserves declared AIR type for typed null
- [x] public `InterpreterState.ValueStack : Stack<object?>` contract is preserved
- [x] final conditional fallthrough is explicit in the CFG
- [x] terminal stack shape is checked by shared AIR stack analysis
- [x] extension intrinsics require semantic stack descriptors
- [x] all managed-call consumers use the neutral descriptor contract
- [x] execution-scoped provider calls with arguments have backend parity
- [x] duplicate language-feature IDs fail composition
- [x] FunctionCalls consumes one composition-scoped capability catalog
- [x] only the requested backend runtime is activated
- [x] invalid runtime factory results are validated before publication
- [x] runtime cache is isolated per service provider / DI container

## Build and tests

- [x] full Debug solution build passed: 74/74 solution projects
- [x] standalone benchmark project built; all 75/75 repository `.csproj` projects compile
- [x] compiler warnings: 0
- [x] compiler errors: 0
- [x] core suite passed: 452/452
- [x] module suite passed: 288/288
- [x] dialect/runtime/facade suite passed: 585/585
- [x] total passed: 1,325; failed: 0; skipped: 0
- [x] all 21 runtime manifests generated through the repository emitter

## Artifact

- [x] stale `CHANGELOG.md` excluded
- [x] `bin`, `obj`, Git metadata, caches and logs removed
- [x] secret-like and unsafe archive paths rejected
- [x] detached release-integrity manifest regenerated after packaging
- [x] clean-unpack build and detached package-integrity verification passed
- [x] final archive SHA-256 generated and rechecked

## Not revalidated in this cycle

- NuGet package and symbols surface;
- external consumer restore/publish smoke;
- VitePress documentation build;
- benchmark-performance claims;
- hostile-code or production workload isolation.
