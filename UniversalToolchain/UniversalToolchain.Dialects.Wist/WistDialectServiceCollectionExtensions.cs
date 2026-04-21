using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public static class WistDialectServiceCollectionExtensions
{
    public static IServiceCollection AddWistDialectServices(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        return services
            .AddWistDialectCoreServices()
            .AddFileSystemRuntimeCatalogServices()
            .AddReflectionRuntimeResolutionServices();
    }
}
