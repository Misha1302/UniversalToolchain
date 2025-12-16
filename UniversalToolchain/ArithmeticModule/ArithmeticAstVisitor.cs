// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using IlCodeGeneratorFactory;

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
        method.Make($"Op_{op}", 2, (il, context) =>
            {
                il.LdArgsAndCall(
                    context.Args[0].GetMethod(op switch
                        {
                            "+" => "Add",
                            "-" => "Sub",
                            "*" => "Mul",
                            "/" => "Div",
                            _ => Thrower.InvalidOpEx<string>()
                        }
                    ).NotNull(),
                    i =>
                    {
                        il.Ldarg(context.Args.Count - 1 - i);
                        return context.Args[context.Args.Count - 1 - i];
                    });
                il.Ret();
            },
            context => context.Stack[0]
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}