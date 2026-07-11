using AbstractIrConverters;
using BasicInterpreter.Contracts;
using BasicCore.Contracts;
using BasicCore.ExecutorWrapper;
using BasicCore.Execution;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.ModuleContracts;
using VariablesRuntime.Runtime;

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

    protected override IReadOnlyList<IBackendPipelineComponent> GetBackendPipelineComponents(
        IServiceProvider provider,
        DialectBackendRuntimeConfiguration configuration) =>
        [
            new ModuleContractBackendPipelineComponent(
                InterpreterBackendContractDescriptorProvider.Module.Value,
                [new InterpreterBackendContractDescriptorProvider(SupportedIntrinsics)]),
            new RuntimeProviderPolicyComponent([
                typeof(ExternalRuntimeCallProvider),
                typeof(VariablesRuntimeCallProvider)
            ])
        ];
}
