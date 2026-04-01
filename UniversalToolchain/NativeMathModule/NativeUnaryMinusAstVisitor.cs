namespace NativeMathModule;

public class NativeUnaryMinusAstVisitor : IAstVisitor
{
    private static readonly ExtensibleEnum<AstNodeTag> NativeUnaryMinus = ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeUnaryMinus");

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != NativeUnaryMinus)
            return;

        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);

        var negateMethod = new AbstractMethodImpl(
            "NativeUnaryMinus_Negate",
            (il, context) =>
            {
                var operandType = context.Stack[^1];
                var resolvedMethod = NativeArithmeticAstVisitor.ResolveNativeUnaryMinusMethod(operandType);
                il.CallCSharp(resolvedMethod);
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(negateMethod));
    }
}
