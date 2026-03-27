using ArithmeticModule;
using CommentsModule;
using ConditionsModule;
using CSharpInteropModule;
using EqualityModule;
using ExceptionsManager;
using IdentifierModule;
using InternalPreprocessorLexemesModule;
using LabelsModule;
using LocalVariablesOptimizerModule;
using LoopsModule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NativeMathModule;
using NumbersModule;
using ParametersSetterModule;
using ScopesModule;
using SemicolonAsNewLineModule;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;
using VariablesModule;
using WhitespacesModule;

namespace UniversalToolchain.Dialects.Wist;

public static class WistDialectServiceCollectionExtensions
{
    public static IServiceCollection AddWistDialectServices(this IServiceCollection services) => AddWistDialectServicesMinimal(services);

    public static IServiceCollection AddWistDialectServicesMinimal(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.AddDialectDslDefaultComposition();
        AddSharedWistDialectServices(services);
        AddMinimalCompositionServices(services);
        return services;
    }

    public static IServiceCollection AddWistDialectServicesLegacy(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.AddDialectDslDefaultComposition();
        AddSharedWistDialectServices(services);
        AddLegacyCompositionServices(services);
        return services;
    }

    private static void AddSharedWistDialectServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWistDialectBackendServiceProvider, WistCilDialectBackendServiceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWistDialectBackendServiceProvider, WistInterpreterDialectBackendServiceProvider>());

        services.TryAddSingleton<IWistRuntimeManifest, WistRuntimeManifest>();
        services.TryAddSingleton<SelectedRuntimePlanResolver>();
        services.TryAddSingleton<RuntimeArtifactLocatorOptions>();
        services.TryAddSingleton<IRuntimeManifestFileLocator>(provider =>
            new DefaultRuntimeManifestFileLocator(provider.GetRequiredService<RuntimeArtifactLocatorOptions>()));
        services.TryAddSingleton<IRuntimeAssemblyLocator>(provider =>
            new DefaultRuntimeAssemblyLocator(provider.GetRequiredService<RuntimeArtifactLocatorOptions>()));
        services.TryAddSingleton<RuntimeManifestJsonSerializer>();
        services.TryAddSingleton<IRuntimeComponentTypeLoader, DefaultRuntimeComponentTypeLoader>();
        services.TryAddSingleton<DialectIntrinsicPolicyResolver>();
        services.TryAddSingleton<IDialectCompiledDialectBuildPlanBuilder, DialectCompiledDialectBuildPlanBuilder>();
        services.TryAddSingleton<WistDialectExecutionConfigurationBuilder>();
        services.TryAddSingleton<WistDialectServiceProviderFactory>();
        services.TryAddSingleton<WistDialectExecutionWorkflow>();
    }

    private static void AddMinimalCompositionServices(IServiceCollection services)
    {
        // minimal path intentionally avoids runtime descriptor discovery services.
    }

    private static void AddLegacyCompositionServices(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, WistDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, ArithmeticDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, ConditionsDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, EqualityDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, NativeMathDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, NumbersDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, CommentsDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, IdentifierDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, InternalPreprocessorLexemesDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, SemicolonAsNewLineDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, WhitespacesDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, ParametersSetterDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, ScopesDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, VariablesDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, LabelsDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, LoopsDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, CSharpInteropDialectRuntimeDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, LocalVariablesOptimizerDialectRuntimeDescriptorProvider>());

        services.TryAddSingleton(static provider => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders(provider.GetServices<IDialectRuntimeDescriptorProvider>()));
        services.TryAddSingleton<IDialectRuntimeCompositionResolver, DialectRuntimeCompositionResolver>();
        services.TryAddSingleton(static provider => new DialectFrameworkCompositionWorkflow(
            provider.GetRequiredService<DialectDslCompiler>(),
            provider.GetRequiredService<IDialectCompiledDialectBuildPlanBuilder>(),
            provider.GetRequiredService<IDialectRuntimeCompositionResolver>()));
        services.TryAddSingleton<LegacyWistDialectCompositionService>();
    }
}
