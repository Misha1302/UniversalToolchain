using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public static class WistCilBackendServiceCollectionExtensions
{
    public static IServiceCollection AddWistCilBackend(this IServiceCollection services)
    {
        services = services.ArgNotNull();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistCilDialectBackendServiceProvider>());
        return services;
    }
}