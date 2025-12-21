using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using IlCodeGeneratorFactory;

namespace LabelsModule;

public class LabelsVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Label")) return;

        var method = new DynamicMethodConvertableWrapperImpl();
        method.Make($"Label_!Intrinsic_{data.Node.Text}", 0,
            (il, _) => il.IntrinsicNotImplemented(),
            _ => typeof(void)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}