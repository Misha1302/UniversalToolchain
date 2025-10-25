// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace ArithmeticModule;

public class ArithmeticAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (ArithmeticModuleImpl.Ops.All(op => data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet(op)))
            return;

        foreach (var child in data.Node.Children)
            data.BytecodeTranslator.Translate(child);

        var method = new DynamicMethodConvertableWrapperImpl();
        var op = (data.Node.LexemeValue?.Text).NotNull();
        method.Make($"Op_{op}", typeof(double), [typeof(double), typeof(double)], il =>
        {
            il.Ldarg(0);
            il.Ldarg(1);
            if (op == "+") il.Add();
            else if (op == "-") il.Sub();
            else if (op == "*") il.Mul();
            else if (op == "/") il.Div(false);
            il.Ret();
        });
        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            [],
            new LevelCollection<float, IDynamicMethodConvertable> { { 0, method } })
        );
    }
}