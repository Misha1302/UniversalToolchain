namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Registers Wist-specific dialect services and descriptor catalogs.
/// </summary>
public static class WistDialectServiceCollectionExtensions
{
    public static IServiceCollection AddWistDialectServices(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.AddDialectDslDefaultComposition();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, WistDialectRuntimeDescriptorProvider>());
        services.TryAddSingleton(static provider => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders(provider.GetServices<IDialectRuntimeDescriptorProvider>()));
        services.TryAddSingleton<IDialectCompiledDialectBuildPlanBuilder, DialectCompiledDialectBuildPlanBuilder>();
        services.TryAddSingleton<IDialectRuntimeCompositionResolver, DialectRuntimeCompositionResolver>();
        services.TryAddSingleton(static provider => new DialectFrameworkCompositionWorkflow(
            provider.GetRequiredService<DialectDslCompiler>(),
            provider.GetRequiredService<IDialectCompiledDialectBuildPlanBuilder>(),
            provider.GetRequiredService<IDialectRuntimeCompositionResolver>()));
        services.TryAddSingleton<WistDialectExecutionConfigurationBuilder>();
        services.TryAddSingleton<WistDialectServiceProviderFactory>();
        services.TryAddSingleton<WistDialectExecutionWorkflow>();
        return services;
    }
}