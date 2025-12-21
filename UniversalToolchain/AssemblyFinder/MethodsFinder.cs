using System.Reflection;

namespace AssemblyFinder;

public static class MethodsFinder
{
    public static MethodInfo? GetMethod(string fullName)
    {
        var split = fullName.Split('.');
        if (split.Length != 2) return null;

        var types = TypesFinder.AllTypes
            .Where(x => x.Name == split[0])
            .Take(1)
            .ToArray();
        if (types.Length == 0) return null;

        var method = types.First().GetMethod(split[1], BindingFlags.Static | BindingFlags.Public);
        return method;
    }
}