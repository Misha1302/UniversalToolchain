---
title: AST Nodes
description: Explain AST node design and ownership.
---

# AST Nodes

AST nodes are the structured representation of parsed syntax.

A module that introduces a language feature should make that feature visible as a clear AST shape before it is lowered into bytecode or AIR. AST structure is the point where syntax becomes owned semantics.

## When to read this page

Read this page after [Parser Extension](/write-modules/parser-extension) when a feature needs a new node shape or a new visitor path.

## Goal

Understand how AST ownership protects modularity and prevents later pipeline stages from guessing source syntax.

## Why AST ownership matters

A parser node creator should produce AST nodes that represent the feature explicitly.

Good pipeline:

```text
source syntax
  -> lexemes
  -> parser node creator
  -> AST node with a stable tag/shape
  -> AST visitor
  -> bytecode/AIR
```

Bad pipeline:

```text
source syntax
  -> partial generic AST
  -> later visitor scans text or child layout heuristically
  -> bytecode emitted by guesswork
```

The second path makes dialect composition fragile because semantics are no longer owned by the feature module.

## Node tags and shape

Current modules commonly identify AST nodes through tags and lexeme-derived structure. For example, arithmetic visitors check whether a node belongs to arithmetic operation tags before emitting bytecode.

A module should define or reuse AST tags intentionally:

- one tag should mean one stable semantic category;
- tags should not be overloaded for unrelated features;
- visitor code should self-filter by tag/shape;
- tests should fail when a malformed shape reaches translation.

## Children are part of the contract

The order and meaning of child nodes matters.

For a binary operation, children usually represent left and right operands. For a loop, children may represent condition and body. For a conditional expression, children represent condition and result expressions.

Document the expected shape in the module's developer documentation or tests when it is not obvious.

## Visitor self-filtering

AST visitors must not assume that every node belongs to them.

A safe visitor pattern is:

```csharp
public void TryVisit(BytecodeVisitorData data)
{
    if (!IsOwnedNode(data.Node))
        return;

    // translate children and emit owned semantics
}
```

This matters because multiple visitors may see the same AST node. A visitor that does not self-filter can double-emit instructions or consume another module's syntax.

## Translation boundaries

An AST visitor should emit only semantics owned by its module.

For example:

- an arithmetic visitor may translate operands and emit arithmetic operation bytecode;
- a variable visitor may emit declaration, lookup or assignment behavior;
- a loop visitor may emit loop control structure;
- a condition visitor may emit conditional control behavior.

A visitor should not repair missing parser output by searching raw source text or by assuming unrelated child layouts.

## Shared AST state

Shared state between visitors is allowed only when explicit and scoped.

Good shared state:

- created per compilation or per translator setup;
- passed explicitly to cooperating visitors;
- narrow in responsibility;
- covered by tests proving no leakage between compilations.

Bad shared state:

- static mutable dictionaries storing compile-specific information;
- state that depends on previous runs;
- hidden coupling between unrelated visitors;
- state used to bypass dialect selection.

## What to test

For AST node work, test:

- valid syntax creates executable semantics;
- malformed syntax fails before incorrect bytecode is emitted;
- visitor self-filtering does not double-emit;
- nested constructs preserve shape;
- disabled dialects reject the feature;
- compiler/interpreter parity when both backends support the feature.

## Common mistakes

- Treating AST nodes as disposable parser artifacts rather than semantic contracts.
- Encoding feature meaning only in child indexes without tests.
- Letting visitors infer syntax from raw text.
- Reusing a tag for unrelated syntax because it is convenient.
- Adding AST behavior without matching negative tests.

## Next

Continue with [Bytecode Generation](/write-modules/bytecode-generation).
