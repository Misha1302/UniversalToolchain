# Why This Exists

## Problem

Hardcoded formulas, rules, and domain logic start simple and become expensive fast.

At first, a condition in C# or a compact expression evaluator is enough. Then the product needs tenant-specific pricing,
versioned validation rules, policy changes without redeploying the app, and a way to reject language features that should
not exist in that domain. The problem is no longer only "evaluate this expression." The problem is owning a small runtime
surface without turning every rule into application code.

## Why a plain evaluator is sometimes not enough

A plain evaluator is useful while the shape of the language stays fixed.

It is usually enough when:

- the syntax is fixed,
- every accepted expression can use the same capabilities,
- there is no need to grow toward a domain-specific language,
- one execution surface is enough.

It starts to strain when the product needs custom syntax, restricted dialects, capability control, diagnostics, or a path
from expressions into a more explicit runtime model.

## Why a full language workbench is sometimes too much

Full language workbench approaches can be the right answer for large language programs.

They are often too heavy when the target is an embeddable .NET runtime layer. An application team may need parser,
validation, translation, and execution hooks, but not a full platform story with heavyweight tooling, editor integration,
and broad language governance before the first production rule can run.

## Where UniversalToolchain fits

UniversalToolchain sits between simple expression evaluators and heavy language-platform work.

It is for .NET applications that need configurable runtime language behavior, but still want the implementation to stay
embeddable, modular, and practical.

## Good entry scenarios

- Pricing formulas that start as arithmetic and grow into controlled product logic.
- Validation rules that need domain-specific constraints instead of arbitrary host-language behavior.
- Routing or policy logic where allowed capabilities differ by context, tenant, or environment.
