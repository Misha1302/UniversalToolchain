---
title: Parser
description: Document parser architecture and extension points.
---

# Parser

The parser converts lexeme values into an AST.

In the current implementation, parsing is performed by `BasicParserImpl`. Frontend modules contribute parser node creators before parsing through `IFrontendCoreModule.InitParser`.

## Place in the pipeline

The parser runs after lexeme post-processing and before AST post-processing:

```text
modules.ProcessLexemes
  -> modules.InitParser
  -> parser.Parse
  -> modules.ProcessAst
```

The parser owns syntax structure. It should not decide backend execution behavior.

## Current implementation shape

`BasicParserImpl.Parse` works in this order:

1. convert every lexeme into an AST node with initial type `Unknown`;
2. create a root `Program` node containing those nodes;
3. set node types from lexeme types;
4. run configured node creators through `ParseRoot`;
5. validate the resulting tree with `TreeValidator`;
6. return the root AST node.

This means parsing starts from a flat token-derived AST and then rewrites it into structured nodes.

## Node creators

Parser behavior is provided by `IAstNodeCreator` instances grouped in parser configuration.

A node creator receives:

- the current scope node;
- the current child index;
- the nearby children in that scope.

It may replace or reorganize children when it recognizes syntax it owns.

Examples:

- arithmetic creators combine operand/operator/operand forms;
- variable creators build declaration or assignment nodes;
- loop creators build `while` or `for` nodes;
- condition creators build conditional nodes.

## ParseScope algorithm

`ParseScope` is recursive and restart-oriented:

1. recursively parse child scopes first;
2. iterate over children in the current scope;
3. try registered node creators whose visit predicate accepts the child;
4. when a creator succeeds, mark the child as parser-handled;
5. recursively parse the changed node;
6. reset the index and scan the scope again.

This reset matters. A successful creator can change the child list, so the parser restarts scanning the scope to let later syntax become visible.

## Parser-handled nodes

After a creator succeeds, the parser marks the node as handled.

The default visit predicate skips handled nodes. A creator can provide a custom visit predicate when it needs different behavior.

This prevents the same syntax from being repeatedly consumed by unrelated creators.

## Priority and ordering

The parser runs node creators through configured groups. Priority and registration order affect which creators see ambiguous syntax first.

Examples:

- multiplication should bind tighter than addition;
- unary minus must be handled in a position compatible with binary operations;
- `for` and `while` should not steal syntax from unrelated constructs;
- optional grouping forms must be tested when accepted.

Parser priority is therefore a grammar decision, not a magic number.

## Scope-sensitive syntax

Different node creators can require different child shapes.

For example, current `while` parsing takes the next AST node as the condition and the following AST node as the body. The condition does not have to be a `Scope` when it parses as one expression node.

By contrast, current `for` parsing expects initialization, condition, update and body to be grouped as `Scope` nodes.

Internals documentation should describe these differences as current implementation facts, not infer a global rule from one construct.

## What parser extensions must not do

A parser extension should not:

- inspect raw source text after lexing;
- rely on accidental reflection order;
- mutate unrelated ancestor nodes;
- create backend-specific nodes;
- hide missing module dependencies;
- accept syntax that should be unavailable in a restricted dialect.

## What to test

Parser changes need tests for:

- minimal valid syntax;
- malformed syntax;
- precedence and grouping;
- overlapping constructs;
- optional syntax forms that are intentionally accepted;
- disabled dialect behavior;
- downstream parity when parsing changes emitted semantics.

## Next

Continue with [AST](/internals/ast).
