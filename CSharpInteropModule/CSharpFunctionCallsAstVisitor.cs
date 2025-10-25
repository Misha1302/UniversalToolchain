// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace CSharpInteropModule;

public class CSharpFunctionCallsAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("CSharpFunctionCall"))
            return;

        foreach (var child in data.Node.Children)
            data.BytecodeTranslator.Translate(child);

        var method = new DynamicMethodConvertableWrapperImpl();
        var fullName = (data.Node.LexemeValue?.Text).NotNull();
        var call = MethodsFinder.GetMethod(fullName).NotNull();
        var parameters = call.GetParameters();
        var argsCount = data.Node.Children[0].Children.Count;
        method.Make($"Call_{fullName}", call.ReturnType, Enumerable.Repeat<Type?>(null, argsCount).ToList(),
            (il, argsArray) =>
            {
                for (var i = 0; i < argsCount; i++)
                {
                    il.Ldarg(i);
                    if (parameters[i].ParameterType == typeof(object) && argsArray[i].IsValueType)
                        il.Box(argsArray[i]);
                }

                il.Call(call);
                il.Ret();
            });
        data.Bytecode.Instructions.Add(new BytecodeInstruction(
            [],
            new LevelCollection<float, IDynamicMethodConvertable> { { 0, method } })
        );
    }
}