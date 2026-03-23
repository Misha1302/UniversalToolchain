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

/// <summary>
///     Registers Wist-specific dialect services and descriptor catalogs.
/// </summary>
public static class WistDialectServiceCollectionExtensions
{
    public static IServiceCollection AddWistDialectServices(this IServiceCollection services)
    {
        if (services == null)
        {
            Thrower.ArgumentNull(nameof(services));
        }

        services.AddDialectDslDefaultComposition();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWistDialectBackendServiceProvider, WistCilDialectBackendServiceProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWistDialectBackendServiceProvider, WistInterpreterDialectBackendServiceProvider>());
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
