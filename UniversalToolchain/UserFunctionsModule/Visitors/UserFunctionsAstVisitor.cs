using AbstractIrExtensions;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using DynamicMethodWrapper;
using BasicTypesExtensions;
using ExceptionsManager;
using UserFunctionsModule.Core;

namespace UserFunctionsModule.Visitors;

public class UserFunctionsAstVisitor : IAstVisitor
{
    private readonly Dictionary<string, UserFunctionDefinition> _functions = [];

    public UserFunctionsAstVisitor()
    {
        UserFunctionsRuntime.Clear();
    }

    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;

        if (nodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("FunctionDeclaration"))
        {
            RegisterFunction(data.Node);
            return;
        }

        if (nodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("UserFunctionCall"))
        {
            TranslateFunctionCall(data);
            return;
        }

        if (nodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Return"))
            Thrower.InvalidOpEx("'return' can be used only inside a function body.");
    }

    private void RegisterFunction(AstNode declarationNode)
    {
        var functionName = declarationNode.Children[0].Text;
        var parameters = AstInliningHelper.ExtractParameters(declarationNode.Children[1]);
        var bodyScope = declarationNode.Children[2];

        Thrower.AssertAlways(parameters.Count == parameters.Distinct(StringComparer.Ordinal).Count(),
            $"Function '{functionName}' has duplicated parameter names.");

        _functions[functionName] = new UserFunctionDefinition(functionName, parameters, bodyScope);
        UserFunctionsRuntime.Register(functionName, parameters, bodyScope);
    }

    private void TranslateFunctionCall(BytecodeVisitorData data)
    {
        var functionName = data.Node.Text;
        Thrower.AssertAlways(_functions.TryGetValue(functionName, out var function), $"Функция не объявлена: '{functionName}'.");

        var argsScope = data.Node.Children[0];
        var arguments = AstInliningHelper.ExtractArguments(argsScope).ToList();
        Thrower.AssertAlways(arguments.Count == function.Parameters.Count,
            $"Неверное число аргументов для функции '{functionName}': ожидалось {function.Parameters.Count}, получено {arguments.Count}.");

        foreach (var argument in arguments)
            data.AstToBytecodeTranslator.Translate(argument);

        var invokeMethod = UserFunctionsRuntimeMethodCache.GetInvokeMethod(arguments.Count);
        var method = new AbstractMethodImpl(
            $"CallUserFunction_{functionName}",
            (il, _) =>
            {
                il.Push(functionName);
                il.CallCSharp(invokeMethod);
            }
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }

    private sealed record UserFunctionDefinition(string Name, IReadOnlyList<string> Parameters, AstNode Body);
}
