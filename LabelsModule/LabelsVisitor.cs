// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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
        method.Make($"Label_!Intrinsic_{data.Node.Text}", typeof(double), [],
            (il, _) => il.IntrinsicNotImplemented()
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}