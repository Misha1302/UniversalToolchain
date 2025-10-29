// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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
        method.Make($"Goto_!Intrinsic_{data.Node.Children[0].Text}", typeof(void), [],
            (il, _) => il.IntrinsicNotImplemented()
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}