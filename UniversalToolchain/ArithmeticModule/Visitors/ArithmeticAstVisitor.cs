using System.Collections.Frozen;

namespace ArithmeticModule.Visitors;

[AutoRegisterService]
public class ArithmeticAstVisitor : IAstVisitor
{
    private static readonly FrozenDictionary<string, string> _opToName = new Dictionary<string, string>
    {
        ["-"] = "Sub",
        ["*"] = "Mul",
        ["/"] = "Div"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly ExtensibleEnum<AstNodeTag> LegacyAddition =
        ExtensibleEnum<AstNodeTag>.CreateOrGet("Addition");

    public void TryVisit(BytecodeVisitorData data)
    {
        // Addition is lowered exclusively from the canonical semantic Add node.
        if (data.Node.NodeType == LegacyAddition)
            return;
        if (ArithmeticModuleImpl.Ops.All(op => data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet(op)))
            return;

        foreach (var child in data.Node.Children)
            data.AstToBytecodeTranslator.Translate(child);

        var op = (data.Node.LexemeValue?.Text).NotNull();
        var methodName = _opToName[op];

        var method = new AbstractMethodImpl(
            $"Op_{op}",
            (il, context) => il.CallCSharp(context.Stack[^1].GetMethod(methodName).NotNull())
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}
