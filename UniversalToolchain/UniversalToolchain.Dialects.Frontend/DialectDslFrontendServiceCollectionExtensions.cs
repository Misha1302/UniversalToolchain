using BasicCore.Contracts;
using BasicCore.Registration;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Frontend.Registration;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslFrontendServiceCollectionExtensions
{
    public static IServiceCollection AddDialectDslFrontendCompilerServices(
        this IServiceCollection services,
        DialectDslFrontendModule frontendModule)
    {
        services = services.ArgNotNull();
        frontendModule = frontendModule.ArgNotNull();

        services
            .AddCoreIntrinsicServices()
            .AddBasicFrontendPipelineDefaults();

        services.AddSingleton(frontendModule);
        services.AddSingleton<IFrontendCoreModule>(frontendModule);

        return services;
    }
}
