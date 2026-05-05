using System.Reflection.Emit;
using BasicCore.ExecutorWrapper;
using BytecodeDynamicMethodsCompiler.Compilers;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistCilDialectBackendServiceProvider : DialectBackendRuntimeRegistrarBase<DynamicMethod>
{
    private readonly CilIntrinsicRegistry _intrinsicRegistry = new();

    public override DialectBackendId BackendId => WistDialectBackendIds.Cil;

    public override IReadOnlyList<string> SupportedIntrinsics => _intrinsicRegistry.SupportedIntrinsics;

    override protected void RegisterBackendDefaults(IServiceCollection services)
        => services.AddCompilerBackendDefaults();

    override protected AbstractMethodsCompilerImpl ResolveBackendCompiler(IServiceProvider provider)
        => provider.GetRequiredService<AbstractMethodsCompilerImpl>();

    override protected Func<IExecutor<DynamicMethod>> ResolveExecutorFactory(IServiceProvider provider)
        => provider.GetRequiredService<Func<IExecutor<DynamicMethod>>>();
}
