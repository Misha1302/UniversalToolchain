using BasicCilCompiler.Contracts;
using BasicCilCompiler.Execution;
using BasicCore.Contracts;
using BasicCore.ExecutorWrapper;
using BasicCore.Execution;
using BytecodeDynamicMethodsCompiler.Compilers;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.ModuleContracts;
using VariablesRuntime.Runtime;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistCilDialectBackendServiceProvider : DialectBackendRuntimeRegistrarBase<CilCompilationOutput>
{
    private readonly CilIntrinsicRegistry _intrinsicRegistry = new();

    public override DialectBackendId BackendId => WistDialectBackendIds.Cil;

    public override IReadOnlyList<string> SupportedIntrinsics => _intrinsicRegistry.SupportedIntrinsics;

    override protected void RegisterBackendDefaults(IServiceCollection services)
        => services.AddCompilerBackendDefaults();

    override protected AbstractMethodsCompilerImpl ResolveBackendCompiler(IServiceProvider provider)
        => provider.GetRequiredService<AbstractMethodsCompilerImpl>();

    override protected Func<IExecutor<CilCompilationOutput>> ResolveExecutorFactory(IServiceProvider provider)
        => provider.GetRequiredService<Func<IExecutor<CilCompilationOutput>>>();

    protected override IReadOnlyList<IBackendPipelineComponent> GetBackendPipelineComponents(
        IServiceProvider provider,
        DialectBackendRuntimeConfiguration configuration) =>
        [
            new ModuleContractBackendPipelineComponent(
                CilBackendContractDescriptorProvider.Module.Value,
                [new CilBackendContractDescriptorProvider(SupportedIntrinsics)]),
            new RuntimeProviderPolicyComponent([
                typeof(ExternalRuntimeCallProvider),
                typeof(VariablesRuntimeCallProvider)
            ])
        ];
}
