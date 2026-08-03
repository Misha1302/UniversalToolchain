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
        var operandType = ResolveBinaryNumericType(leftType, rightType);

        if (leftType != operandType || rightType != operandType)
        {
            return typeof(NativeArithmetic)
                .GetMethod(methodName + "Promoted", BindingFlags.Static | BindingFlags.Public)
                .NotNull()
                .MakeGenericMethod(leftType, rightType, operandType);
        }

        if (operandType == typeof(decimal))
        {
            return typeof(NativeArithmetic)
                .GetMethod(methodName + "Decimal", BindingFlags.Static | BindingFlags.Public)
                .NotNull();
        }

        return typeof(NativeArithmetic)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)
            .NotNull()
            .MakeGenericMethod(operandType);
    }

    internal static Type ResolveBinaryNumericType(Type leftType, Type rightType)
    {
        var normalizedLeft = NormalizeSmallIntegral(leftType);
        var normalizedRight = NormalizeSmallIntegral(rightType);

        if (normalizedLeft == normalizedRight)
            return normalizedLeft;

        if (normalizedLeft == typeof(decimal) || normalizedRight == typeof(decimal))
        {
            if (IsBinaryFloatingPoint(normalizedLeft) || IsBinaryFloatingPoint(normalizedRight))
            {
                return Thrower.NotSupported<Type>(
                    $"Native arithmetic does not implicitly combine decimal with binary floating-point type(s) '{leftType}' and '{rightType}'.");
            }

            return typeof(decimal);
        }

        if (normalizedLeft == typeof(double) || normalizedRight == typeof(double))
            return typeof(double);
        if (normalizedLeft == typeof(float) || normalizedRight == typeof(float))
            return typeof(float);
        if (normalizedLeft == typeof(Half) || normalizedRight == typeof(Half))
            return typeof(float);

        if (normalizedLeft == typeof(nint) || normalizedLeft == typeof(nuint) ||
            normalizedRight == typeof(nint) || normalizedRight == typeof(nuint))
        {
            return Thrower.NotSupported<Type>(
                $"Native arithmetic requires identical native-integer operand types, but received '{leftType}' and '{rightType}'.");
        }

        if (normalizedLeft == typeof(ulong) || normalizedRight == typeof(ulong))
        {
            if (IsSignedIntegral(normalizedLeft) || IsSignedIntegral(normalizedRight))
            {
                return Thrower.NotSupported<Type>(
                    $"Native arithmetic does not implicitly combine signed integral and UInt64 operands '{leftType}' and '{rightType}'.");
            }

            return typeof(ulong);
        }

        if (normalizedLeft == typeof(long) || normalizedRight == typeof(long))
            return typeof(long);

        if (normalizedLeft == typeof(uint) || normalizedRight == typeof(uint))
            return normalizedLeft == typeof(int) || normalizedRight == typeof(int) ? typeof(long) : typeof(uint);

        if (normalizedLeft == typeof(int) && normalizedRight == typeof(int))
            return typeof(int);

        return Thrower.NotSupported<Type>(
            $"Native arithmetic does not support binary numeric promotion for '{leftType}' and '{rightType}'.");
    }

    private static Type NormalizeSmallIntegral(Type type) =>
        type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(char)
            ? typeof(int)
            : type;

    private static bool IsBinaryFloatingPoint(Type type) =>
        type == typeof(Half) || type == typeof(float) || type == typeof(double);

    private static bool IsSignedIntegral(Type type) =>
        type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long) || type == typeof(nint);

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
