using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistInterpreterBackendServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the Wist interpreter backend registrar as a compatibility convenience. Canonical shipped runtime
    ///     paths resolve and activate this backend from the selected runtime manifest instead.
    /// </summary>
    public static IServiceCollection AddWistInterpreterBackend(this IServiceCollection services)
    {
        services = services.ArgNotNull();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectBackendRuntimeRegistrar, WistInterpreterDialectBackendServiceProvider>());
        return services;
    }
}