using AbstractIrConverters;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistInterpreterDialectBackendServiceProvider : IDialectBackendRuntimeRegistrar
{
    private static readonly IReadOnlyList<string> _supportedIntrinsics = new AbstractIrToAbstractIrStub().SupportedIntrinsics
        .Distinct(StringComparer.Ordinal)
        .OrderBy(x => x, StringComparer.Ordinal)
        .ToList();

    public DialectBackendId BackendId => WistDialectBackendIds.Interpreter;

    public IReadOnlyList<string> SupportedIntrinsics => _supportedIntrinsics;

    public void RegisterRuntime(IServiceCollection services, DialectBackendRuntimeConfiguration configuration)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        if (configuration == null)
            Thrower.ArgumentNull(nameof(configuration));

        services.AddTransient<ICoreRunnable>(provider => CreateCore(provider, configuration));
        services.AddTransient<ICoreOptimizedRunnable>(provider => CreateCore(provider, configuration));
        services.AddTransient<IExecutableGiver<IAbstractIR>>(provider => CreateCore(provider, configuration));
        services.AddTransient(provider => new WistDialectBackendRuntime(configuration.BackendDescriptor, CreateCore(provider, configuration)));
    }

    private static BasicCoreImpl<IAbstractIR> CreateCore(IServiceProvider provider, DialectBackendRuntimeConfiguration configuration)
    {
        return new BasicCoreImpl<IAbstractIR>(
            provider.GetRequiredService<Func<ILexer>>(),
            provider.GetRequiredService<Func<IParser>>(),
            provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
            provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
            () => new DialectIntrinsicPolicyCompiler<IAbstractIR>(
                provider.GetRequiredService<AbstractIrToAbstractIrStub>(),
                configuration.AllowedIntrinsics,
                configuration.ForbiddenIntrinsics,
                configuration.HasExplicitAllowList),
            provider.GetRequiredService<Func<IExecutor<IAbstractIR>>>(),
            provider.GetServices<IFrontendCoreModule>().ToList(),
            provider.GetServices<IIRProcessingModule>().ToList(),
            []);
    }
}
