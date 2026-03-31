namespace NativeMathModule;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
public class NativeArithmeticAstVisitor : IAstVisitor
{
    private static readonly Dictionary<string, string> _opToMethodName = new()
    {
        ["+"] = "Add",
        ["-"] = "Subtract",
        ["*"] = "Multiply",
        ["/"] = "Divide"
    };

    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;

        if (nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeAddition") &&
            nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeSubtraction") &&
            nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeMultiplication") &&
            nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeDivision"))
            return;

        // Обрабатываем оба операнда
        data.AstToBytecodeTranslator.Translate(data.Node.Children[0]);
        data.AstToBytecodeTranslator.Translate(data.Node.Children[1]);

        var lexeme = data.Node.LexemeValue;
        Thrower.AssertAlways(lexeme != null, "Native arithmetic node must contain operation lexeme.");
        var opSymbol = lexeme.Text;
        var methodName = _opToMethodName[opSymbol];

        var method = new AbstractMethodImpl(
            $"NativeArithmetic_{methodName}",
            (il, context) =>
            {
                var resolvedMethod = ResolveNativeArithmeticMethod(methodName, context.Stack[^2], context.Stack[^1]);
                il.CallCSharp(resolvedMethod);
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    internal static MethodInfo ResolveNativeArithmeticMethod(string methodName, Type leftType, Type rightType)
    {
        Thrower.AssertAlways(leftType == rightType);

        if (leftType == typeof(decimal))
        {
            var decimalMethod = typeof(NativeArithmetic)
                .GetMethod(methodName + "Decimal", BindingFlags.Static | BindingFlags.Public)
                .NotNull();

            return decimalMethod;
        }

        return typeof(NativeArithmetic)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)
            .NotNull()
            .MakeGenericMethod(leftType);
    }
}
