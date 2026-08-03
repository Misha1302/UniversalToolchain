using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Registers reflection-based runtime component resolution services.
/// </summary>
public static class ReflectionRuntimeResolutionServiceCollectionExtensions
{
    /// <summary>
    ///     Adds assembly loading, component resolution, component type loading, and known backend discovery services.
    /// </summary>
    public static IServiceCollection AddReflectionRuntimeResolutionServices(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        services.TryAddSingleton<IRuntimeAssemblyLocator, DefaultRuntimeAssemblyLocator>();
        services.TryAddSingleton<IRuntimeSharedAssemblyResolver, DefaultRuntimeSharedAssemblyResolver>();
        services.TryAddSingleton<IRuntimeAssemblyLoadStrategy, DefaultRuntimeAssemblyLoadStrategy>();
        services.TryAddSingleton<IRuntimeAssemblyTypeLoader, DefaultRuntimeAssemblyTypeLoader>();
        services.TryAddSingleton<IRuntimeComponentResolver>(provider => new DefaultRuntimeComponentResolver(
            provider.GetRequiredService<IRuntimeAssemblyTypeLoader>()));
        services.TryAddSingleton<IRuntimeComponentTypeLoader, DefaultRuntimeComponentTypeLoader>();
        services.TryAddSingleton<IRuntimeBackendRegistrarResolver, DefaultRuntimeBackendRegistrarResolver>();
        services.TryAddSingleton<IRuntimeKnownBackendsProvider, RuntimeKnownBackendsProvider>();

        return services;
    }
}