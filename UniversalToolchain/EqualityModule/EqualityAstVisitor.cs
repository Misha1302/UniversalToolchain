// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using IlCodeGeneratorFactory;

namespace EqualityModule;

public class EqualityAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality"))
            return;

        data.BytecodeTranslator.Translate(data.Node.Children[1]); // value
        data.BytecodeTranslator.Translate(data.Node.Children[0]); // ref

        var method = new DynamicMethodConvertableWrapperImpl();
        method.Make(
            $"Set_{data.Node.Children[0].LexemeValue?.Text}={data.Node.Children[1].LexemeValue?.Text}",
            2,
            (il, context) =>
            {
                il.LdArgsAndCall(
                    context.Args[0].GetMethod("SetValue", BindingFlags.Instance | BindingFlags.Public).NotNull(),
                    i =>
                    {
                        il.Ldarg(i);
                        return context.Args[1 - i];
                    }
                );
                il.Ret();
            }, _ => typeof(void)
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}