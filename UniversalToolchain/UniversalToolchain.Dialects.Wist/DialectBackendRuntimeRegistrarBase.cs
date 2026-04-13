using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Intrinsics.Capabilities;

namespace UniversalToolchain.Dialects.Wist;

internal abstract class DialectBackendRuntimeRegistrarBase<TCompilationOutput> : IDialectBackendRuntimeRegistrar
{
    public abstract DialectBackendId BackendId { get; }

    public abstract IReadOnlyList<string> SupportedIntrinsics { get; }

    public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
    {
        services = services.ArgNotNull();

        configuration = configuration.ArgNotNull();

        RegisterBackendDefaults(services);

        services.AddTransient<ICoreRunnable>(provider => CreateCore(provider, configuration));
        services.AddTransient<ICoreOptimizedRunnable>(provider => CreateCore(provider, configuration));
        services.AddTransient<IExecutableGiver<TCompilationOutput>>(provider => CreateCore(provider, configuration));
        services.AddTransient(provider => new WistDialectBackendRuntime(configuration.BackendDescriptor, CreateCore(provider, configuration)));
    }

    protected abstract void RegisterBackendDefaults(IServiceCollection services);

    protected abstract IAbstractIrCompiler<TCompilationOutput> ResolveBackendCompiler(IServiceProvider provider);

    protected abstract Func<IExecutor<TCompilationOutput>> ResolveExecutorFactory(IServiceProvider provider);

    private BasicCoreImpl<TCompilationOutput> CreateCore(IServiceProvider provider, DialectBackendRuntimeConfiguration configuration)
    {
        var capabilitySetFactory = provider.GetRequiredService<IIntrinsicCapabilitySetFactory>();
        var backendOptimizers = configuration.OptimizerTypes
            .Select(type => (IIRProcessingModule)provider.GetRequiredService(type))
            .ToList();
        var frontendModules = provider.GetServices<IFrontendCoreModule>().ToList();
        var backendCompiler = ResolveBackendCompiler(provider);
        var executorFactory = ResolveExecutorFactory(provider);

        return new BasicCoreImpl<TCompilationOutput>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => new DialectIntrinsicPolicyCompiler<TCompilationOutput>(
                backendCompiler,
                configuration.AllowedIntrinsics,
                configuration.ForbiddenIntrinsics,
                configuration.HasExplicitAllowList),
            executorFactory,
            frontendModules,
            backendOptimizers,
            [],
            capabilitySetFactory);
    }
}
