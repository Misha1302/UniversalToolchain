using BasicCore.Contracts;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslServiceCollectionExtensions
{
    public static IServiceCollection AddDialectDslDefaultComposition(this IServiceCollection services)
    {
        if (services == null)
        {
            Thrower.ArgumentNull(nameof(services));
        }

        return services
            .AddDialectDsl()
            .AddDialectDslBuiltIns();
    }

    public static IServiceCollection AddDialectDsl(this IServiceCollection services)
    {
        if (services == null)
        {
            Thrower.ArgumentNull(nameof(services));
        }

        services.TryAddSingleton<IDialectDslRegistryFactory, DialectDslRegistryFactory>();
        services.TryAddSingleton(static provider => provider.GetRequiredService<IDialectDslRegistryFactory>().CreateRegistry());
        services.TryAddSingleton<DialectDslFrontendModule>();
        if (!services.Any(x => x.ServiceType == typeof(IFrontendCoreModule) && x.ImplementationType == typeof(DialectDslFrontendModule)))
        {
            services.AddSingleton<IFrontendCoreModule>(static provider => provider.GetRequiredService<DialectDslFrontendModule>());
        }
        services.TryAddTransient<DialectDirectiveLineParser>();
        services.TryAddTransient<DialectDefinitionSliceParser>();
        services.TryAddTransient<DialectDslCompiler>();
        return services;
    }

    public static IServiceCollection AddDialectDslBuiltIns(this IServiceCollection services)
    {
        if (services == null)
        {
            Thrower.ArgumentNull(nameof(services));
        }

        return services
            .AddDialectDirectiveFeature<UseModulesDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<ExcludeModulesDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<RequiresModulesDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<BeforeModulesDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<AfterModulesDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<BackendDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<AllowIntrinsicDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<ForbidIntrinsicDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<EnableOptimizerDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<DisableOptimizerDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<SecurityDialectDirectiveFeature>()
            .AddDialectDirectiveFeature<CapabilityDialectDirectiveFeature>()
            .AddDialectDocumentValidationRule<UseExcludeConflictDocumentValidationRule>();
    }

    public static IServiceCollection AddDialectDirectiveFeature<TFeature>(this IServiceCollection services)
        where TFeature : class, IDialectDirectiveFeature
    {
        if (services == null)
        {
            Thrower.ArgumentNull(nameof(services));
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectDirectiveFeature, TFeature>());
        return services;
    }

    public static IServiceCollection AddDialectDirectiveFeatureProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IDialectDslFeatureProvider
    {
        if (services == null)
        {
            Thrower.ArgumentNull(nameof(services));
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectDslFeatureProvider, TProvider>());
        return services;
    }

    public static IServiceCollection AddDialectDocumentValidationRule<TRule>(this IServiceCollection services)
        where TRule : class, IDialectDocumentValidationRule
    {
        if (services == null)
        {
            Thrower.ArgumentNull(nameof(services));
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectDocumentValidationRule, TRule>());
        return services;
    }
}
