using ExceptionsManager;

namespace ParametersSetterModule;

public sealed class ParametersExternalBindingVisitor : IAstVisitor
{
    private static readonly ExtensibleEnum<AstNodeTag> _identifierNodeType =
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier");

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != _identifierNodeType)
            return;

        if (data.Node is BoundAstNode boundNode)
        {
            HandleBoundIdentifier(data, boundNode.Symbol);
            return;
        }

        Thrower.InvalidOpEx($"Identifier '{data.Node.Text}' is not declared as an external parameter.");
    }

    private static void HandleBoundIdentifier(BytecodeVisitorData data, Symbol symbol)
    {
        switch (symbol)
        {
            case ExternalVariableSymbol externalVariable:
                AddExternalLoad(data, externalVariable.Name, externalVariable.Slot, externalVariable.Type);
                return;
            case ExternalConstantSymbol externalConstant:
                AddExternalLoad(data, externalConstant.Name, externalConstant.Slot, externalConstant.Type);
                return;
            default:
                Thrower.InvalidOpEx($"Identifier '{symbol.Name}' is not declared as an external parameter.");
                return;
        }
    }

    private static void AddExternalLoad(BytecodeVisitorData data, string name, int slot, Type type)
    {
        var loadMethod = new AbstractMethodImpl(
            $"LoadValueOfParameter_{name}",
            (ir, _) => ir.Intrinsic("load_external", slot, type));

        data.Bytecode.Instructions.Add(new BytecodeInstruction(loadMethod));
    }
}
