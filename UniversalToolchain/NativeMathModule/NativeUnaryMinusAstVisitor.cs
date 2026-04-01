namespace NativeMathModule;

public class NativeUnaryMinusAstVisitor : IAstVisitor
{
    private static readonly ExtensibleEnum<AstNodeTag> NativeUnaryMinus = ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeUnaryMinus");

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != NativeUnaryMinus)
            return;

        var pushZeroMethod = new AbstractMethodImpl(
            "NativeUnaryMinus_PushZero",
            (il, _) =>
            {
                il.Push(0);
            }
        );

        var subtractMethod = new AbstractMethodImpl(
            "NativeUnaryMinus_Subtract",
            (il, context) =>
            {
                var resolvedMethod = NativeArithmeticAstVisitor.ResolveNativeArithmeticMethod("Subtract", context.Stack[^2], context.Stack[^1]);
                il.CallCSharp(resolvedMethod);
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(pushZeroMethod));
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(subtractMethod));
    }
}
