using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Features.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Features;

/// <summary>
///     Registers data-only Wist feature metadata services.
/// </summary>
public static class WistFeatureServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Wist feature catalog as an optional singleton metadata service.
    /// </summary>
    public static IServiceCollection AddWistFeatureCatalog(this IServiceCollection services)
    {
        services = services.ArgNotNull();
        services.AddSingleton<ILanguageFeatureCatalog, WistLanguageFeatureCatalog>();
        return services;
    }
}
