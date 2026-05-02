---
title: Create Your First Module
description: Build a small frontend module from syntax idea to tested dialect behavior.
---

# Create Your First Module

This tutorial builds a real, intentionally small frontend module: `TextualAddition`.

It adds this Wist syntax:

```wist
2 plus 3
```

Expected result:

```text
5
```

The feature is deliberately small. It exercises the module pipeline without requiring a new backend, new AIR shape, or new intrinsic.

## What you will build

| Concern | Value |
|---|---|
| Module alias | `TextualAddition` |
| Runtime export | `FrontendModule/TextualAddition` |
| New syntax | `<expression> plus <expression>` |
| Parser precedence | same as addition/subtraction |
| Runtime behavior | lower to the existing `Add` operation |
| Required related modules | `Whitespaces`, `Numbers`, `Scopes`, `Arithmetic` |
| Tests | positive execution, precedence, missing-module rejection |

## Files created

| Concern | File |
|---|---|
| Module entry point | `UniversalToolchain/ArithmeticModule/Module/TextualAdditionModuleImpl.cs` |
| Parser node creator | `UniversalToolchain/ArithmeticModule/Creators/TextualAdditionOperationNodeCreator.cs` |
| AST visitor | `UniversalToolchain/ArithmeticModule/Visitors/TextualAdditionAstVisitor.cs` |
| Tests | `UniversalToolchain/UniversalToolchain.Dialects.Tests/TextualAdditionModuleTests.cs` |

The example lives in `ArithmeticModule` because it is semantically arithmetic-related. A larger external feature can live in its own project, but the same responsibilities apply.

## Step 1. Define the feature boundary

`TextualAddition` owns exactly one source-level feature: the keyword-like binary operator `plus`.

It does not own numbers, scopes, multiplication, backend selection, or optimizer behavior. Those remain separate module/backend concerns.

## Step 2. Add the module entry point

The module entry point declares dialect metadata and registers lexer, parser, and AST translation contributions.

```csharp
[DialectModuleAlias("TextualAddition")]
[DialectRuntimeExport("FrontendModule", "TextualAddition")]
[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class TextualAdditionModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"\bplus\b", "TextualAddition", Priority: 110f)
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-30f, new TextualAdditionOperationNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new TextualAdditionAstVisitor());
}
```

Important details:

- `DialectModuleAlias` makes the module selectable from a dialect file.
- `DialectRuntimeExport` exposes it to manifest-backed runtime composition.
- `\bplus\b` prevents accidental matches inside words such as `surplus`.
- priority `110f` keeps `plus` above identifier-like lexemes when both are selected;
- parser priority `-30f` matches ordinary addition/subtraction precedence.

## Step 3. Add the parser node creator

The parser node creator reuses the existing binary-operation parser shape:

```csharp
public class TextualAdditionOperationNodeCreator() : BinaryOperationBase("TextualAddition");
```

This transforms a flat token sequence like `2 plus 3` into a binary AST node with left and right operands.

The node type remains `TextualAddition`, not `Addition`, because this module owns a distinct syntax surface. The AST visitor maps that syntax to addition semantics explicitly.

## Step 4. Add the AST visitor

The visitor must self-filter and emit bytecode only for `TextualAddition` nodes.

```csharp
public void TryVisit(BytecodeVisitorData data)
{
    if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("TextualAddition"))
        return;

    foreach (var child in data.Node.Children)
        data.AstToBytecodeTranslator.Translate(child);

    var method = new AbstractMethodImpl(
        "Op_plus",
        (il, context) => il.CallCSharp(context.Stack[^1].GetMethod("Add").NotNull())
    );

    data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
}
```

The stack order is:

```text
left operand -> right operand -> Add operation
```

That mirrors the existing arithmetic visitor and keeps compiler/interpreter behavior aligned.

## Step 5. Enable the module in a dialect

The syntax exists only when the dialect selects the module:

```text
dialect TextualAdditionDemo
use Whitespaces,Numbers,Scopes,Arithmetic,TextualAddition
backend compiler,interpreter
```

Without `TextualAddition`, `2 plus 3` should be rejected. This is part of the module contract, not an optional nicety.

## Step 6. Add tests

The module test file covers three cases:

```csharp
[Test]
public void TextualAddition_Module_ExecutesPlusKeyword()
{
    var result = DialectTestHostInfrastructure.RunInBothBackends(TextualAdditionDialect, "2 plus 3");

    Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(5.0d).Within(1e-9));
}
```

Also test:

- `2 plus 3 * 4` returns `14`, proving addition-level precedence;
- `2 plus 3` fails when the dialect selects `Arithmetic` but not `TextualAddition`, proving dialect visibility is real.

Use `BackendParityInfrastructure` rather than a legacy smoke-test base. A module is not done until the intended backend paths agree.

## Step 7. Run the checks

From repository root:

```bash ci-run=false
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Wist.sln -c Release --no-build
```

For documentation smoke checks:

```bash ci-run=false
python3 .github/scripts/run-markdown-bash-blocks.py
```

These blocks are marked `ci-run=false` because the GitHub workflow already runs restore/build/test before the markdown smoke pass. The commands remain copyable for humans without making the documentation smoke step slow or recursive.

## Finished module checklist

Before considering a module complete, verify:

- the module has a clear alias;
- runtime export metadata is present;
- syntax belongs to lexer/parser, not raw source scanning;
- parser priority is intentional and tested;
- AST visitors self-filter;
- bytecode emission preserves stack discipline;
- the feature is available only when selected by dialect;
- selected backend modes agree on observable results;
- negative tests prove omitted syntax stays unavailable.

## Next

Read [Frontend Module](/write-modules/frontend-module) for the contract shape, then [Testing a Module](/write-modules/testing-module) for the broader test matrix.
