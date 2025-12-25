using System.Collections.Concurrent;
using System.Reflection;

namespace AssemblyFinder;

public static class MethodsFinder
{
    private static readonly ConcurrentDictionary<string, MethodInfo?> _methodCache = new();

    public static MethodInfo? GetMethod(string fullName)
    {
        return _methodCache.GetOrAdd(fullName, FindMethod);
    }

    private static MethodInfo? FindMethod(string fullName)
    {
        var split = fullName.Split('.');
        if (split.Length != 2)
            return null;

        var types = TypesFinder.AllTypes
            .Where(x => x.Name == split[0])
            .Take(1)
            .ToArray();

        return types.Length == 0
            ? null
            : types.First().GetMethod(split[1], BindingFlags.Static | BindingFlags.Public);
    }
}