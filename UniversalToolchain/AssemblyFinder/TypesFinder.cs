using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using ExceptionsManager;

namespace AssemblyFinder;

public static class TypesFinder
{
    private static readonly Lazy<IReadOnlyList<Assembly>> _assemblies = new(() => GetAssemblies().ToArray());
    private static readonly Lazy<IReadOnlyList<Type>> _allTypes = new(() => GetAllTypes().ToArray());
    private static readonly ConcurrentDictionary<string, Type> _typeCache = new();

    public static IReadOnlyList<Assembly> Assemblies => _assemblies.Value;
    public static IReadOnlyList<Type> AllTypes => _allTypes.Value;

    public static Type GetType(string name)
    {
        return _typeCache.GetOrAdd(name, n =>
            AllTypes.FirstOrDefault(x => MakeSimpleName(x) == n)
                .NotNull($"Cannot find type: {n}"));
    }

    private static IEnumerable<Type> GetAllTypes()
    {
        return Assemblies
            .SelectMany(x => x.GetTypes().Union(x.GetTypes().SelectMany(y => y.GetInterfaces())))
            .Distinct();
    }

    private static string MakeSimpleName(Type type)
    {
        if (!type.ContainsGenericParameters)
            return type.Name;

        var simpleName = type.Name.Contains('`')
            ? type.Name[..type.Name.IndexOf('`')]
            : type.Name;

        if (type.GetGenericArguments().Length == 0)
            return simpleName;

        return simpleName + "<" + string.Join(", ", type.GetGenericArguments().Select(MakeSimpleName)) + ">";
    }

    private static IEnumerable<Assembly> GetAssemblies()
    {
        var visited = new HashSet<string>();
        var stack = new Stack<Assembly>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            stack.Push(assembly);

        while (stack.Count > 0)
        {
            var asm = stack.Pop();
            yield return asm;

            foreach (var reference in asm.GetReferencedAssemblies())
            {
                if (visited.Contains(reference.FullName))
                    continue;

                try
                {
                    stack.Push(Assembly.Load(reference));
                    visited.Add(reference.FullName);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Could not load {reference} assembly 'cause {e}");
                }
            }
        }
    }
}