using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using ExceptionsManager;
using NumbersModule.Core;

namespace UserFunctionsModule.Core;

public static class UserFunctionsRuntime
{
    private static readonly Dictionary<string, UserFunctionDefinition> Functions = new(StringComparer.Ordinal);

    public static void Register(string functionName, IReadOnlyList<string> parameters, AstNode body)
    {
        Functions[functionName] = new UserFunctionDefinition(functionName, parameters, body);
    }

    public static void Clear() => Functions.Clear();

    public static RealNumberImpl Invoke(string functionName) => InvokeInternal(functionName, []);
    public static RealNumberImpl Invoke<T1>(T1 arg1, string functionName) => InvokeInternal(functionName, [arg1!]);
    public static RealNumberImpl Invoke<T1, T2>(T1 arg1, T2 arg2, string functionName) => InvokeInternal(functionName, [arg1!, arg2!]);
    public static RealNumberImpl Invoke<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, string functionName) => InvokeInternal(functionName, [arg1!, arg2!, arg3!]);
    public static RealNumberImpl Invoke<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, string functionName) => InvokeInternal(functionName, [arg1!, arg2!, arg3!, arg4!]);
    public static RealNumberImpl Invoke<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, string functionName) => InvokeInternal(functionName, [arg1!, arg2!, arg3!, arg4!, arg5!]);

    private static RealNumberImpl InvokeInternal(string functionName, IReadOnlyList<object?> args)
    {
        Thrower.AssertAlways(Functions.TryGetValue(functionName, out var function), $"Функция не объявлена: '{functionName}'.");
        Thrower.AssertAlways(args.Count == function!.Parameters.Count,
            $"Неверное число аргументов для функции '{functionName}': ожидалось {function.Parameters.Count}, получено {args.Count}.");

        var frame = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (var i = 0; i < function.Parameters.Count; i++)
            frame[function.Parameters[i]] = args[i];

        var result = ExecuteScope(function.Body, frame);
        Thrower.AssertAlways(result.HasReturned, $"Функция '{functionName}': return отсутствует.");
        Thrower.AssertAlways(result.Value is RealNumberImpl, $"Функция '{functionName}' должна возвращать число.");
        return (RealNumberImpl)result.Value!;
    }

    private static ExecutionResult ExecuteScope(AstNode scope, Dictionary<string, object?> frame)
    {
        foreach (var node in scope.Children)
        {
            var statementResult = ExecuteStatement(node, frame);
            if (statementResult.HasReturned)
                return statementResult;
        }

        return ExecutionResult.NoReturn;
    }

    private static ExecutionResult ExecuteStatement(AstNode node, Dictionary<string, object?> frame)
    {
        var nodeType = node.NodeType.GetName();

        if (nodeType == "Return")
            return ExecutionResult.Return(EvaluateExpression(node.Children[0], frame));

        if (nodeType == "If")
            return ExecuteIf(node, frame);

        if (nodeType == "While")
            return ExecuteWhile(node, frame);

        if (nodeType == "Equality")
        {
            var target = node.Children[0];
            var value = EvaluateExpression(node.Children[1], frame);
            frame[target.Text] = value;
            return ExecutionResult.NoReturn;
        }

        EvaluateExpression(node, frame);
        return ExecutionResult.NoReturn;
    }

    private static ExecutionResult ExecuteIf(AstNode ifNode, Dictionary<string, object?> frame)
    {
        var condition = ToBool(EvaluateExpression(ifNode.Children[0], frame));
        if (condition)
            return ExecuteScope(ifNode.Children[1], frame);

        for (var i = 2; i < ifNode.Children.Count; i++)
        {
            var branch = ifNode.Children[i];
            var branchType = branch.NodeType.GetName();

            if (branchType == "Elif")
            {
                if (ToBool(EvaluateExpression(branch.Children[0], frame)))
                    return ExecuteScope(branch.Children[1], frame);
            }
            else if (branchType == "Else")
            {
                return ExecuteScope(branch.Children[0], frame);
            }
        }

        return ExecutionResult.NoReturn;
    }

    private static ExecutionResult ExecuteWhile(AstNode whileNode, Dictionary<string, object?> frame)
    {
        while (ToBool(EvaluateExpression(whileNode.Children[0], frame)))
        {
            var bodyResult = ExecuteScope(whileNode.Children[1], frame);
            if (bodyResult.HasReturned)
                return bodyResult;
        }

        return ExecutionResult.NoReturn;
    }

    private static object? EvaluateExpression(AstNode node, Dictionary<string, object?> frame)
    {
        var nodeType = node.NodeType.GetName();

        return nodeType switch
        {
            "Number" => ParseNumber(node),
            "Variable" => ResolveVariable(node.Text, frame),
            "UserFunctionCall" => EvaluateUserFunctionCall(node, frame),
            "Addition" => RealNumberImpl.Add(ToNumber(EvaluateExpression(node.Children[0], frame)), ToNumber(EvaluateExpression(node.Children[1], frame))),
            "Substraction" => RealNumberImpl.Sub(ToNumber(EvaluateExpression(node.Children[0], frame)), ToNumber(EvaluateExpression(node.Children[1], frame))),
            "Multiplication" => RealNumberImpl.Mul(ToNumber(EvaluateExpression(node.Children[0], frame)), ToNumber(EvaluateExpression(node.Children[1], frame))),
            "Division" => RealNumberImpl.Div(ToNumber(EvaluateExpression(node.Children[0], frame)), ToNumber(EvaluateExpression(node.Children[1], frame))),
            "Equal" => ToNumber(EvaluateExpression(node.Children[0], frame)).CompareTo(ToNumber(EvaluateExpression(node.Children[1], frame))) == 0,
            "NotEqual" => ToNumber(EvaluateExpression(node.Children[0], frame)).CompareTo(ToNumber(EvaluateExpression(node.Children[1], frame))) != 0,
            "Greater" => ToNumber(EvaluateExpression(node.Children[0], frame)).CompareTo(ToNumber(EvaluateExpression(node.Children[1], frame))) > 0,
            "Less" => ToNumber(EvaluateExpression(node.Children[0], frame)).CompareTo(ToNumber(EvaluateExpression(node.Children[1], frame))) < 0,
            "GreaterOrEqual" => ToNumber(EvaluateExpression(node.Children[0], frame)).CompareTo(ToNumber(EvaluateExpression(node.Children[1], frame))) >= 0,
            "LessOrEqual" => ToNumber(EvaluateExpression(node.Children[0], frame)).CompareTo(ToNumber(EvaluateExpression(node.Children[1], frame))) <= 0,
            "True" => true,
            "False" => false,
            "And" => ToBool(EvaluateExpression(node.Children[0], frame)) && ToBool(EvaluateExpression(node.Children[1], frame)),
            "Or" => ToBool(EvaluateExpression(node.Children[0], frame)) || ToBool(EvaluateExpression(node.Children[1], frame)),
            "Not" => !ToBool(EvaluateExpression(node.Children[0], frame)),
            "Scope" => EvaluateScopeExpression(node, frame),
            _ => Thrower.InvalidOpEx<object?>($"Неподдерживаемое выражение в функции: {nodeType}.")
        };
    }



    private static object? EvaluateScopeExpression(AstNode scopeNode, Dictionary<string, object?> frame)
    {
        object? last = null;
        foreach (var child in scopeNode.Children)
            last = EvaluateExpression(child, frame);
        return last;
    }

        private static object? EvaluateUserFunctionCall(AstNode node, Dictionary<string, object?> frame)
    {
        var args = AstInliningHelper.ExtractArguments(node.Children[0])
            .Select(arg => EvaluateExpression(arg, frame))
            .ToList();
        return InvokeInternal(node.Text, args);
    }

    private static object? ResolveVariable(string name, Dictionary<string, object?> frame)
    {
        Thrower.AssertAlways(frame.TryGetValue(name, out var value), $"Переменная '{name}' не объявлена в функции.");
        return value;
    }

    private static RealNumberImpl ParseNumber(AstNode node)
    {
        var text = (node.LexemeValue?.Text ?? node.Text).Replace("_", "");
        return new RealNumberImpl(double.Parse(text, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static RealNumberImpl ToNumber(object? value)
    {
        Thrower.AssertAlways(value is RealNumberImpl, "Ожидалось числовое значение.");
        return (RealNumberImpl)value;
    }

    private static bool ToBool(object? value)
    {
        Thrower.AssertAlways(value is bool, "Ожидалось логическое значение.");
        return (bool)value;
    }

    private sealed record UserFunctionDefinition(string Name, IReadOnlyList<string> Parameters, AstNode Body);

    private readonly record struct ExecutionResult(bool HasReturned, object? Value)
    {
        public static ExecutionResult NoReturn => new(false, null);
        public static ExecutionResult Return(object? value) => new(true, value);
    }
}
