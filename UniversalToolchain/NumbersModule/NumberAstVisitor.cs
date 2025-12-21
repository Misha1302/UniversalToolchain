using System.Globalization;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace NumbersModule;

public class NumberAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Number")) return;

        var method = new DynamicMethodConvertableWrapperImpl();
        var numText = (data.Node.LexemeValue?.Text).NotNull();
        var num = double.Parse(numText, NumberStyles.Any);
        method.Make($"PushNumber_{num}", 0, (il, _) =>
        {
            il.Ldc_R8(num);
            il.Newobj(typeof(RealNumberImpl).GetConstructor([typeof(double)]));
            il.Ret();
        }, _ => typeof(RealNumberImpl));
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}