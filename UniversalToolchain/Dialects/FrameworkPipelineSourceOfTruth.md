# Dialect DSL pipeline flow (incremental refactor)

## What changed

The framework-native dialect path now reuses lexer output from the UniversalToolchain pipeline instead of lexing source text a second time inside the dialect compiler stage:

1. `DialectDslCompiler` now wires `DialectCaptureLexer` as the lexer used by `BasicCore`.
2. `DialectCaptureLexer` stores the pipeline-produced token list in `DialectCompilationTokenContext`.
3. `DialectDefinitionSliceCompiler` first consumes these captured tokens and builds `DialectDefinitionSlice` from them.
4. A local lexer fallback is still kept only for direct `DialectDefinitionSliceCompiler` usage outside the full framework pipeline.

`DialectDslCompiler` clears token context before and after each compile call to avoid cross-run leakage.

## Why this improves architecture

- Removes normal second-pass re-lexing of raw source after the framework lexer has already run.
- Keeps the frontend/pipeline path as primary source of lexical truth.
- Preserves existing DSL behavior while reducing duplicate lexing logic.
- Keeps refactor incremental and compatible with existing tests and extension points.

## Current end-to-end flow

`text -> framework lexer (captured tokens) -> framework parser/AST -> slice parser -> build plan -> runtime composition`

## Deferred follow-up

- `UniversalToolchain.Dialects.Parsing` still exists for compatibility/diagnostics workflows; complete parser unification is intentionally deferred.
- `DialectDefinitionSliceCompiler` fallback lexing path remains for non-pipeline callers and targeted tests.
