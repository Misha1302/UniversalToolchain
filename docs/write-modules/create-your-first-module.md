---
title: Create Your First Module
description: Build a small frontend module from syntax idea to tested dialect behavior.
---

# Create Your First Module

This tutorial builds a real, intentionally small frontend module: `TextualAddition`.

The module adds a textual addition operator:

```wist
2 plus 3
```

Expected result:

```text
5
```

The feature is small on purpose. It touches the full module authoring path without requiring a new backend or intrinsic.

## What you will build

| Concern | Value |
|---|---|
| Module alias | `TextualAddition` |
| Runtime export | `FrontendModule/TextualAddition` |
| New syntax | `<expression> plus <expression>` |
| Parser precedence | same as normal addition |
| Runtime behavior | reuse existing `Add` operation on the left operand type |
| Required related modules | `Whitespaces`, `Numbers`, `Scopes`, `Arithmetic` |
| Backend expectation | compiler and interpreter parity |

This module is a good first example because it adds syntax but reuses existing arithmetic semantics.

## Files created

The implementation lives in the existing `ArithmeticModule` project because this tutorial feature is semantically arithmetic-related:

| Concern | File |
|---|---|
| Module entry point | `UniversalToolchain/ArithmeticModule/Module/TextualAdditionModuleImpl.cs` |
| Parser node creator | `UniversalToolchain/ArithmeticModule/Creators/TextualAdditionOperationNodeCreator.cs` |
| AST visitor | `UniversalToolchain/ArithmeticModule/Visitors/TextualAdditionAstVisitor.cs` |
| Tests | `UniversalToolchain/UniversalToolchain.Dialects.Tests/TextualAdditionModuleTests.cs` |

A production feature could live in its own project. For a first tutorial, keeping the example near the existing arithmetic code makes ownership and reuse easier to understand.

## Step 1. Define the feature boundary

`TextualAddition` owns only one source-level feature: the keyword-like operator `plus`.

It does not own numeric literals, scopes, multiplication, or backend selection. Those are provided by existing modules.

This boundary matters because a module should not quietly duplicate unrelated language behavior.

## Step 2. Add the module entry point

Create `UniversalToolchain/ArithmeticModule/Module/TextualAdditionModuleImpl.cs`:

