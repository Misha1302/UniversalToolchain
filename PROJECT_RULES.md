# Project Rules and Conventions

This document describes the implicit rules used throughout the project, which must be followed when developing new
modules, nodes, and visitors.

## General Principles

### Priorities

Priorities are sorted in ascending order from left to right (lower value → higher priority).

### Stack (LIFO)

Data is added to the end of the stack and removed from the end. This applies to both the value stack and the type stack.

### Extensible Enums (ExtensibleEnum)

It is forbidden to create instances via the constructor. Use only `CreateOrGet`.

```csharp
// Correct
AstNodeType.CreateOrGet("Addition");
// Incorrect
new ExtensibleEnum<AstNodeTag>(5);
```

### Tags in AST

Nodes can contain tags to mark states:

* `CurrentTags` — tags added directly to the node.
* `AllTags` — the union of the node's tags and all its parents' tags.

## Lexer

### Pattern Priorities

* Patterns with higher priority (lower number) are processed first.
* Specific patterns must have higher priority than general ones.

```csharp
// Pattern ">=" is processed before ">"
lexer.Configuration.TryAddPattern(
    new LexemePattern(@"\>\=", lexemeType),
    priority: -1
);
lexer.Configuration.TryAddPattern(
    new LexemePattern(@"\>", lexemeType),
    priority: 0
);
```

## Parser and AST

### Node Creation (AstNodeCreator)

**Forbidden:**

* Modifying the tree structure upper than parent.

**Allowed:**

* Changing the `NodeType` of existing nodes.
* Moving existing nodes between parents.
* Creating new nodes.
* Adding tags to nodes.

### Safe Access to Child Nodes

Always use `SafeGet` to check child nodes.

```csharp
var left = scope.SafeGet(i - 1);
if (left == null) return false;
```

### Marking Processed Nodes

If a node has been processed by the parser, it must be marked.

```csharp
child.MarkAsParserHandled();
```

### Working with Child Nodes

When adding a node to a parent, the `Parent` property is set automatically.

```csharp
parent.Children.Add(child); // Parent of child is set to parent
```

When removing nodes, indices shift. The index must be adjusted manually.

```csharp
scope.Children.RemoveAt(i - 1);
i--; // Adjustment after removal
```

### NodeCreator Priorities

`NodeCreators` are processed in order of priority (from lower to higher).

```csharp
// First multiplication and division (priority -1)
parser.Configuration.NodeCreators.Add(-1, new MultiplicationOperationNodeCreator());
parser.Configuration.NodeCreators.Add(-1, new DivisionOperationNodeCreator());
// Then addition and subtraction (priority 0)
parser.Configuration.NodeCreators.Add(0, new AdditionOperationNodeCreator());
parser.Configuration.NodeCreators.Add(0, new SubstractionOperationNodeCreator());
```

## AST Visitors (Bytecode Visitors)

### Processing Order

First, all child nodes are processed, then the current node.

```csharp
public void TryVisit(BytecodeVisitorData data)
{
    // First, process children
    foreach (var child in data.Node.Children)
        data.AstToBytecodeTranslator.Translate(child);

    // Then the current node
    // ...
}
```

### Type Stack in Context

Access to types on the stack is through `context.Stack`. Types are added to the end of the stack.

```csharp
// For a binary operation: two arguments are already on the stack
var arg1Type = context.Stack[^2]; // First argument
var arg2Type = context.Stack[^1]; // Second argument
```

## Type System and Stack

### Argument Order

Arguments are placed on the stack in direct order (left to right), but are accessed from the end.

```
Expression: a + b
Stack order: push a, push b
Type access: Stack[^2] = a, Stack[^1] = b
```

### Determining the Result Type

The result type of an operation:

* Either known explicitly (for literals, method calls).
* Or inferred from the types of arguments on the stack.

### Working with Generic Types

Use `GenericTypeResolver` to resolve generic method parameters. Constraints of generic methods must be compatible with
the project's type system.

## C# Interop

### Limitations

**Supported:**

* Static and instance methods.
* Generic methods with constraints.
* Constructors.

**Not Supported:**

* Interfaces as parameters (only concrete types).
* Overloaded methods with the same number of parameters.

### Example of a Generic Method

```csharp
// Supported
public static T Add<T>(T a, T b) where T : IAddable<T>
{
    return T.Add(a, b);
}
```

### Calling C# Methods

Methods are called via the intrinsic "call C#". The method signature must match the types on the stack.

## Intrinsics and Backends

### The Set of Intrinsics Depends on the Backend

Different backends support different intrinsics.

| Backend      | Supported Intrinsics                             | Not Supported           |
|:-------------|:-------------------------------------------------|:------------------------|
| CIL Compiler | call C#, store_local, load_local, load_local_ref | -                       |
| Interpreter  | call C#, call C# ctor                            | store_local, load_local |

### Registering New Intrinsics

When adding a new intrinsic, its handler must be registered in `AirTypes`.

```csharp
AirTypes.TryRegisterIntrinsic(
    "new_intrinsic",
    (instruction, stack) => {
        // Processing types on the stack
    }
);
```

## Peephole Optimizations

### General Principles

* Optimizations are performed at the intermediate representation (AIR) level.
* Instruction patterns are recognized and replaced with more efficient ones.
* Optimizations must not change program semantics.

### Example Variable Optimization

Pattern `Push(string) + GetRef` → `load_local_ref`:

```
Before optimization:
  Push "varName"
  Intrinsic "call C#", VariablesContainer<>.GetRef

After optimization:
  Intrinsic "load_local_ref", "varName", varType
```

### Rules for Optimizations

* Optimization must preserve the order and number of stack operations.
* Types on the stack must remain consistent.
* Instructions affecting side effects cannot be removed.

## Error Handling

### Using Thrower

All exceptions must be thrown via `Thrower`.

```csharp
// For condition checks
Thrower.AssertAlways(stack.Count > 0, "Stack is empty");

// For unimplemented functionality
Thrower.NotImplementedException("Method not implemented");

// For invalid operations
Thrower.InvalidOpEx("Invalid operation");

// For null checks
obj.NotNull("Object cannot be null");
```

**Important:** Do not use direct calls to `throw new Exception()`.

### Error Messages

The message format is not strictly regulated, but it is recommended to provide:

* A clear description of the problem.
* Error context (if appropriate).
* Do not reveal internal implementation details.

## Forbidden Practices

* Directly throwing exceptions — use only `Thrower`.
* Violating stack order — arguments must be in the correct order.
* Hardcoding enumeration values — use only `CreateOrGet`.
* Using interfaces in C# interop — only concrete types.
* Modifying tree structure in visitors — only in `NodeCreator`.
* Ignoring marking of processed nodes — always use `MarkAsParserHandled`.
* Creating Jump/Label without Guid — all labels must be unique.

## If a Rule is Missing

* Study the existing code in the relevant module.
* Follow the general style and architectural principles.
* This is a living document; propose changes when inconsistencies are found.

## Architecture Notes

### Modularity

Each module should be independent and perform one clear task. Modules register their handlers via the
`IFrontendCoreModule`/`IMiddleEndCoreModule` interfaces.

### Extensibility

New features are added through modules, not by modifying the core. Use existing extension points:

* `InitLexer`, `InitParser`, `InitAstTranslator`
* `ProcessAst`, `ProcessBytecode`, `ProcessIr`

### Performance

Keep in mind that the parser can traverse the tree multiple times. Avoid complex operations in `TryCreateNode`.

---
*This document is current based on analysis of the codebase. Last checked: all code examples are taken from existing
modules.*