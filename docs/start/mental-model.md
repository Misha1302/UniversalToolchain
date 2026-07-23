---
title: Mental Model
description: Distinguish the Wist compiler pipeline from the generic typed route model.
---

# Mental model

UniversalToolchain has a common design principle—**plan first, activate only selected components, validate boundaries**—but not every language uses the same internal artifacts.

## Wist model

```text
source
-> lexer/parser
-> AST
-> Bytecode
-> AIR
-> optimizers
-> interpreter or CIL backend
-> result / compiled artifact
```

A `.wistdialect` selects Wist modules, optimizers and backends. Wist uses interpreter/CIL parity to protect shared semantics.

## Generic language-authoring model

```text
Authored packages
-> LanguageDefinition
-> LanguageCompiler
-> LanguagePlan
-> LanguageRuntime
-> typed route selected for one backend
-> result
```

A language chooses its own artifact contracts. A small language may route `string -> SyntaxTree -> result`; another may use syntax, bound tree, AIR, SSA and executable artifacts.

## Important distinctions

- **feature** is user-selected capability;
- **contribution** is one implementation participant;
- **slot** is an ownership location;
- **artifact contract** is a typed boundary;
- **pass** preserves an artifact contract;
- **conversion** changes an artifact contract;
- **backend** owns the terminal executor;
- **runtime provider** owns session construction, not language semantics by itself.

## Security boundary

Deterministic planning, restricted features and no-host-interop policy reduce the selected surface. They do not make hostile code or hostile extension packages safe in-process. See [Security](/SECURITY).
