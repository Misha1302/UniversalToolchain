namespace ArithmeticModule.Visitors;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class ArithmeticAstVisitor : IAstVisitor
{
    private static readonly Dictionary<string, string> _opToName = new()
    {
        ["+"] = "Add",
        ["-"] = "Sub",
        ["*"] = "Mul",
        ["/"] = "Div"
    };

    public void TryVisit(BytecodeVisitorData data)
    {
        if (ArithmeticModuleImpl.Ops.All(op => data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet(op)))
            return;

        foreach (var child in data.Node.Children)
            data.AstToBytecodeTranslator.Translate(child);

        var op = (data.Node.LexemeValue?.Text).NotNull();
        var methodName = _opToName[op];

        var method = new AbstractMethodImpl(
            $"Op_{op}",
            (il, context) =>
            {
                var resolvedMethod = ResolveArithmeticMethod(methodName, context.Stack[^2], context.Stack[^1]);
                il.CallCSharp(resolvedMethod);
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private static System.Reflection.MethodInfo ResolveArithmeticMethod(string methodName, Type leftType, Type rightType)
    {
        if (leftType == rightType)
        {
            var method = rightType.GetMethod(
                methodName,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                [leftType, rightType]);
            if (method != null)
                return method;
        }

        return typeof(RuntimeArithmeticFallback)
            .GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .NotNull();
    }

    private static class RuntimeArithmeticFallback
    {
        public static double Add(object left, object right) => ToDouble(left) + ToDouble(right);

        public static double Sub(object left, object right) => ToDouble(left) - ToDouble(right);

        public static double Mul(object left, object right) => ToDouble(left) * ToDouble(right);

        public static double Div(object left, object right) => ToDouble(left) / ToDouble(right);

        private static double ToDouble(object value)
        {
            if (value is IConvertible convertible)
                return convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture);

            var getValue = value.GetType().GetMethod(
                "GetValue",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
                Type.EmptyTypes);
            if (getValue?.Invoke(value, []) is IConvertible extracted)
                return extracted.ToDouble(System.Globalization.CultureInfo.InvariantCulture);

            return Thrower.InvalidOpEx<double>(
                $"Value of type '{value.GetType().FullName}' cannot be used as a numeric arithmetic operand.");
        }
    }
}
