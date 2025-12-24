# Project Rules and Conventions

This document describes **implicit rules** used throughout the project that **must be followed** when developing new modules, nodes, and visitors.

---

## Priorities (Lexer / Parser / Bytecode)

### General Rule
**Priorities are always sorted in ascending order from left to right**  
(smaller value — processed earlier).

### Example
```text
[-2] [-1] [0] [1]
 ^ highest priority
```

---

## Lexer

- The `priority` property of a `LexemePattern`
- Smaller value → higher priority
- More specific patterns should come first

```csharp
TryAddPattern(">=", Token.GreaterOrEqual, priority: -1);
TryAddPattern(">",  Token.Greater,        priority: 0);
```

---

## Parser / NodeCreators

### Rules
1. `NodeCreators` — `SortedDictionary`
2. Traversal from left to right
3. Smaller value → higher priority

```csharp
NodeCreators.Add(-1, new Multiply());
NodeCreators.Add( 0, new Add());
```

---

## AST NodeCreators

### Forbidden
❌ Create new `AstNode` instances

### Allowed
✅ Change `NodeType` <br>
✅ Reorder existing nodes

```csharp
node.NodeType = AstNodeType.Addition;
```

### Safe Access
```csharp
var left = scope.SafeGet(i - 1);
if (left == null) return false;
```

---

## ParserHandled

If a node has been processed by the parser — **must** be marked:

```csharp
child.MarkAsParserHandled();
```

---

## Working with Children

### Adding
`Parent` is set automatically

```csharp
node.Children.Add(child);
```

### Removing
Indexes shift — adjust manually

```csharp
scope.Children.RemoveAt(i - 1);
i--;
```

---

## Bytecode Visitor

### Template
1. Translate children first
2. Then translate the current node

```csharp
foreach (var child in data.Node.Children)
    data.BytecodeTranslator.Translate(child);
```

---

## Stack

### Main Rule
Arguments are placed so that the type can be deduced

```text
a = 5
```

```text
push 5
push ref(a)
set
```

---

## Types

The result type:
- either known explicitly
- or taken from the `Stack`

```csharp
context.Stack[0]
```

---

## C# Interop

### Limitations
❌ Interfaces <br>
✅ Generic methods with constraints

```csharp
Add<T>(T a, T b) where T : IAddable<T>
```

---

## Exceptions

### Forbidden
```csharp
throw new Exception();
```

### Mandatory
```csharp
Thrower.InvalidOpEx();
Thrower.AssertAlways(cond);
obj.NotNull();
```

---

## AssertAlways

Used **instead of** `Debug.Assert`

```csharp
Thrower.AssertAlways(stack.Count > 0);
```

---

## ExtensibleEnum

### Rules
❌ Hardcoding int values <br>
✅ Only `CreateOrGet`

```csharp
AstNodeType.CreateOrGet("If");
```

---

## Jumps / Labels

- All jumps are directional
- Labels via `Guid`

```csharp
var label = Guid.NewGuid();
il.Jmp(label);
```

---

## Forbidden Practices

❌ New AST creation <br>
❌ Direct throw <br>
❌ Violating stack order <br>
❌ Hardcoding enum values <br>

---

## If a Rule is Missing

➡ Look at existing code and follow the style <br>
➡ This file is a **living document**