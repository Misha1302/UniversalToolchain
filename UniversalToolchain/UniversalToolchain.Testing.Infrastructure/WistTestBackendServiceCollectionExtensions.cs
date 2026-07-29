using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Test-only explicit backend registration for low-level host contract tests.
/// Product runtime activation remains manifest-backed.
/// </summary>
internal static class WistTestBackendServiceCollectionExtensions
{
    public static IServiceCollection AddWistCilBackend(this IServiceCollection services)
    {
        services = services.ArgNotNull();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistCilDialectBackendServiceProvider>());
        return services;
    }

    public static IServiceCollection AddWistInterpreterBackend(this IServiceCollection services)
    {
        services = services.ArgNotNull();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistInterpreterDialectBackendServiceProvider>());
        return services;
    }
}
