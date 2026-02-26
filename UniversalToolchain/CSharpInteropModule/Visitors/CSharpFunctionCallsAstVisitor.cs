using AbstractIrExtensions;
using AssemblyFinder;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;

namespace CSharpInteropModule.Visitors;

public class CSharpFunctionCallsAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("CSharpFunctionCall"))
            return;

        var argsScope = data.Node.Children[0];
        var arguments = argsScope.Children;

        // Обрабатываем аргументы - они будут положены в стек
        foreach (var argument in arguments)
            data.AstToBytecodeTranslator.Translate(argument);

        var fullName = (data.Node.LexemeValue?.Text).NotNull();

        var method = new AbstractMethodImpl(
            $"Call_{fullName}",
            (il, context) =>
            {
                // Используем типы из стека для разрешения перегрузки
                // Количество аргументов = количество детей узла
                var argCount = arguments.Count;
                var stackTypes = context.Stack.TakeLast(argCount).ToList();

                // Пытаемся найти метод с учетом типов параметров
                var methodInfo = MethodsFinder.GetMethod(fullName, stackTypes.ToArray())
                                 ?? MethodsFinder.GetMethod(fullName, argCount)
                                 ?? MethodsFinder.GetMethod(fullName);

                il.CallCSharp(methodInfo.NotNull());
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}