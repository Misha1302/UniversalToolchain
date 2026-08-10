namespace ArithmeticModule.Visitors;

[AutoRegisterService]
public class UnaryMinusAstVisitor : IAstVisitor
{
    private static readonly ExtensibleEnum<AstNodeTag> _unaryMinusNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("UnaryMinus");
    private static readonly ExtensibleEnum<AstNodeTag> _numberNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Number");

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != _unaryMinusNodeType)
            return;

        var zeroNode = new AstNode(_numberNodeType, new LexemeValue("0", null, -1, null), []);
        data.AstToBytecodeTranslator.Translate(zeroNode);

        var operand = data.Node.Children.FirstOrDefault();
        if (operand == null)
            return;

        data.AstToBytecodeTranslator.Translate(operand);

        var subMethod = new AbstractMethodImpl(
            "Op_-",
            (il, context) => il.CallCSharp(context.Stack[^1].GetMethod("Sub").NotNull())
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(subMethod));
    }
}