// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection;

namespace CSharpInteropModule;

public static class MethodsFinder
{
    private static readonly IReadOnlyList<Type> _allTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(x => x.GetTypes()).ToArray();

    public static MethodInfo? GetMethod(string fullName)
    {
        var split = fullName.Split('.');
        if (split.Length != 2) return null;

        var types = _allTypes
            .Where(x => x.Name == split[0])
            .Take(1)
            .ToArray();
        if (types.Length == 0) return null;

        var method = types.First().GetMethod(split[1], BindingFlags.Static | BindingFlags.Public);
        return method;
    }
}