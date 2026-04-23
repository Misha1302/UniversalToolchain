using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public static class WistDialectServiceCollectionExtensions
{
    /// <summary>
    ///     Adds canonical Wist dialect composition services with manifest-driven runtime catalog and exact activation.
    /// </summary>
    public static IServiceCollection AddWistDialectServices(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        return services
            .AddWistDialectCoreServices()
            .AddFileSystemRuntimeCatalogServices()
            .AddReflectionRuntimeResolutionServices();
    }
}
