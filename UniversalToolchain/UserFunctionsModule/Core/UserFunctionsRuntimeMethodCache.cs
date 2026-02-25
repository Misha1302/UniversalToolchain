using System.Reflection;
using ExceptionsManager;

namespace UserFunctionsModule.Core;

internal static class UserFunctionsRuntimeMethodCache
{
    private static readonly Dictionary<int, MethodInfo> Cache = new();

    public static MethodInfo GetInvokeMethod(int argsCount)
    {
        if (Cache.TryGetValue(argsCount, out var method))
            return method;

        var methods = typeof(UserFunctionsRuntime).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(UserFunctionsRuntime.Invoke))
            .ToList();

        method = methods.FirstOrDefault(m =>
            m.GetParameters().Length == argsCount + 1 &&
            m.GetParameters()[^1].ParameterType == typeof(string));

        Thrower.AssertAlways(method != null, $"Слишком много аргументов для пользовательской функции: {argsCount}.");
        Cache[argsCount] = method!;
        return method!;
    }
}
