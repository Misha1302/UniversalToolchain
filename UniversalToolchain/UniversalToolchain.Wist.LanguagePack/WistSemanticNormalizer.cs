using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BasicCore.Binding;
using BasicCore.Binding.Symbols;
using BasicCore.ParserWrapper;
using BasicCore.Semantics;
using InternalPreprocessorLexemesModule;
using NativeMathModule;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistSemanticNormalizer
{
    public static WistSemanticProgram Normalize(AstNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new WistSemanticProgram(NormalizeNode(root));
    }

    private static WistSemanticNode NormalizeNode(AstNode node)
    {
        if (PreprocessorLexemeContracts.TryReadDefineDirective(node, out var directive))
            return new WistDefineArgumentNode(directive.Name, directive.TypeName);

        return node.NodeType.GetName() switch
        {
            "Program" => new WistSemanticSequenceNode(
                WistSemanticSequenceKind.Program,
                node.Children.Select(NormalizeNode)),
            "Scope" => new WistSemanticSequenceNode(
                WistSemanticSequenceKind.Scope,
                node.Children.Select(NormalizeNode)),
            "Number" => new WistNumberNode(ParseRealNumber(node)),
            "NativeNumber" => new WistNativeNumberNode(
                WistNativeLiteralValue.FromRuntimeValue(NativeTypesModuleImpl.ParseNumber(RequiredText(node)))),
            "Variable" => NormalizeVariable(node),
            "Addition" or "TextualAddition" => Operation(WistSemanticOperations.Add, node),
            "Subtraction" => Operation(WistSemanticOperations.Subtract, node),
            "Multiplication" => Operation(WistSemanticOperations.Multiply, node),
            "Division" => Operation(WistSemanticOperations.Divide, node),
            "UnaryMinus" => Operation(WistSemanticOperations.UnaryMinus, node),
            "NativeAddition" => Operation(WistSemanticOperations.NativeAdd, node),
            "NativeSubtraction" => Operation(WistSemanticOperations.NativeSubtract, node),
            "NativeMultiplication" => Operation(WistSemanticOperations.NativeMultiply, node),
            "NativeDivision" => Operation(WistSemanticOperations.NativeDivide, node),
            "NativeUnaryMinus" => Operation(WistSemanticOperations.NativeUnaryMinus, node),
            "True" => new WistBooleanLiteralNode(true),
            "False" => new WistBooleanLiteralNode(false),
            "Not" => Operation(WistSemanticOperations.BooleanNot, node),
            "And" => ShortCircuit(node, isAnd: true),
            "Or" => ShortCircuit(node, isAnd: false),
            "Equal" => Operation(WistSemanticOperations.Equal, node),
            "NotEqual" => Operation(WistSemanticOperations.NotEqual, node),
            "Greater" => Operation(WistSemanticOperations.Greater, node),
            "Less" => Operation(WistSemanticOperations.Less, node),
            "GreaterOrEqual" => Operation(WistSemanticOperations.GreaterOrEqual, node),
            "LessOrEqual" => Operation(WistSemanticOperations.LessOrEqual, node),
            "Equality" => NormalizeAssignment(node),
            "If" or "Elif" => NormalizeConditional(node),
            "Else" => NormalizeElse(node),
            "IfExpression" => NormalizeIfExpression(node),
            "While" => NormalizeWhile(node),
            "For" => NormalizeFor(node),
            "Label" => new WistLabelNode(RequiredText(node)),
            "Goto" => NormalizeGoto(node),
            "FunctionCall" => NormalizeFunctionCall(node),
            "CSharpFunctionCall" => NormalizeCSharpCall(node),
            var unsupported => throw new InvalidOperationException(
                $"Unsupported Wist semantic construct '{unsupported}'. No syntax node may cross the semantic boundary.")
        };
    }

    private static WistSemanticOperationNode Operation(WistSemanticOperationId operation, AstNode node) =>
        new(operation, node.Children.Select(NormalizeNode));

    private static WistSymbolReferenceNode NormalizeVariable(AstNode node)
    {
        if (node is not BoundAstNode bound)
        {
            throw new InvalidOperationException(
                $"Unbound variable '{node.Text}' reached Wist semantic construction. " +
                "Variable binding must complete before the semantic artifact is created.");
        }

        var symbol = bound.Symbol switch
        {
            LocalVariableSymbol local => new WistSemanticSymbolId(
                WistSemanticSymbolKind.Local,
                local.Name,
                local.StorageKey,
                WistSemanticTypeId.FromType(local.Type)),
            ExternalVariableSymbol external => new WistSemanticSymbolId(
                WistSemanticSymbolKind.ExternalVariable,
                external.Name,
                external.StorageKey,
                WistSemanticTypeId.FromType(external.Type),
                external.Slot),
            ExternalConstantSymbol external => new WistSemanticSymbolId(
                WistSemanticSymbolKind.ExternalConstant,
                external.Name,
                external.StorageKey,
                WistSemanticTypeId.FromType(external.Type),
                external.Slot),
            _ => throw new InvalidOperationException(
                $"Unsupported bound symbol type '{bound.Symbol.GetType().FullName}' for variable '{bound.Symbol.Name}'.")
        };

        return new WistSymbolReferenceNode(
            symbol,
            node.HasLocalSemanticTag(AssignmentSemanticContractIds.WriteTarget));
    }

    private static WistAssignmentNode NormalizeAssignment(AstNode node)
    {
        if (node.Children.Count != 2)
            throw new InvalidOperationException("Wist assignment must contain exactly one target and one value.");

        if (NormalizeNode(node.Children[0]) is not WistSymbolReferenceNode target)
            throw new InvalidOperationException("Wist assignment target must resolve to a bound symbol reference.");

        return new WistAssignmentNode(target, NormalizeNode(node.Children[1]));
    }

    private static WistShortCircuitNode ShortCircuit(AstNode node, bool isAnd)
    {
        if (node.Children.Count != 2)
            throw new InvalidOperationException($"Boolean {(isAnd ? "and" : "or")} requires exactly two operands.");

        return new WistShortCircuitNode(
            isAnd,
            NormalizeNode(node.Children[0]),
            NormalizeNode(node.Children[1]),
            CreateDeterministicControlFlowLabel(node, "conditions", "false"),
            CreateDeterministicControlFlowLabel(node, "conditions", "true"),
            CreateDeterministicControlFlowLabel(node, "conditions", "end"));
    }

    private static WistConditionalBranchNode NormalizeConditional(AstNode node)
    {
        if (node.Children.Count < 2)
            throw new InvalidOperationException("Wist conditional branch requires a condition and body.");

        return new WistConditionalBranchNode(
            NormalizeNode(node.Children[0]),
            NormalizeNode(node.Children[1]),
            node.Children.Skip(2).Select(NormalizeNode).ToArray(),
            CreateDeterministicControlFlowLabel(node, "conditions", "else"),
            CreateDeterministicControlFlowLabel(node, "conditions", "end"));
    }

    private static WistElseNode NormalizeElse(AstNode node)
    {
        if (node.Children.Count != 1)
            throw new InvalidOperationException("Wist else branch must contain exactly one body node.");
        return new WistElseNode(NormalizeNode(node.Children[0]));
    }

    private static WistIfExpressionNode NormalizeIfExpression(AstNode node)
    {
        if (node.Children.Count != 3)
            throw new InvalidOperationException("IfExpression node must contain condition, true branch, and false branch.");
        return new WistIfExpressionNode(
            NormalizeNode(node.Children[0]),
            NormalizeNode(node.Children[1]),
            NormalizeNode(node.Children[2]));
    }

    private static WistWhileNode NormalizeWhile(AstNode node)
    {
        if (node.Children.Count != 2)
            throw new InvalidOperationException("Wist while loop requires a condition and body.");
        return new WistWhileNode(
            NormalizeNode(node.Children[0]),
            NormalizeNode(node.Children[1]),
            CreateDeterministicControlFlowLabel(node, "loops", "while-start"),
            CreateDeterministicControlFlowLabel(node, "loops", "while-end"));
    }

    private static WistForNode NormalizeFor(AstNode node)
    {
        if (node.Children.Count != 4)
            throw new InvalidOperationException("Wist for loop requires initialization, condition, step, and body.");
        return new WistForNode(
            NormalizeNode(node.Children[0]),
            NormalizeNode(node.Children[1]),
            NormalizeNode(node.Children[2]),
            NormalizeNode(node.Children[3]),
            CreateDeterministicControlFlowLabel(node, "loops", "for-start"),
            CreateDeterministicControlFlowLabel(node, "loops", "for-end"));
    }

    private static WistGotoNode NormalizeGoto(AstNode node)
    {
        if (node.Children.Count == 0)
            throw new InvalidOperationException("Wist goto requires a label target.");
        return new WistGotoNode(RequiredText(node.Children[0]));
    }

    private static WistFunctionCallNode NormalizeFunctionCall(AstNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Text))
            throw new InvalidOperationException("Function call node must contain a function identifier.");
        var argumentsScope = node.Children.FirstOrDefault();
        if (argumentsScope?.NodeType.GetName() != "Scope")
            throw new InvalidOperationException($"Function call '{node.Text}' must contain an argument scope.");
        return new WistFunctionCallNode(node.Text, NormalizeCallArguments(node.Text, argumentsScope));
    }

    private static WistCSharpCallNode NormalizeCSharpCall(AstNode node)
    {
        var fullName = RequiredText(node);
        var argumentsScope = node.Children.FirstOrDefault();
        if (argumentsScope?.NodeType.GetName() != "Scope")
            throw new InvalidOperationException($"C# function call '{fullName}' must contain an argument scope.");
        return new WistCSharpCallNode(fullName, NormalizeCallArguments(fullName, argumentsScope));
    }

    private static IReadOnlyList<WistSemanticNode> NormalizeCallArguments(string functionName, AstNode argumentsScope)
    {
        if (argumentsScope.Children.Count == 0)
            return [];

        var arguments = new List<WistSemanticNode>();
        var currentSegment = new List<AstNode>();
        var previousWasSeparator = false;
        foreach (var child in argumentsScope.Children)
        {
            if (child.NodeType.GetName() == "Comma")
            {
                AddArgument(functionName, arguments, currentSegment);
                currentSegment.Clear();
                previousWasSeparator = true;
                continue;
            }

            currentSegment.Add(child);
            previousWasSeparator = false;
        }

        if (previousWasSeparator)
            throw new InvalidOperationException($"Function call '{functionName}' contains an empty argument.");
        AddArgument(functionName, arguments, currentSegment);
        return arguments;
    }

    private static void AddArgument(
        string functionName,
        ICollection<WistSemanticNode> arguments,
        IReadOnlyList<AstNode> segment)
    {
        if (segment.Count == 0)
            throw new InvalidOperationException($"Function call '{functionName}' contains an empty argument.");
        if (segment.Count != 1)
            throw new InvalidOperationException($"Function call '{functionName}' argument is not a single expression node.");
        arguments.Add(NormalizeNode(segment[0]));
    }

    private static double ParseRealNumber(AstNode node)
    {
        var text = RequiredText(node).Replace("_", "", StringComparison.Ordinal);
        return double.Parse(text, NumberStyles.Any, CultureInfo.CurrentCulture);
    }

    private static string RequiredText(AstNode node) =>
        node.LexemeValue?.Text ?? node.Text ?? throw new InvalidOperationException(
            $"Wist syntax node '{node.NodeType}' does not contain required text.");

    private static Guid CreateDeterministicControlFlowLabel(AstNode node, string prefix, string role)
    {
        var path = new Stack<int>();
        for (var current = node; current.Parent is { } parent; current = parent)
        {
            var index = 0;
            foreach (var child in parent.Children)
            {
                if (ReferenceEquals(child, current))
                    break;
                index++;
            }
            path.Push(index);
        }

        var identity = $"{prefix}:{string.Join('.', path)}:{node.LexemeValue?.StartIndex ?? -1}:{node.NodeType}:{node.Text}:{role}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return new Guid(hash.AsSpan(0, 16));
    }
}
