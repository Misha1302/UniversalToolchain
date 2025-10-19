// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Globalization;
using BasicCore;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace BasicCodeTranslator;

public class NumberAstVisitor : IAstVisitor
{
    public void TryVisit(VisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Number")) return;

        var pushNumber = new DynamicMethodConvertableWrapperImpl();
        var numText = (data.Node.LexemeValue?.Text).NotNull();
        var num = double.Parse(numText, NumberStyles.Any);
        pushNumber.Make($"PushNumber_{num}", typeof(double), [null], il =>
        {
            il.Ldc_R8(num);
            il.Ret();
        });
        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            [],
            new SortedDictionary<float, IDynamicMethodConvertable> { { 0, pushNumber } })
        );
    }
}