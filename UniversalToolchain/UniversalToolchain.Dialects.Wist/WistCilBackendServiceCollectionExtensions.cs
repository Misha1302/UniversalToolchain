using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistCilBackendServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the Wist CIL backend registrar as a compatibility convenience. Canonical shipped runtime paths
    ///     resolve and activate this backend from the selected runtime manifest instead.
    /// </summary>
    public static IServiceCollection AddWistCilBackend(this IServiceCollection services)
    {
        services = services.ArgNotNull();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistCilDialectBackendServiceProvider>());
        return services;
    }
}