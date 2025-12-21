using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using IlCodeGeneratorFactory;

namespace LabelsModule;

public class GotoVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Goto")) return;

        var method = new DynamicMethodConvertableWrapperImpl();
        method.Make($"Goto_!Intrinsic_{data.Node.Children[0].Text}", 0,
            (il, _) => il.IntrinsicNotImplemented(),
            _ => typeof(void)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}