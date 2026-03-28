using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public static class WistCilBackendServiceCollectionExtensions
{
    public static IServiceCollection AddWistCilBackend(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistCilDialectBackendServiceProvider>());
        return services;
    }
}
