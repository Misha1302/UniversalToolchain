using System.Collections.Frozen;

namespace NativeMathModule;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
public class NativeArithmeticAstVisitor : IAstVisitor
{
    private static readonly FrozenDictionary<string, string> _opToMethodName = new Dictionary<string, string>
    {
        ["+"] = "Add",
        ["-"] = "Subtract",
        ["*"] = "Multiply",
        ["/"] = "Divide"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;

        if (nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeAddition") &&
            nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeSubtraction") &&
            nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeMultiplication") &&
            nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeDivision"))
            return;

        // Process both operands first to keep stack typing deterministic.
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

    internal static MethodInfo ResolveNativeUnaryMinusMethod(Type operandType)
    {
        if (operandType == typeof(decimal))
            return typeof(NativeArithmetic)
                .GetMethod(nameof(NativeArithmetic.NegateDecimal), BindingFlags.Static | BindingFlags.Public)
                .NotNull();

        try
        {
            return typeof(NativeArithmetic)
                .GetMethod(nameof(NativeArithmetic.Negate), BindingFlags.Static | BindingFlags.Public)
                .NotNull()
                .MakeGenericMethod(operandType);
        }
        catch (Exception)
        {
            return Thrower.NotSupported<MethodInfo>(
                $"Native unary minus does not support operand type '{operandType}'.");
        }
    }
}