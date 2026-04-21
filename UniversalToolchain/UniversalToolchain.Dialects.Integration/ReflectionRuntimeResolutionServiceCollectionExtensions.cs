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
        services.TryAddSingleton<IRuntimeAssemblyLoadStrategy, DefaultRuntimeAssemblyLoadStrategy>();
        services.TryAddSingleton<IRuntimeComponentResolver, DefaultRuntimeComponentResolver>();
        services.TryAddSingleton<IRuntimeComponentTypeLoader, DefaultRuntimeComponentTypeLoader>();
        services.TryAddSingleton<IRuntimeKnownBackendsProvider, RuntimeKnownBackendsProvider>();

        return services;
    }
}
