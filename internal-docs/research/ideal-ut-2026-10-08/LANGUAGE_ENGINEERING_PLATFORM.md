# Language Engineering Platform — distinct product track, same owner model

**PROPOSED PRODUCT PLAN; nothing here is a shipped UT workbench.** Preserves the first ZIP's separate second strategic goal, `05_LANGUAGE_TOOLING_PRODUCT.md`. Success of this track does **not** imply success of the separate cross-representation semantic research hypothesis.

## Target experience: two user journeys

**External language author:** create an embedded `.NET` Rules/Policy DSL from a small parser/model, bind host symbols and custom types, register typed UT package, select one interpreter backend through `LanguageCompiler`, validate structured source spans, run in an independent clean consumer, attach minimal editor language service, and debug a rule. The second test language must be semantically different (e.g. tensor/stream or typed configuration) so generic infrastructure cannot quietly specialize to Wist.

**Application end user:** edit a versioned policy in VS Code or a browser editor; receive syntax, name/type and policy errors before any execution; preview against a frozen document and exact selected `LanguagePlan`; review a semantic diff; explicitly approve; store signed/reproducible rule version; run with constrained host inputs; audit effective policy/engine/input-schema versions and roll back to prior approved artifact. UT itself does **not** automatically supply host authorization, secure sandboxing or production policy rollout.

## Architecture boundaries

```text
Text (canonical source) → parser/lossless spans/trivia → binder/types/symbols
                                  |                        |
                          document snapshot       reusable semantic queries
                                  |                        |
                    LanguageCompiler/Plan (one config owner)
                                  |
                      compiler/runtime executor
                                  |
   read-only/validated projections to diagnostics, LSP, DAP, CLI, web UI, AI
```

**Compiler-grade** data may justify legal transformations only with trusted, exact evidence. **IDE-grade** data must tolerate incomplete ASTs/holes and return Unknown. **Speculative/agent-derived** data are hints only. A response computed for document revision `r` cannot overwrite diagnostics for revision `r+1`. Workspace indexing may use independent incremental storage later; the batch compiler need not acquire a permanent query database.

## Minimum LSP PoC (not a language server product)

Implement open/change, diagnostics, completion, hover, definition; preserve document revisions, cancellation, source spans, stable bound symbol identity and deterministic errors. Second-wave references/rename, semantic tokens, formatting, code actions, signature help, workspace symbols, inlay hints. Prefer an existing LSP transport to rewriting the protocol. Test valid→invalid→valid edit sequence, stale asynchronous response suppression, and that hover/definition see the **same binder semantics** as batch compilation.

## Distinct debugging surfaces

1. **End-user DSL debugger:** operation boundaries, read/write events, call frames, source breakpoints, step/continue, exception surfaces. DAP follows interpreter hooks and stable source mapping; cannot invent optimized-away variables.
2. **Compiler developer trace:** immutable AST/semantic/Bytecode/AIR stage artifacts, module ownership, diagnostics and phase timings (not an end-user debugger).
3. **Optimization proof explainer:** before/after IR with obligations, fact sources, rejected alternatives and invalidations. This is not a proof engine merely because it displays a DAG.
4. **Generated CIL/native debugger:** sequence/source maps, optimized locals/inlining limitations and honest degraded stepping.

## Text-first multi-view editing

Keep human-readable Git-friendly text the canonical editable source; formatter and parser should preserve comments/trivia and line endings. Forms/tables/graphs must produce validated source edits at an exact revision, not maintain a second mutable semantic model. Compare a projectional editor only for a concrete structural-language need. Undo/redo and concurrent change conflicts must be transactionally consistent.

## Product wedge and acceptance

Start with narrow decimal policy/pricing DSL: fixed host symbol schema, no arbitrary I/O, versioned diagnostics, deterministic interpreter, editor basics and clean consumer integration. Metrics: independent author onboarding steps and LOC, first functional sample completion, editor diagnostic correctness, cold build and runtime behavior, five discovery interviews and two pilots. Never claim customer demand before those trials. This should exercise a real customer workflow while E1 separately evaluates Ideal UT's scientific hypothesis.

## AI scope

Model tools are read/propose/validate clients of the same typed compiler services. `PreviewEdit` returns a deterministic diff and diagnostics without mutating current selected world; `ApplyApprovedEdit` requires authorization plus an exact snapshot match. Compare seeded editor refactoring/rename tasks against a plain assistant on correctness, regression count and human review time. LLM output can never declare an unsafe transformation sound.
