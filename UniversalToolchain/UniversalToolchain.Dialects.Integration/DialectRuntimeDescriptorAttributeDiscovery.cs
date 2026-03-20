using System.Reflection;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

internal static class DialectRuntimeDescriptorAttributeDiscovery
{
    public static IReadOnlyList<RuntimeModuleDescriptor> DiscoverModules(IEnumerable<Assembly> assemblies)
    {
        return DiscoverModules(EnumerateCandidateTypes(assemblies));
    }

    public static IReadOnlyList<RuntimeModuleDescriptor> DiscoverModules(IEnumerable<Type> types)
    {
        return DiscoverTypeDescriptors(
            types,
            static type => type.GetCustomAttributes<DialectModuleAliasAttribute>(false).ToArray(),
            static (type, aliases) => new RuntimeModuleDescriptor(type, aliases));
    }

    public static IReadOnlyList<RuntimeOptimizerDescriptor> DiscoverOptimizers(IEnumerable<Assembly> assemblies)
    {
        return DiscoverOptimizers(EnumerateCandidateTypes(assemblies));
    }

    public static IReadOnlyList<RuntimeOptimizerDescriptor> DiscoverOptimizers(IEnumerable<Type> types)
    {
        return DiscoverTypeDescriptors(
            types,
            static type => type.GetCustomAttributes<DialectOptimizerAliasAttribute>(false).ToArray(),
            static (type, aliases) => new RuntimeOptimizerDescriptor(type, aliases));
    }

    public static IReadOnlyList<RuntimeBackendDescriptor> DiscoverBackends(IEnumerable<Assembly> assemblies)
    {
        return DiscoverBackends(EnumerateCandidateTypes(assemblies));
    }

    public static IReadOnlyList<RuntimeBackendDescriptor> DiscoverBackends(IEnumerable<Type> types)
    {
        return DiscoverTypeDescriptors(
            types,
            static type => type.GetCustomAttributes<DialectBackendAliasAttribute>(false).ToArray(),
            CreateBackendDescriptor);
    }

    private static IReadOnlyList<TDescriptor> DiscoverTypeDescriptors<TAttribute, TDescriptor>(
        IEnumerable<Type> types,
        Func<Type, IReadOnlyList<TAttribute>> getAttributes,
        Func<Type, IReadOnlyList<string>, TDescriptor> createDescriptor)
        where TAttribute : DialectAliasAttributeBase
    {
        if (types == null)
            Thrower.ArgumentNull(nameof(types));

        var result = new List<TDescriptor>();
        foreach (var type in EnumerateCandidateTypes(types))
        {
            var attributes = getAttributes(type);
            if (attributes.Count == 0)
                continue;

            var aliases = attributes
                .SelectMany(static x => x.Aliases)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToArray();
            result.Add(createDescriptor(type, aliases));
        }

        return result;
    }

    private static RuntimeBackendDescriptor CreateBackendDescriptor(Type type, IReadOnlyList<string> aliases)
    {
        if (!typeof(DialectBackendDeclaration).IsAssignableFrom(type))
            Thrower.Argument(nameof(type), $"Backend declaration type '{type.FullName}' must inherit from '{typeof(DialectBackendDeclaration).FullName}'.");

        var instance = Activator.CreateInstance(type);
        if (instance is not DialectBackendDeclaration)
            Thrower.InvalidOpEx($"Could not create backend declaration '{type.FullName}'.");

        var declaration = (DialectBackendDeclaration)instance;
        return new RuntimeBackendDescriptor(declaration.GetBackendId(), type, aliases);
    }

    private static IReadOnlyList<Type> EnumerateCandidateTypes(IEnumerable<Assembly> assemblies)
    {
        var assemblyList = assemblies
            .Select(static x =>
            {
                if (x == null)
                    Thrower.Argument(nameof(assemblies), "Assembly list must not contain null entries.");

                return x;
            })
            .Distinct()
            .OrderBy(static x => x.FullName, StringComparer.Ordinal)
            .SelectMany(GetAssemblyTypes)
            .Where(static x => x is { IsClass: true, IsAbstract: false })
            .OrderBy(static x => x.FullName, StringComparer.Ordinal)
            .ToArray();

        return assemblyList;
    }

    private static IReadOnlyList<Type> EnumerateCandidateTypes(IEnumerable<Type> types)
    {
        return types
            .Select(static x =>
            {
                if (x == null)
                    Thrower.Argument(nameof(types), "Type list must not contain null entries.");

                return x;
            })
            .Distinct()
            .Where(static x => x is { IsClass: true, IsAbstract: false })
            .OrderBy(static x => x.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<Type> GetAssemblyTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var loaderMessages = ex.LoaderExceptions
                .Where(static x => x != null)
                .Select(static x => x!.Message)
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToArray();
            var details = loaderMessages.Length == 0 ? "No loader exceptions were provided." : string.Join(" | ", loaderMessages);
            Thrower.InvalidOpEx($"Could not enumerate runtime descriptor types from assembly '{assembly.FullName}'. {details}");
            return Array.Empty<Type>();
        }
    }
}
