# Current architecture status

This document describes the currently supported surface of the branch. It is intentionally short and practical.

Use it to distinguish current behavior from future or historical design plans.

## Rules feature

Status: removed from this branch.

Removed surfaces:

- `UniversalToolchain.Rules.Abstractions`;
- Wist rule runtime files under `UniversalToolchain.Dialects.Wist/Rules`;
- `RuleDeclarationsModule`;
- `CompileRuleSet`;
- `rule-run`;
- `rule-schema`;
- executable `pricing-rules`, `validation-rules`, and `policy-rules` profiles;
- rule runtime type bindings;
- rule-specific diagnostic codes;
- raw-source rule declaration parser.

Do not restore rules code, rule profiles, rule CLI commands, or marker-only rule modules without a new explicit architecture task.

## FunctionCalls and SafeMathFunctions

Status: limited MVP.

Currently supported:

- source-level calls to provider-backed built-in functions;
- SafeMathFunctions through the neutral `function-calls-safe-math` example profile;
- interpreter/compiler parity coverage for the supported SafeMath MVP.

Not final yet:

- a shared function call planner;
- full type-directed overload resolution;
- final diagnostics parity for every function authoring error.

## Let bindings

Status: normal Wist `let` support exists.

There is no rule-local LetBindings layer in this branch because the rules feature has been removed.

Forbidden shortcuts:

- raw-source local binding scanners;
- restoring rule-local validation tests without first restoring an explicit rules architecture task.

## Local variables runtime path

Status: migrated to execution-scoped C# runtime calls.

Current behavior:

- local variables are lowered to ordinary `call C#` instructions via `VariablesRuntimeCallProvider` and `VariablesRuntimeCalls`;
- local variable state is session-scoped through `ExecutionEnvironment` runtime context storage;
- `VariablesContainer<T>` static storage is removed from the production runtime path;
- `LocalVariablesOptimization` is removed from the current runtime path.

Future direction:

- any local-variable optimization must operate on generated C# runtime call patterns;
- do not reintroduce local-variable intrinsics or static/global variable storage.

## Documentation policy

`docs/DOCUMENTATION_RULES.md` defines how agents must handle stale Markdown examples and architecture documents.
