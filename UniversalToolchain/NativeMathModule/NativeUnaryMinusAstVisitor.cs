namespace NativeMathModule;

public class NativeUnaryMinusAstVisitor : IAstVisitor
{
    private static readonly ExtensibleEnum<AstNodeTag> NativeUnaryMinus = ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeUnaryMinus");

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != NativeUnaryMinus)
            return;

        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);

        var pushZeroMethod = new AbstractMethodImpl(
            "NativeUnaryMinus_PushZero",
            (il, context) =>
            {
                var operandType = context.Stack[^1];

                if (operandType == typeof(decimal))
                    il.Push(0m);
                else if (operandType == typeof(double))
                    il.Push(0d);
                else if (operandType == typeof(float))
                    il.Push(0f);
                else if (operandType == typeof(long))
                    il.Push(0L);
                else if (operandType == typeof(int))
                    il.Push(0);
                else
                    Thrower.NotSupported<object>($"Unsupported native unary minus operand type '{operandType}'.");
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
        data.Bytecode.Instructions.Add(new BytecodeInstruction(subtractMethod));
    }
}
