using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Registers file-system-backed runtime catalog services.
/// </summary>
public static class FileSystemRuntimeCatalogServiceCollectionExtensions
{
    /// <summary>
    ///     Adds manifest discovery, manifest serialization, and component catalog services backed by runtime artifact files.
    /// </summary>
    public static IServiceCollection AddFileSystemRuntimeCatalogServices(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        services.TryAddSingleton(new RuntimeArtifactLocatorOptions());
        services.TryAddSingleton<IRuntimeManifestFileLocator, DefaultRuntimeManifestFileLocator>();
        services.TryAddSingleton<IRuntimeManifestSerializer, RuntimeManifestJsonSerializer>();
        services.TryAddSingleton<IRuntimeComponentCatalog, FileBasedRuntimeComponentCatalog>();

        return services;
    }
}