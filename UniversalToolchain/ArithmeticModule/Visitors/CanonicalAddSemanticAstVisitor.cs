namespace ArithmeticModule.Visitors;

/// <summary>
/// Canonical arithmetic lowering boundary. The visitor consumes only the semantic Add identity;
/// it has no dependency on source spelling, syntax node creators, or frontend modules.
/// </summary>
public static class ArithmeticSemanticLowering
{
    public static ExtensibleEnum<AstNodeTag> AddNodeType { get; } =
        ExtensibleEnum<AstNodeTag>.CreateOrGet("WistSemantic.Add");
}

public sealed class CanonicalAddSemanticAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Node.NodeType != ArithmeticSemanticLowering.AddNodeType)
            return;

        foreach (var child in data.Node.Children)
            data.AstToBytecodeTranslator.Translate(child);

        var method = new AbstractMethodImpl(
            "Op_Add",
            static (il, context) => il.CallCSharp(context.Stack[^1].GetMethod("Add").NotNull()));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}
