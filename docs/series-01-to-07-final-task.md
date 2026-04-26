# Series 01-07 Final Task

This is the implementation task for finishing PR #206 on branch `auto/series-01-to-07-a9li0x`.

UniversalToolchain is the product. Wist is the reference language and proving ground. Do not turn generic framework layers into Wist-, SafeMath-, pricing-, or demo-specific code.

## Non-negotiable architecture rules

1. Generic framework layers must not hardcode dialect, profile, module, function, rule, backend, or demo names.
2. Framework/core/runtime layers must not branch by shipped profile names such as `pricing-rules`, `validation-rules`, or `policy-rules`.
3. Runtime truth must flow only through dialect definition, compiled dialect slice, build plan, selected runtime plan, runtime configuration, and host/executor.
4. Capabilities and features are projection/reporting layers. They explain selected runtime composition; they do not activate behavior.
5. BasicCore must not reference Wist, Rules, SafeMath, FunctionCalls concrete modules, RuleDeclarations concrete modules, product profiles, or feature-specific modules.
6. Function names belong to function providers/modules. Parser, resolver, framework, and RuleSet facade must not own SafeMath function names.
7. Product profiles must be ordinary dialect preset/configuration files, not runtime modes.
8. Convenience APIs must be thin wrappers over existing composition/runtime paths. They must not become second runtimes.
9. All ordering must be deterministic: provider discovery, catalogs, diagnostics, feature reports, schema output, CLI output, and overload resolution.
10. Single Responsibility is mandatory: parser parses, extractor extracts, resolver resolves, projector projects, runtime executes, formatter formats, catalog describes.

## Controlled reflection policy

Reflection is allowed and recommended when it reduces compile-time coupling between generic framework layers and concrete modules.

Use reflection to discover module-owned providers through explicit composition boundaries, avoid direct framework dependencies on concrete modules, reduce manual registration boilerplate, and preserve modular extensibility.

Reflection must be bounded, deterministic, cached where appropriate, and kept out of hot execution paths.

Allowed boundaries: selected assemblies, selected dialect modules, selected provider contracts, selected compiled dialect slices, and selected runtime composition plans.

Forbidden patterns: scanning all loaded assemblies blindly, scanning the whole AppDomain without explicit boundaries, resolving behavior by concrete type names or shipped profile names, repeated reflection scans during rule/function execution, and reflection-based branching such as checking a concrete module type name.

Recommended pattern: composition selects modules; discovery scans only selected boundaries; providers are discovered through stable contracts; providers produce immutable descriptors and runtime bindings; catalogs/build plans are built deterministically; execution uses resolved plans without repeated reflection.

## Component ownership

Generic framework layers may define neutral abstractions, catalogs, provider interfaces, diagnostics, descriptors, and deterministic composition helpers. They must not contain Wist syntax, SafeMath function names, product profile names, or concrete product behavior.

The Wist layer may contain Wist parser integration, Wist AST nodes, Wist rule declaration extraction, Wist facade, and Wist lowering into the existing pipeline. It must not hardcode SafeMath names in parser/resolver or branch by product profile.

FunctionCallsModule owns generic syntax and mechanics for `identifier(args...)`. It does not own SafeMath function descriptors.

SafeMathFunctionsModule owns descriptors and runtime bindings for `min`, `max`, `abs`, `clamp`, and `round`. It does not own generic function-call syntax or rule parsing.

RuleSet API must be a facade over the existing Wist pipeline. It must not introduce a second parser, evaluator, function registry, pricing runtime, or rule-only execution engine.

IfExpression owns only conditional syntax, typing, and lowering. LetBindings owns only local binding syntax, scope, validation, and lowering. Neither may depend on concrete number, comparison, or SafeMath implementation details.

## Required implementation scope

Complete real FunctionCalls infrastructure: parse generic calls, resolve through provider catalogs, type-check arguments, lower through the existing pipeline, and execute in interpreter/CIL when the selected backend supports the binding.

Complete SafeMath runtime behavior through module-owned descriptors and runtime bindings for number-only MVP signatures: `min(number, number)`, `max(number, number)`, `abs(number)`, `clamp(number, number, number)`, and `round(number)`.

Complete modular expression typing for MVP types `number` and `bool`. Type descriptor to runtime type mapping must be provider-owned.

Complete IfExpression syntax: `if condition then expr else expr`. Condition must be bool, branch types must match, and interpreter/CIL behavior must match where both backends are enabled.

Complete LetBindings inside rule bodies. Bindings are ordered, rule-local, cannot shadow parameters for MVP, cannot duplicate locals, and cannot be affected by extra runtime arguments.

Complete Wist rule declaration parsing/extraction for rules with explicit parameters, explicit return type, zero or more `let` bindings, and one final expression.

Complete RuleSet compile/run facade with structured diagnostics, rule descriptors, argument validation, run by rule name, and no hardcoded pricing behavior.

Add ordinary dialect preset files for `pricing-rules`, `validation-rules`, and `policy-rules`. No framework code may branch by these names.

Complete CLI commands: keep `wistc features`; add or finish `wistc rule-schema` and `wistc rule-run`. CLI must use the facade/composition API and diagnostics formatter.

Synchronize docs and CI. Future examples must be marked future/non-CI or moved out of executable smoke coverage. The markdown bash smoke placeholder must not remain final technical debt.

## Final demo

A pricing-rules profile must support source equivalent to:

```wist
rule FinalPrice(price: number, quantity: number, discount: number, maxDiscount: number) -> number {
    let base = price * quantity
    let discountValue = clamp(base * discount, 0.0, maxDiscount)
    let result = base - discountValue

    if result < 0.0 then 0.0 else result
}
```

With arguments `price = 100.0`, `quantity = 3.0`, `discount = 0.15`, and `maxDiscount = 50.0`, the expected result is `255.0`.

This is only an acceptance demo. Do not hardcode the rule name, argument names, function names outside owning modules/tests/docs, or pricing semantics.

## Required tests

Add tests for FunctionCalls parser, function catalog/resolution, SafeMath runtime behavior, modular typing, IfExpression, LetBindings, rule parser/extractor, RuleSet facade, interpreter/CIL parity, CLI behavior where supported, and architecture guardrails.

Regression requirement: extra runtime arguments must never shadow local bindings.

## Validation

Before final response, run or report inability to run:

```bash ci-timeout=300s
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Wist.sln -c Release --no-build
```

Also run repository-specific validation and anti-hardcode checks. Do not claim green checks unless they were actually run or CI confirms them.

## PR body update

Before final, update the PR body with implemented architecture, provider/catalog/runtime binding flow, RuleSet facade design, reflection/discovery boundaries, capability reporting versus runtime activation, type descriptor ownership, product profiles as ordinary presets, tests added, commands run, and intentionally deferred work.

Keep the PR open. Do not merge.
