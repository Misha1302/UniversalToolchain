# Wist2 vs NCalc

## Where NCalc is strong

NCalc is a good fit when the job is expression evaluation.

It is strong for compact formulas, simple embedding, familiar expression syntax, and application code that needs to
evaluate configurable values without building a language pipeline. If the product needs only a normal evaluator, NCalc is
usually the simpler thing to own.

## Where UniversalToolchain starts to make sense

UniversalToolchain starts to make sense when expression evaluation is no longer the whole problem.

Use it when the product needs:

- custom syntax instead of one fixed expression grammar,
- restricted dialects for different domains or trust levels,
- capability control over what language features exist,
- multiple execution surfaces such as interpreter and compiler paths,
- a path from expressions toward a DSL runtime.

That boundary matters. UniversalToolchain is not trying to be a smaller evaluator. It is runtime infrastructure for
language behavior that may need to grow.

## Simple decision rule

If you only need an evaluator, use NCalc.

If you need a controlled path toward DSL/runtime behavior, UniversalToolchain is the more relevant category.
