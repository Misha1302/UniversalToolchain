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