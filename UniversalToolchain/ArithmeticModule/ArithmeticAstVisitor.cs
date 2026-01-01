using AbstractIrExtensions;
using BasicCore;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace ArithmeticModule;

[AutoRegisterService]
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
            (il, context) => il.CallCSharp(context.Stack[^1].GetMethod(methodName).NotNull())
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}