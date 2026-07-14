using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Core.ServiceCollection;

/// <summary>
///     Compatibility entry point for the built-in frontend defaults.
/// </summary>
public static class BasicFrontendPipelineServiceCollectionExtensions
{
    public static IServiceCollection AddBasicFrontendPipelineDefaults(this IServiceCollection services)
    {
        services = services.ArgNotNull();
        return UniversalToolchain.Dialects.Frontend.Registration.BasicFrontendPipelineServiceCollectionExtensions
            .AddBasicFrontendPipelineDefaults(services);
    }
}
