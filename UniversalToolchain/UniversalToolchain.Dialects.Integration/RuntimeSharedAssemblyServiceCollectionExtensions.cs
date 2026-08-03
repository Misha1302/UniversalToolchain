using System.Reflection;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Integration;

public static class RuntimeSharedAssemblyServiceCollectionExtensions
{
    public static IServiceCollection AddRuntimeSharedAssembly(this IServiceCollection services, Assembly assembly)
    {
        services = services.ArgNotNull();
        assembly = assembly.ArgNotNull();
        services.AddSingleton(RuntimeSharedAssemblyDescriptor.Create(assembly));
        return services;
    }

    public static IServiceCollection AddRuntimeSharedAssemblies(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        services = services.ArgNotNull();
        assemblies = assemblies.ArgNotNull();
        foreach (var assembly in assemblies)
            services.AddRuntimeSharedAssembly(assembly);
        return services;
    }
}
