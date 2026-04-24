using BasicCore.Binding;

namespace ConditionsModule.Visitors;

public sealed class IfExpressionVisitor : IAstVisitor
{
    private static readonly ExtensibleEnum<AstNodeTag> AdditionType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Addition");
    private static readonly ExtensibleEnum<AstNodeTag> AndType = ExtensibleEnum<AstNodeTag>.CreateOrGet("And");
    private static readonly ExtensibleEnum<AstNodeTag> DivisionType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Division");
    private static readonly ExtensibleEnum<AstNodeTag> EqualType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Equal");
    private static readonly ExtensibleEnum<AstNodeTag> FalseType = ExtensibleEnum<AstNodeTag>.CreateOrGet("False");
    private static readonly ExtensibleEnum<AstNodeTag> GreaterOrEqualType = ExtensibleEnum<AstNodeTag>.CreateOrGet("GreaterOrEqual");
    private static readonly ExtensibleEnum<AstNodeTag> GreaterType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Greater");
    private static readonly ExtensibleEnum<AstNodeTag> IfExpressionType = ExtensibleEnum<AstNodeTag>.CreateOrGet("IfExpression");
    private static readonly ExtensibleEnum<AstNodeTag> LessOrEqualType = ExtensibleEnum<AstNodeTag>.CreateOrGet("LessOrEqual");
    private static readonly ExtensibleEnum<AstNodeTag> LessType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Less");
    private static readonly ExtensibleEnum<AstNodeTag> MultiplicationType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Multiplication");
    private static readonly ExtensibleEnum<AstNodeTag> NotEqualType = ExtensibleEnum<AstNodeTag>.CreateOrGet("NotEqual");
    private static readonly ExtensibleEnum<AstNodeTag> NotType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Not");
    private static readonly ExtensibleEnum<AstNodeTag> NumberType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Number");
    private static readonly ExtensibleEnum<AstNodeTag> OrType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Or");
    private static readonly ExtensibleEnum<AstNodeTag> ScopeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope");
    private static readonly ExtensibleEnum<AstNodeTag> SubtractionType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Subtraction");
    private static readonly ExtensibleEnum<AstNodeTag> TrueType = ExtensibleEnum<AstNodeTag>.CreateOrGet("True");
    private static readonly ExtensibleEnum<AstNodeTag> VariableType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable");

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != IfExpressionType)
        {
            return;
        }

        ValidateIfExpression(data.Node);

        var elseLabel = Guid.NewGuid();
        var endLabel = Guid.NewGuid();

        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl(
                $"IfExpressionBranchFalse_{elseLabel}",
                (il, _) => il.JmpIfNot(elseLabel))));

        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl(
                $"IfExpressionJumpEnd_{endLabel}",
                (il, _) => il.Jmp(endLabel))));

        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl(
                $"IfExpressionElseLabel_{elseLabel}",
                (il, _) => il.SetLabel(elseLabel))));

        data.AstToBytecodeTranslator.Translate(data.Node.Children[2]);

        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            new AbstractMethodImpl(
                $"IfExpressionEndLabel_{endLabel}",
                (il, _) => il.SetLabel(endLabel))));
    }

    private static void ValidateIfExpression(AstNode node)
    {
        var conditionKind = InferValueKind(UnwrapExpression(node.Children[0]));
        if (conditionKind != IfExpressionValueKind.Boolean)
        {
            ThrowTypeMismatch("If expression condition must be bool.");
        }

        var thenKind = InferValueKind(UnwrapExpression(node.Children[1]));
        var elseKind = InferValueKind(UnwrapExpression(node.Children[2]));

        if ((thenKind == IfExpressionValueKind.Number && elseKind == IfExpressionValueKind.Number) ||
            (thenKind == IfExpressionValueKind.Boolean && elseKind == IfExpressionValueKind.Boolean))
        {
            return;
        }

        ThrowTypeMismatch("If expression branches must both resolve to number or both resolve to bool.");
    }

    private static AstNode UnwrapExpression(AstNode node)
    {
        if (node.NodeType == ScopeType && node.Children.Count == 1)
        {
            return node.Children[0];
        }

        return node;
    }

    private static IfExpressionValueKind InferValueKind(AstNode node)
    {
        if (node.NodeType == ScopeType)
        {
            if (node.Children.Count != 1)
            {
                return IfExpressionValueKind.Unknown;
            }

            return InferValueKind(node.Children[0]);
        }

        if (node.NodeType == NumberType)
        {
            return IfExpressionValueKind.Number;
        }

        if (node.NodeType == TrueType || node.NodeType == FalseType)
        {
            return IfExpressionValueKind.Boolean;
        }

        if (node.NodeType == VariableType && node is BoundAstNode boundNode)
        {
            return InferValueKind(boundNode.Symbol.Type);
        }

        if (node.NodeType == IfExpressionType)
        {
            var thenKind = InferValueKind(UnwrapExpression(node.Children[1]));
            var elseKind = InferValueKind(UnwrapExpression(node.Children[2]));

            return thenKind == elseKind ? thenKind : IfExpressionValueKind.Unknown;
        }

        if (node.NodeType == AdditionType ||
            node.NodeType == SubtractionType ||
            node.NodeType == MultiplicationType ||
            node.NodeType == DivisionType)
        {
            return InferBinaryKind(node, IfExpressionValueKind.Number, IfExpressionValueKind.Number);
        }

        if (node.NodeType == AndType || node.NodeType == OrType)
        {
            return InferBinaryKind(node, IfExpressionValueKind.Boolean, IfExpressionValueKind.Boolean);
        }

        if (node.NodeType == NotType)
        {
            return node.Children.Count == 1 && InferValueKind(node.Children[0]) == IfExpressionValueKind.Boolean
                ? IfExpressionValueKind.Boolean
                : IfExpressionValueKind.Unknown;
        }

        if (node.NodeType == GreaterType ||
            node.NodeType == LessType ||
            node.NodeType == GreaterOrEqualType ||
            node.NodeType == LessOrEqualType)
        {
            return InferBinaryKind(node, IfExpressionValueKind.Number, IfExpressionValueKind.Boolean);
        }

        if (node.NodeType == EqualType || node.NodeType == NotEqualType)
        {
            var leftKind = InferValueKind(node.Children[0]);
            var rightKind = InferValueKind(node.Children[1]);

            return leftKind != IfExpressionValueKind.Unknown &&
                   leftKind == rightKind &&
                   (leftKind == IfExpressionValueKind.Number || leftKind == IfExpressionValueKind.Boolean)
                ? IfExpressionValueKind.Boolean
                : IfExpressionValueKind.Unknown;
        }

        return IfExpressionValueKind.Unknown;
    }

    private static IfExpressionValueKind InferValueKind(Type type)
    {
        if (type == typeof(bool))
        {
            return IfExpressionValueKind.Boolean;
        }

        if (string.Equals(type.FullName, "NumbersModule.Core.RealNumberImpl", StringComparison.Ordinal) ||
            type == typeof(byte) ||
            type == typeof(sbyte) ||
            type == typeof(short) ||
            type == typeof(ushort) ||
            type == typeof(int) ||
            type == typeof(uint) ||
            type == typeof(long) ||
            type == typeof(ulong) ||
            type == typeof(float) ||
            type == typeof(double) ||
            type == typeof(decimal))
        {
            return IfExpressionValueKind.Number;
        }

        return IfExpressionValueKind.Unknown;
    }

    private static IfExpressionValueKind InferBinaryKind(
        AstNode node,
        IfExpressionValueKind operandKind,
        IfExpressionValueKind resultKind)
    {
        if (node.Children.Count != 2)
        {
            return IfExpressionValueKind.Unknown;
        }

        var leftKind = InferValueKind(node.Children[0]);
        var rightKind = InferValueKind(node.Children[1]);

        return leftKind == operandKind && rightKind == operandKind
            ? resultKind
            : IfExpressionValueKind.Unknown;
    }

    private static void ThrowTypeMismatch(string message)
    {
        Thrower.InvalidOpEx($"WST-TYPE-001: {message}");
    }

    private enum IfExpressionValueKind
    {
        Unknown,
        Number,
        Boolean
    }
}
