---
title: Parser Extension
description: Show how parser behavior is extended.
---

# Parser Extension

Parser extensions turn token streams into AST structure.

A module that introduces syntax should register parser node creators through its frontend module. Parser behavior is part of the language contract, not a local implementation detail.

## When to read this page

Read this page when a feature needs new syntax shape: operators, declarations, conditions, loops, function calls or grouped constructs.

## Goal

Understand how parser node creators should be registered, prioritized and tested.

## Where parser extensions are registered

A frontend module registers parser node creators in `InitParser`:

```csharp
public void InitParser(IParser parser) => parser.AddNodeCreators(...);
```

For example, arithmetic registers operator node creators with explicit priorities:

```csharp
private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
[
    new(-40f, new UnaryMinusOperationNodeCreator()),
    new(-31f, new MultiplicationOperationNodeCreator()),
    new(-31f, new DivisionOperationNodeCreator()),
    new(-30f, new AdditionOperationNodeCreator()),
    new(-30f, new SubtractionOperationNodeCreator())
];
```

The exact priority values are implementation details, but the idea is important: parser ordering is deliberate.

## What a node creator owns

A node creator should own one syntax pattern or one tightly related family of patterns.

Examples:

- arithmetic operator creators own binary arithmetic expressions;
- variable creators own `let` declaration and assignment forms;
- loop creators own `while` and `for` forms;
- condition creators own conditional expression forms.

A node creator should not become a generic fallback that guesses what unrelated syntax means.

## Priority is semantic

Priority decides how overlapping parser candidates are applied. Treat it as part of the grammar design.

Good priority work:

- explains why the feature belongs before or after nearby syntax;
- has tests for precedence and grouping;
- has negative tests for malformed syntax;
- avoids changing existing priorities unless the intended grammar change is clear.

Bad priority work:

- copying a nearby priority until one test passes;
- using priority to hide an ambiguous syntax design;
- fixing one construct by making another construct parse differently;
- relying on reflection or registration order instead of explicit priority.

## Minimal examples to test

For arithmetic precedence:

```wist
2 + 3 * 4
```

Expected result:

```text
14
```

For grouping:

```wist
(2 + 3) * 4
```

Expected result:

```text
20
```

For variables:

```wist
let x = 5
x = x + 1
x
```

Expected result:

```text
6
```

For loops:

```wist
let sum = 0
let i = 1

while (i <= 5) (
    sum = sum + i
    i = i + 1
)

sum
```

Expected result:

```text
15
```

These examples are useful because they exercise interaction between parser extensions, not only one isolated token.

## Syntax ownership rule

Syntax must be owned by the lexer/parser/AST path.

Do not add behavior like this:

```text
raw source contains "some keyword"
  -> bypass parser
  -> create semantics manually
```

That kind of shortcut breaks dialect composition and makes feature ownership unclear.

## Dependency awareness

Parser extensions often depend on other modules:

- variable syntax needs identifiers;
- loops usually need variables and comparisons to be useful;
- conditional expressions need equality or comparison modules depending on the condition;
- function calls need both call syntax and a runtime call target surface.

Document these dependencies in the feature page or module reference. Also test that the feature is unavailable when the owning module is omitted.

## What to test

A parser extension should have tests for:

- the smallest valid syntax;
- a realistic multi-line example;
- malformed syntax;
- precedence or priority conflicts;
- grouped syntax;
- disabled dialect behavior;
- compiler/interpreter parity when the feature has runtime semantics.

## Common mistakes

- Treating parser priority as a random number.
- Adding node creators without negative tests.
- Letting a node creator mutate unrelated ancestor nodes.
- Recovering missing AST structure later during bytecode generation.
- Making a generic parser layer aware of concrete module syntax.

## Next

Continue with [AST Nodes](/write-modules/ast-nodes).
