using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public static class WistInterpreterBackendServiceCollectionExtensions
{
    public static IServiceCollection AddWistInterpreterBackend(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistInterpreterDialectBackendServiceProvider>());
        return services;
    }
}