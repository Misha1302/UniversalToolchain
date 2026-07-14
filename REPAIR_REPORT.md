# Wist2 abstraction and runtime correctness repair report

Validation date: 2026-07-14.

## Scope

This archive contains two consecutive implementation-and-review cycles over the supplied
`Wist2-0.1.0-alpha.1-fixed-20260713` source tree. The work targets confirmed lifecycle,
configuration, AIR, verifier, managed-call, capability-composition and runtime-activation defects.
The second cycle deliberately reviewed the first implementation adversarially and corrected gaps
that ordinary green tests did not reveal.

This report does not claim stable 1.0 compatibility, hostile-code isolation, an SSA-native backend,
or production performance evidence.

## Corrected defects

### 1. Frontend configuration lifecycle

Parser and lexer configuration modules no longer retain a one-time `_isInitialized` flag. A
singleton frontend module applies its configuration to every fresh parser or lexer, and a failed
load does not poison later retries.

### 2. Transactional lexer configuration

Lexer configuration is parsed and validated completely before publication. Invalid field counts,
non-finite priorities, malformed Base64/UTF-8, invalid regular expressions and duplicate ownership
leave the existing live configuration untouched.

The second cycle aligned loader validation with the live `LexerConfiguration` invariant: a regex
cannot belong to two lexeme types and a lexeme type cannot own two regexes. These checks run before
creating extensible enum values or replacing live state.

### 3. Prepared-program lifecycle

`BasicCoreImpl.PrepareToRun` invalidates the previous prepared artifact before starting a new build.
After failed preparation, `RunPrepared` cannot execute stale code.

### 4. Immutable and typed AIR constants

AIR instructions snapshot operands and metadata into read-only collections. Null constants carry an
explicit declared type through `AirConstant`; raw untyped null is rejected. Interpreter, CIL type
simulation, CIL emission, AIR analysis, optimizers and SSA lowering use the shared `AirPushOperand`
contract.

CIL emits reference null through `ldnull` and nullable-value null through a zero-initialized local.

### 5. Declared AIR types survive interpreter execution

The interpreter now retains each stack value's declared AIR type independently from its runtime
value. Generic calls therefore resolve a typed null such as `string?` as `string`, not `object`.

The pre-existing public API `InterpreterState.ValueStack : Stack<object?>` remains available. Type
metadata is maintained internally and is reconstructed best-effort if external code mutates the
public stack, avoiding a source/API break introduced during the intermediate implementation.

### 6. Complete terminal-path and stack verification

A conditional jump in the final instruction now has an explicit synthetic fallthrough exit in the
CFG. The edge is visible to reachability, stack analysis, structural verification and SSA consumers.

Terminal stack shape is owned by `AirStackAnalyzer`: every reachable terminal block must expose zero
or one value, and all terminal return shapes must agree. The module-contract verifier keeps its
corresponding runtime-type check. This closes the previous gap where one verifier rejected malformed
terminal stacks while the public structural verifier accepted them.

### 7. Mandatory intrinsic stack semantics

Capability presence no longer substitutes for stack semantics. Every intrinsic accepted by generic
AIR verification must be readable as a canonical invocation and resolvable through an intrinsic
semantic descriptor. Unknown extension intrinsics without semantics are rejected.

### 8. One neutral managed-call contract across all layers

`IManagedCallDescriptor` is now consumed consistently by AIR analysis, intrinsic type processing,
SSA lowering, CIL simulation, CIL emission, interpreter execution and native-CIL optimization. No
layer discovers call semantics through reflection over property names or nested CLR type names.

The public `BasicCore.Core.CSharpCallDescriptor` remains in its original assembly and implements the
neutral interface, preserving ownership and compatibility.

### 9. Execution-scoped provider calls support arguments

Both interpreter and CIL backends now support execution-scoped provider methods with parameters. The
CIL backend spills already-cast arguments to locals, loads and casts the provider, then restores
arguments in call order. Backend parity is covered by a shared regression scenario.

### 10. Deterministic capability ownership

A duplicate `LanguageFeatureId` is a composition error (`UTC-CAP-002`) naming both providers. The
duplicate is not inserted, so projections and owner lookup cannot observe contradictory entries.

### 11. One composition-scoped capability catalog

`FunctionCallsModuleImpl` consumes the catalog built by the selected runtime composition. It no
longer performs a second frontend-only reflection discovery. Selected modules, optimizers and
backend metadata share one source of truth.

### 12. Lazy backend activation without cross-container leakage

`ToolchainRuntimeHost` activates only the selected backend. Runtime factory results are validated
before publication and duplicate backend IDs are rejected.

Caching is scoped to the resolving `IServiceProvider` through a weak-key table. Reusing a registration
object across two DI containers no longer returns a core constructed with dependencies from the
first container.

## Regression coverage added or strengthened

The combined cycles cover:

- repeated parser/lexer configuration over fresh frontend instances;
- retry after failed configuration and atomic preservation of live lexer state;
- regex/type ownership collisions during lexer snapshot loading;
- stale prepared-artifact invalidation;
- reference and nullable typed-null parity between interpreter and CIL;
- generic resolution using the declared type of a null value;
- immutable AIR instruction snapshots;
- terminal stack cardinality and incompatible return shapes;
- explicit final conditional fallthrough and its verifier-visible terminal state;
- extension intrinsic rejection without semantic descriptors;
- neutral managed-call descriptors in both backends;
- execution-scoped provider calls with arguments;
- preservation of the public `InterpreterState.ValueStack` contract;
- duplicate language-feature ownership;
- lazy backend activation, validate-before-publish and per-container caching.

## Final verification summary

- Core test assembly: **452 passed**.
- Module test assembly: **288 passed**.
- Dialect/runtime/facade test assembly: **585 passed**.
- Total: **1,325 passed, 0 failed, 0 skipped**.
- Solution Debug build: **PASS — 74/74 solution projects, 0 warnings, 0 errors**. The standalone benchmark project also builds cleanly, so all **75/75 repository `.csproj` projects** compile with 0 warnings and 0 errors.

Exact environment, commands and evidence boundaries are recorded in `VERIFICATION.md`.
