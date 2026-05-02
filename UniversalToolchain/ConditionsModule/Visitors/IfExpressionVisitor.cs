namespace ConditionsModule.Visitors;

public sealed class IfExpressionVisitor : IAstVisitor
{
    private static readonly ExtensibleEnum<AstNodeTag> IfExpressionNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("IfExpression");
    private int _ifExpressionSequence;

    public void TryVisit(BytecodeVisitorData data)
    {
        data = data.ArgNotNull();

        if (data.Node.NodeType != IfExpressionNodeType)
            return;

        if (data.Node.Children.Count != 3)
            Thrower.InvalidOpEx("IfExpression node must contain condition, true branch, and false branch.");

        var sequence = _ifExpressionSequence++;
        var falseLabel = CreateDeterministicLabel(sequence, 1);
        var endLabel = CreateDeterministicLabel(sequence, 2);
        var resultLocalName = $"__if_expression_result_{sequence}";
        var resultType = default(Type);

        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_RequireBooleanCondition_{sequence}",
            (_, context) => RequireBooleanCondition(context))));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_JmpIfNot_{falseLabel}",
            (il, _) => il.JmpIfNot(falseLabel))));

        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_StoreTrueResult_{resultLocalName}",
            (il, context) => StoreBranchResult(il, context, resultLocalName, ref resultType))));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_Jmp_{endLabel}",
            (il, _) => il.Jmp(endLabel))));

        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_Label_{falseLabel}",
            (il, _) => il.SetLabel(falseLabel))));
        data.AstToBytecodeTranslator.Translate(data.Node.Children[2]);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_StoreFalseResult_{resultLocalName}",
            (il, context) => StoreBranchResult(il, context, resultLocalName, ref resultType))));

        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_Label_{endLabel}",
            (il, _) => il.SetLabel(endLabel))));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_LoadResult_{resultLocalName}",
            (il, _) => il.LdLoc(resultLocalName, resultType.NotNull()))));
    }

    private static Guid CreateDeterministicLabel(int sequence, byte marker)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(sequence).CopyTo(bytes, 0);
        bytes[15] = marker;
        return new Guid(bytes);
    }

    private static void RequireBooleanCondition(IAbstractMethodConvertable.Context context)
    {
        if (context.Stack.Count == 0)
            Thrower.InvalidOpEx("IfExpression condition must leave a value on the stack.");

        var conditionType = context.Stack[^1];
        if (conditionType != typeof(bool))
            Thrower.InvalidOpEx(
                $"IfExpression condition must be boolean. Actual type: '{conditionType.FullName}'.");
    }

    private static void StoreBranchResult(
        IAbstractIR il,
        IAbstractMethodConvertable.Context context,
        string resultLocalName,
        ref Type? resultType)
    {
        if (context.Stack.Count == 0)
            Thrower.InvalidOpEx("IfExpression branch must leave a value on the stack.");

        var branchType = context.Stack[^1];
        if (resultType == null)
            resultType = branchType;
        else if (resultType != branchType)
            Thrower.InvalidOpEx(
                $"IfExpression branch types must match. Expected '{resultType.FullName}', actual '{branchType.FullName}'.");

        il.SetValueToLocal(resultLocalName, branchType);
    }
}