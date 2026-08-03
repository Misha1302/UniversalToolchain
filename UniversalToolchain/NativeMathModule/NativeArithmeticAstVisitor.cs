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
        var resultType = ResolveBinaryNumericType(leftType, rightType);

        if (leftType != rightType)
        {
            return typeof(NativeArithmetic)
                .GetMethod(methodName + "Promoted", BindingFlags.Static | BindingFlags.NonPublic)
                .NotNull()
                .MakeGenericMethod(leftType, rightType, resultType);
        }

        if (resultType == typeof(decimal))
        {
            var decimalMethod = typeof(NativeArithmetic)
                .GetMethod(methodName + "Decimal", BindingFlags.Static | BindingFlags.Public)
                .NotNull();

            return decimalMethod;
        }

        return typeof(NativeArithmetic)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)
            .NotNull()
            .MakeGenericMethod(resultType);
    }

    internal static Type ResolveBinaryNumericType(Type leftType, Type rightType)
    {
        leftType = leftType.ArgNotNull();
        rightType = rightType.ArgNotNull();

        if (!IsSupportedNumericType(leftType) || !IsSupportedNumericType(rightType))
        {
            return Thrower.NotSupported<Type>(
                $"Native arithmetic supports only Int32, Int64, Single, Double, and Decimal operands; " +
                $"received '{leftType}' and '{rightType}'.");
        }

        if (leftType == rightType)
            return leftType;

        if (leftType == typeof(decimal) || rightType == typeof(decimal))
        {
            if (leftType == typeof(float) || leftType == typeof(double) ||
                rightType == typeof(float) || rightType == typeof(double))
            {
                return Thrower.NotSupported<Type>(
                    $"Native arithmetic does not implicitly combine Decimal with floating-point operands: " +
                    $"'{leftType}' and '{rightType}'.");
            }

            return typeof(decimal);
        }

        if (leftType == typeof(double) || rightType == typeof(double))
            return typeof(double);

        if (leftType == typeof(float) || rightType == typeof(float))
            return typeof(float);

        if (leftType == typeof(long) || rightType == typeof(long))
            return typeof(long);

        return typeof(int);
    }

    private static bool IsSupportedNumericType(Type type) =>
        type == typeof(int) ||
        type == typeof(long) ||
        type == typeof(float) ||
        type == typeof(double) ||
        type == typeof(decimal);

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