```csharp
namespace ArithmeticModule.Module;

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

- `DialectModuleAlias("TextualAddition")` makes the module selectable from a dialect file.
- `DialectRuntimeExport("FrontendModule", "TextualAddition")` exposes the runtime component to manifest-backed composition.
- `AutoRegisterService` keeps service registration consistent with the rest of the module system.
- The lexeme uses `\bplus\b` so `surplus` does not accidentally become `sur` + `plus`.
- Lexeme priority is higher than the identifier pattern, so `plus` is recognized as module-owned syntax when this module is selected.
- Parser priority `-30f` matches normal addition/subtraction precedence.

## Step 3. Add the parser node creator

Create `UniversalToolchain/ArithmeticModule/Creators/TextualAdditionOperationNodeCreator.cs`:

```csharp
namespace ArithmeticModule.Creators;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class TextualAdditionOperationNodeCreator() : BinaryOperationBase("TextualAddition");
```

This reuses `BinaryOperationBase`, the same shape used by ordinary arithmetic binary operators.

The parser creator turns this flat token sequence:

```text
2 plus 3
```

into a binary AST node where `plus` has left and right operands.

The node type is `TextualAddition`, not `Addition`, because the module owns a distinct syntax surface. The visitor will map that syntax to addition semantics explicitly.

## Step 4. Add the AST visitor

Create `UniversalToolchain/ArithmeticModule/Visitors/TextualAdditionAstVisitor.cs`:

```csharp
namespace ArithmeticModule.Visitors;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class TextualAdditionAstVisitor : IAstVisitor
{
    private static readonly ExtensibleEnum<AstNodeTag> _nodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("TextualAddition");

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != _nodeType)
            return;

        foreach (var child in data.Node.Children)
            data.AstToBytecodeTranslator.Translate(child);

        var method = new AbstractMethodImpl(
            "Op_plus",
            (il, context) => il.CallCSharp(context.Stack[^1].GetMethod("Add").NotNull())
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}
```

The visitor must self-filter. It should emit bytecode only for `TextualAddition` nodes.

The child translation order matters:

```text
left operand → right operand → Add operation
```

That keeps stack behavior consistent with the existing arithmetic visitor.

## Step 5. Enable the module in a dialect

A dialect must select the module explicitly:

```text
dialect TextualAdditionDemo
use Whitespaces,Numbers,Scopes,Arithmetic,TextualAddition
backend compiler,interpreter
```

`TextualAddition` is not a global language feature. Without this `use` entry, the `plus` syntax should be unavailable.

## Step 6. Add positive and parity tests

Create `UniversalToolchain/UniversalToolchain.Dialects.Tests/TextualAdditionModuleTests.cs`:

```csharp
using UniversalToolchain.Modules.Tests;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public sealed class TextualAdditionModuleTests
{
    private const string TextualAdditionDialect = """
                                                 dialect TextualAdditionDemo
                                                 use Whitespaces,Numbers,Scopes,Arithmetic,TextualAddition
                                                 backend compiler,interpreter
                                                 """;

    private const string ArithmeticOnlyDialect = """
                                                dialect ArithmeticOnlyDemo
                                                use Whitespaces,Numbers,Scopes,Arithmetic
                                                backend compiler,interpreter
                                                """;

    [Test]
    public void TextualAddition_Module_ExecutesPlusKeyword()
    {
        var result = DialectTestHostInfrastructure.RunInBothBackends(TextualAdditionDialect, "2 plus 3");

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(5.0d).Within(1e-9));
    }

    [Test]
    public void TextualAddition_Module_UsesAdditionPrecedence()
    {
        var result = DialectTestHostInfrastructure.RunInBothBackends(TextualAdditionDialect, "2 plus 3 * 4");

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(14.0d).Within(1e-9));
    }

    [Test]
    public void TextualAddition_Syntax_IsUnavailable_WhenModuleIsNotSelected()
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(ArithmeticOnlyDialect, "2 plus 3");

        Assert.Multiple(() =>
        {
            Assert.That(compilerResult.IsSuccess, Is.False, "Compiler path must reject syntax owned by an unselected module.");
            Assert.That(interpreterResult.IsSuccess, Is.False, "Interpreter path must reject syntax owned by an unselected module.");
            Assert.That(compilerResult.Exception, Is.Not.Null);
            Assert.That(interpreterResult.Exception, Is.Not.Null);
        });
    }
}
```

These tests prove three things:

1. selected module syntax works;
2. `plus` uses addition precedence;
3. unselected module syntax is rejected in both backend paths.

## Step 7. Run the checks

From repository root:

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
dotnet test UniversalToolchain/Wist.sln -c Release --no-build
```

For documentation smoke checks:

```bash
python3 .github/scripts/run-markdown-bash-blocks.py
```

The GitHub workflow runs the same broad restore/build/test path, so keep the tutorial code blocks copyable and consistent with CI.

## What you have learned

This module demonstrates the minimum useful vertical slice:

```text
module metadata
  → lexeme registration
  → parser node creator
  → AST visitor
  → bytecode emission
  → dialect selection
  → backend parity tests
```

It does not demonstrate new backend behavior. That is the next level: a feature that emits new bytecode/AIR or requires interpreter/compiler support.

## Finished module checklist

Before considering a module complete, verify:

- the module has a clear alias;
- runtime export metadata is present;
- syntax belongs to lexer/parser, not raw source scanning;
- parser priority is intentional and tested;
- AST visitors self-filter;
- emitted bytecode preserves stack discipline;
- the feature is available only when selected by dialect;
- selected backend modes agree on observable results;
- negative tests prove omitted syntax stays unavailable.

## Next

Read [Frontend Module](/write-modules/frontend-module) for the contract shape, then [Testing a Module](/write-modules/testing-module) for the broader test matrix.
