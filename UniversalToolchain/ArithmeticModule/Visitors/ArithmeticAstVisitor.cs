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
                var leftType = context.Stack[^1];
                var operationMethod = leftType.GetMethod(methodName);
                if (operationMethod == null)
                    Thrower.NotSupported<object>($"Operator '{op}' is not supported for type '{leftType.Name}'.");
                il.CallCSharp(operationMethod!);
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}