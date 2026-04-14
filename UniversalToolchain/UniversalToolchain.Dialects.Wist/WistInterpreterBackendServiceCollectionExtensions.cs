using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public static class WistInterpreterBackendServiceCollectionExtensions
{
    public static IServiceCollection AddWistInterpreterBackend(this IServiceCollection services)
    {
        services = services.ArgNotNull();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistInterpreterDialectBackendServiceProvider>());
        return services;
    }
}