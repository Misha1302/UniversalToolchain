using System.Reflection.Emit;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BytecodeDynamicMethodsCompiler.Compilers;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Intrinsics.Capabilities;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistCilDialectBackendServiceProvider : IDialectBackendRuntimeRegistrar
{
    public DialectBackendId BackendId => WistDialectBackendIds.Cil;

    public IReadOnlyList<string> SupportedIntrinsics => AbstractMethodsCompilerImpl.SupportedIntrinsicIds;

    public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        if (configuration == null)
            Thrower.ArgumentNull(nameof(configuration));

        services.AddTransient<ICoreRunnable>(provider => CreateCore(provider, configuration));
        services.AddTransient<ICoreOptimizedRunnable>(provider => CreateCore(provider, configuration));
        services.AddTransient<IExecutableGiver<DynamicMethod>>(provider => CreateCore(provider, configuration));
        services.AddTransient(provider => new WistDialectBackendRuntime(configuration.BackendDescriptor, CreateCore(provider, configuration)));
    }

    private static BasicCoreImpl<DynamicMethod> CreateCore(IServiceProvider provider, DialectBackendRuntimeConfiguration configuration)
    {
        var capabilitySetFactory = provider.GetRequiredService<IIntrinsicCapabilitySetFactory>();
        var backendOptimizers = configuration.OptimizerTypes
            .Select(type => (IIRProcessingModule)provider.GetRequiredService(type))
            .ToList();

        return new BasicCoreImpl<DynamicMethod>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () =>
            {
                var compiler = new DialectIntrinsicPolicyCompiler<DynamicMethod>(
                    provider.GetRequiredService<AbstractMethodsCompilerImpl>(),
                    configuration.AllowedIntrinsics,
                    configuration.ForbiddenIntrinsics,
                    configuration.HasExplicitAllowList);
                return compiler;
            },
            provider.GetRequiredService<Func<IExecutor<DynamicMethod>>>(),
            provider.GetServices<IFrontendCoreModule>().ToList(),
            backendOptimizers,
            [],
            capabilitySetFactory);
    }
}
