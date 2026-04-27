# Current architecture status

This document describes the currently supported surface of the branch. It is intentionally short and practical.

Use it to distinguish current behavior from future or historical design plans.

## Rules and RuleSet API

Status: temporarily removed from public runtime surface.

Currently not supported:

- `CompileRuleSet`;
- `rule-run`;
- `rule-schema`;
- executable `pricing-rules`, `validation-rules`, or `policy-rules` profiles;
- runtime-visible `RuleDeclarationsModule`;
- raw-source rule declaration parser.

Future rules work must start from parser-owned and AST-backed rule declarations. It must not reintroduce raw-source rule parsers, marker-only runtime modules, or CLI/facade commands before a real implementation exists.

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

Rule-local LetBindings validation is not currently implemented because the temporary RuleSet surface has been removed.

Forbidden shortcuts:

- raw-source local binding scanners;
- pretending rule-local validation is complete;
- restoring rule-local validation tests without AST-backed extraction.

## Documentation policy

`docs/DOCUMENTATION_RULES.md` defines how agents must handle stale Markdown examples and architecture documents.
