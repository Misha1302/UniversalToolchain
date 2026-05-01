---
title: Lexer
description: Document lexing responsibilities and extension points.
---

# Lexer

The lexer converts processed source text into `LexemeValue` objects.

In the current implementation, lexing is performed by `BasicLexerImpl`. Frontend modules contribute token patterns before lexing through `IFrontendCoreModule.InitLexer`.

## Place in the pipeline

The lexer runs after module text preprocessing and before lexeme post-processing:

```text
modules.ProcessText
  -> modules.InitLexer
  -> lexer.Lexemize
  -> modules.ProcessLexemes
```

Lexing should recognize tokens. It should not decide high-level language structure.

## Current implementation shape

`BasicLexerImpl.Lexemize` works in this order:

1. copy configured patterns into a local list;
2. collect all regex matches for all patterns;
3. sort matches by source position and then by pattern registration order;
4. walk through the source text from left to right;
5. at each position, take the next matching lexeme;
6. throw a lexer error if there is an unmatched gap;
7. skip lexemes whose type is configured as ignored;
8. return the filtered `LexemeValue` list.

This is not a simple “try the next regex and advance” scanner. It first gathers matches and then consumes them in source order.

## Pattern order matters

When multiple patterns match at the same position, the order of patterns in the lexer configuration matters.

Frontend modules should therefore register lexemes deterministically. If two modules introduce overlapping token patterns, the resulting behavior depends on deterministic module and pattern order.

Do not rely on reflection enumeration order for lexeme registration.

## Invalid gaps

The lexer expects the entire source text to be covered by registered patterns, including whitespace or ignored lexemes.

If the current source index does not match the next lexeme start, the lexer reports an invalid token segment.

This means a dialect must register whitespace/comment handling when ordinary source text contains whitespace/comments that should be ignored.

## Ignored lexemes

Some lexemes are recognized but omitted from the final token list. This is how whitespace and comments can be handled without losing coverage of the source text.

The important distinction is:

- recognized and ignored: valid input, not passed to the parser;
- not recognized: invalid token gap.

## Regex timeout

Regex matching uses a timeout. This prevents a single token pattern from blocking lexing indefinitely.

When adding complex token patterns, keep them simple and test them against representative input.

## Module responsibilities

A frontend module that introduces token syntax should:

- register lexemes in `InitLexer`;
- keep token names stable once parser code depends on them;
- avoid recognizing syntax through raw text scans outside the lexer/parser path;
- add tests for valid tokens and invalid gaps;
- document dependencies on whitespace/comment modules when relevant.

## Examples

Arithmetic modules register operator tokens such as addition, subtraction, multiplication and division.

Variable modules register `let`.

Loop modules register keywords such as `while` and `for`.

These registrations make tokens visible. Parser node creators are still responsible for turning tokens into AST structure.

## Common mistakes

- Assuming ignored whitespace does not need to be recognized.
- Adding a parser node creator without registering the token it depends on.
- Adding overlapping token patterns without deterministic order tests.
- Using text preprocessing to simulate lexing.
- Treating token names as private when parser/visitor code depends on them.

## What to test

For lexer changes, test:

- a valid token appears with the expected type;
- ordinary whitespace/comment handling still covers the source text;
- invalid characters produce a lexer failure;
- overlapping patterns resolve deterministically;
- disabling a module removes the token from that dialect surface.

## Next

Continue with [Parser](/internals/parser).
