using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Intrinsics.Capabilities;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistDialectBackendCoreFactory
{
    public static BasicCoreImpl<TCompilationOutput> Create<TCompilationOutput>(
        IServiceProvider provider,
        DialectBackendRuntimeConfiguration configuration,
        Func<IServiceProvider, DialectBackendRuntimeConfiguration, IAbstractIrCompiler<TCompilationOutput>> compilerFactory,
        Func<IServiceProvider, IExecutor<TCompilationOutput>> executorFactory)
    {
        if (provider == null)
            Thrower.ArgumentNull(nameof(provider));

        if (configuration == null)
            Thrower.ArgumentNull(nameof(configuration));

        if (compilerFactory == null)
            Thrower.ArgumentNull(nameof(compilerFactory));

        if (executorFactory == null)
            Thrower.ArgumentNull(nameof(executorFactory));

        var capabilitySetFactory = provider.GetRequiredService<IIntrinsicCapabilitySetFactory>();
        var backendOptimizers = configuration.OptimizerTypes
            .Select(type => (IIRProcessingModule)provider.GetRequiredService(type))
            .ToList();

        return new BasicCoreImpl<TCompilationOutput>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => compilerFactory(provider, configuration),
            () => executorFactory(provider),
            provider.GetServices<IFrontendCoreModule>().ToList(),
            backendOptimizers,
            [],
            capabilitySetFactory);
    }
}
