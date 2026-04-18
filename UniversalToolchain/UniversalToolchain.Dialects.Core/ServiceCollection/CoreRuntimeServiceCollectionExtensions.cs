using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Core.ServiceCollection;

/// <summary>
///     Registers the compatibility runtime bootstrap that includes neutral services plus the built-in concrete defaults.
/// </summary>
public static class CoreRuntimeServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the historical convenience bootstrap for consumers that expect the full built-in runtime pipeline.
    ///     This method is not the minimal neutral runtime layer; use
    ///     <see cref="NeutralRuntimeServiceCollectionExtensions.AddNeutralRuntimeInfrastructure" />
    ///     when concrete frontend and backend defaults should be composed explicitly.
    /// </summary>
    public static IServiceCollection AddCoreRuntimeInfrastructure(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        return services
            .AddNeutralRuntimeInfrastructure()
            .AddBasicFrontendPipelineDefaults()
            .AddCompilerBackendDefaults()
            .AddInterpreterBackendDefaults();
    }
}