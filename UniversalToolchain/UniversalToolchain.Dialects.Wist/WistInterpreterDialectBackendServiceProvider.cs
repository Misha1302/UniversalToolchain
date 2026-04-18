using AbstractIrConverters;
using BasicCore.ExecutorWrapper;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistInterpreterDialectBackendServiceProvider : DialectBackendRuntimeRegistrarBase<IAbstractIR>
{
    public override DialectBackendId BackendId => WistDialectBackendIds.Interpreter;

    public override IReadOnlyList<string> SupportedIntrinsics => AbstractIrToAbstractIrStub.SupportedIntrinsicIds;

    override protected void RegisterBackendDefaults(IServiceCollection services)
        => services.AddInterpreterBackendDefaults();

    override protected AbstractIrToAbstractIrStub ResolveBackendCompiler(IServiceProvider provider)
        => provider.GetRequiredService<AbstractIrToAbstractIrStub>();

    override protected Func<IExecutor<IAbstractIR>> ResolveExecutorFactory(IServiceProvider provider)
        => provider.GetRequiredService<Func<IExecutor<IAbstractIR>>>();
}