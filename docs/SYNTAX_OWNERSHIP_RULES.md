# Syntax ownership rules

This document defines mandatory syntax ownership rules for UniversalToolchain, Wist, and every future DSL built with the framework.

Violating these rules is a release-blocking architecture defect.

## Core law

Language syntax must be recognized only by the owning lexer, parser, AST node creators, AST visitors, or syntax-specific extractors built from parser output.

Production validators, facades, resolvers, runtime wrappers, catalogs, CLI commands, optimizers, and convenience layers must consume structured syntax output. They must not rediscover language syntax from raw source text.

Structured syntax output means one of:

- tokens produced by the owning lexer;
- AST nodes produced by the owning parser;
- declaration models produced by a parser-backed extractor;
- descriptors, symbols, typed expressions, or compiled plans produced from the parser pipeline.

## Disallowed production patterns

Do not recognize language constructs through:

- regular expressions;
- line splitting;
- `StartsWith`, `Contains`, `IndexOf`, or substring slicing over source text;
- manual parenthesis, bracket, or brace matching outside the parser;
- one-off scanners inside validators, facades, resolvers, runtimes, CLI commands, optimizers, or catalogs.

These patterns create a second parser, ignore module ownership, break extensibility when syntax changes, and make framework behavior depend on local shortcuts rather than declared language structure.

## Allowed exceptions

Raw string matching is allowed only for non-language input or true syntax owners:

- lexer/tokenizer internals that own lexical recognition;
- parser/node creators that own syntax recognition;
- parser-backed syntax extractors that consume tokens or AST;
- tests, docs, and examples;
- command-line option parsing, JSON, paths, manifests, or other non-language external formats;
- temporary diagnostic scripts outside production code.

## Required implementation direction

If a feature needs syntax-level information, implement or reuse the structured model first.

A missing parser, AST, or declaration model is not permission to parse raw source text locally.

If the structured model does not exist yet, the correct implementation task is to add that model or leave the feature incomplete and document the limitation. A regex/string workaround is not an acceptable MVP.

## Rule-local binding validation

Rule-local binding validation must be based on structured syntax output.

A validator may inspect declarations such as:

- local binding declaration name;
- declaration order;
- parameter references;
- typed expression model;
- rule-local scope model.

A validator must not infer these facts by scanning rule body text.

Correct ownership:

- parser owns local binding syntax;
- a parser-backed extractor may expose local binding declaration models;
- a validator validates duplicate locals, parameter shadowing, ordering, and scope using those models;
- facade only orchestrates extraction, validation, and compilation.
