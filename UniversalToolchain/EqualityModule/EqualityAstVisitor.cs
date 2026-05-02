namespace EqualityModule;

public class EqualityAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality"))
            return;

        var targetNode = data.Node.Children[0];
        var valueNode = data.Node.Children[1];

        data.AstToBytecodeTranslator.Translate(valueNode);
        data.AstToBytecodeTranslator.Translate(targetNode);

        var method = new AbstractMethodImpl(
            $"Set_{targetNode.LexemeValue?.Text}={valueNode.LexemeValue?.Text}",
            (il, context) => StoreAssignmentTarget(il, context, targetNode)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private static void StoreAssignmentTarget(
        IAbstractIR il,
        IAbstractMethodConvertable.Context context,
        AstNode targetNode)
    {
        if (context.Stack.Count == 0)
            Thrower.InvalidOpEx("Assignment requires a value on the stack.");

        if (targetNode is BoundAstNode boundNode)
        {
            if (boundNode.Symbol is ExternalConstantSymbol externalConstantSymbol)
                Thrower.InvalidOpEx($"External constant '{externalConstantSymbol.Name}' cannot be assigned.");

            if (boundNode.Symbol is ExternalVariableSymbol externalVariableSymbol)
            {
                il.StExternal(externalVariableSymbol.Slot, externalVariableSymbol.Type);
                return;
            }

            il.SetValueToLocal(boundNode.Symbol.StorageKey, context.Stack[^1]);
            return;
        }

        if (targetNode.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable"))
            Thrower.InvalidOpEx("Assignment target must be a variable.");

        il.SetValueToLocal(targetNode.Text, context.Stack[^1]);
    }
}