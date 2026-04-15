# Wist2 vs RulesEngine

## Where RulesEngine is strong

RulesEngine is a good fit for externalized business rules in .NET applications.

It is strong when rules are structured as policies or workflows, when JSON-defined rules match the product model, and
when the team wants a familiar rules-engine shape rather than a language runtime.

## Where UniversalToolchain starts to make sense

UniversalToolchain starts to make sense when the rule surface needs to become more language-like.

Use it when the product needs:

- its own syntax instead of a fixed rules representation,
- logic that is closer to a small language than a rule table,
- execution and runtime control across backend surfaces,
- dialect-based restrictions that define which capabilities exist.

This is not a replacement rule engine for every workflow case. It is a framework for building controlled language
runtimes when rules are only one part of the runtime story.

## Simple decision rule

If structured externalized rules solve the problem, use RulesEngine.

If the product needs custom language/runtime control, UniversalToolchain is the better fit.
