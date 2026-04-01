using Microsoft.Extensions.DependencyInjection;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public static class WistCilBackendServiceCollectionExtensions
{
    public static IServiceCollection AddWistCilBackend(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistCilDialectBackendServiceProvider>());
        return services;
    }
}