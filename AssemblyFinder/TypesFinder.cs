// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com
using System.Reflection;
using ExceptionsManager;

namespace AssemblyFinder;

public static class TypesFinder
{
    public static readonly IReadOnlyList<Assembly> Assemblies = GetAssemblies().ToList();

    public static readonly IReadOnlyList<Type> AllTypes =
        Assemblies
            .SelectMany(x => x.GetTypes().Union(x.GetTypes().SelectMany(y => y.GetInterfaces())))
            .ToArray();

    public static Type GetType(string name)
    {
        return AllTypes.FirstOrDefault(x => MakeSimpleName(x) == name).NotNull("Cannot find type: " + name);

        string MakeSimpleName(Type type)
        {
            if (!type.ContainsGenericParameters) return type.Name;

            var simpleName = type.Name.Contains('`') ? type.Name[..type.Name.IndexOf('`')] : type.Name;

            if (type.GetGenericArguments().Length == 0) return simpleName;

            return simpleName
                   + "<"
                   + string.Join(", ", type.GetGenericArguments().Select(MakeSimpleName).ToArray()) +
                   ">";
        }
    }

    private static IEnumerable<Assembly> GetAssemblies()
    {
        var list = new List<string>();
        var stack = new Stack<Assembly>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) stack.Push(assembly);

        do
        {
            var asm = stack.Pop();

            yield return asm;

            foreach (var reference in asm.GetReferencedAssemblies())
                if (!list.Contains(reference.FullName))
                {
                    stack.Push(Assembly.Load(reference));
                    list.Add(reference.FullName);
                }
        } while (stack.Count > 0);
    }
}