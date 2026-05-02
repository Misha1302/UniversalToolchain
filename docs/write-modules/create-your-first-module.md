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

Create `UniversalToolchain/ArithmeticModule/Module/TextualAdditionModuleImpl.cs` with the complete file content below:

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

- `DialectModuleAlias` makes the module selectable from a dialect file.
- `DialectRuntimeExport` exposes it to manifest-backed runtime composition.
- `\bplus\b` prevents accidental matches inside words such as `surplus`.
- priority `110f` keeps `plus` above identifier-like lexemes when both are selected.
- parser priority `-30f` matches ordinary addition/subtraction precedence.

## Step 2.1. Do not create runtime JSON by hand

Older module registration flows required hand-maintained JSON metadata. Current module projects should not do that.

Runtime manifest JSON files are generated during build from attributes such as:

```csharp
[DialectModuleAlias("TextualAddition")]
[DialectRuntimeExport("FrontendModule", "TextualAddition")]
```

If you add a module inside an existing project that already emits manifests, such as `ArithmeticModule`, no extra JSON file is needed.

If you create a new standalone module project, enable manifest generation in that project's `.csproj`:

```xml
<PropertyGroup>
    <EmitDialectRuntimeManifest>true</EmitDialectRuntimeManifest>
</PropertyGroup>
```

The build then emits a file named like this in the output directory:

```text
YourModuleAssembly.dialect.runtime.json
```

The generated file is a build artifact. Do not commit a hand-written copy unless a future document explicitly says a specific scenario still needs one.

## Step 3. Add the parser node creator

Create `UniversalToolchain/ArithmeticModule/Creators/TextualAdditionOperationNodeCreator.cs` with the complete file content below:

```csharp
namespace ArithmeticModule.Creators;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class TextualAdditionOperationNodeCreator() : BinaryOperationBase("TextualAddition");
```

This transforms a flat token sequence like `2 plus 3` into a binary AST node with left and right operands.

The node type remains `TextualAddition`, not `Addition`, because this module owns a distinct syntax surface. The AST visitor maps that syntax to addition semantics explicitly.

## Step 4. Add the AST visitor

Create `UniversalToolchain/ArithmeticModule/Visitors/TextualAdditionAstVisitor.cs` with the complete file content below:

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

Create `UniversalToolchain/UniversalToolchain.Dialects.Tests/TextualAdditionModuleTests.cs` with the complete file content below:

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

- `2 plus 3` works when `TextualAddition` is selected.
- `2 plus 3 * 4` returns `14`, proving addition-level precedence.
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

## Verification

The files above are intentionally complete, not illustrative fragments. The implementation in this repository follows these exact steps:

- `TextualAdditionModuleImpl` registers the `plus` lexeme, parser creator, and AST visitor.
- `TextualAdditionOperationNodeCreator` creates a binary AST shape for the operator.
- `TextualAdditionAstVisitor` lowers the node to the existing `Add` operation.
- Runtime manifest metadata is generated from module/export attributes during build; this tutorial does not require a hand-written `.dialect.runtime.json` file.
- `TextualAdditionModuleTests` verifies positive execution, precedence, and missing-module rejection through both selected backend paths.

## Finished module checklist

Before considering a module complete, verify:

- the module has a clear alias;
- runtime export metadata is present;
- hand-written runtime JSON is not needed; manifest generation is enabled when the module lives in a new project;
- syntax belongs to lexer/parser, not raw source scanning;
- parser priority is intentional and tested;
- AST visitors self-filter;
- bytecode emission preserves stack discipline;
- the feature is available only when selected by dialect;
- selected backend modes agree on observable results;
- negative tests prove omitted syntax stays unavailable.

## Next

Read [Frontend Module](/write-modules/frontend-module) for the contract shape, then [Testing a Module](/write-modules/testing-module) for the broader test matrix.
