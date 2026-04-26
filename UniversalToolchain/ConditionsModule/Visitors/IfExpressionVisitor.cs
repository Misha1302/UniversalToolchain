namespace ConditionsModule.Visitors;

public sealed class IfExpressionVisitor : IAstVisitor
{
    private static readonly ExtensibleEnum<AstNodeTag> IfExpressionNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("IfExpression");

    public void TryVisit(BytecodeVisitorData data)
    {
        data = data.ArgNotNull();

        if (data.Node.NodeType != IfExpressionNodeType)
            return;

        if (data.Node.Children.Count != 3)
            Thrower.InvalidOpEx("IfExpression node must contain condition, true branch, and false branch.");

        var falseLabel = Guid.NewGuid();
        var endLabel = Guid.NewGuid();

        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_JmpIfNot_{falseLabel}",
            (il, _) => il.JmpIfNot(falseLabel))));

        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_Jmp_{endLabel}",
            (il, _) => il.Jmp(endLabel))));

        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_Label_{falseLabel}",
            (il, _) => il.SetLabel(falseLabel))));
        data.AstToBytecodeTranslator.Translate(data.Node.Children[2]);

        data.Bytecode.Instructions.Add(new BytecodeInstruction(new AbstractMethodImpl(
            $"IfExpression_Label_{endLabel}",
            (il, _) => il.SetLabel(endLabel))));
    }
}
