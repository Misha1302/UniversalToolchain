using System.Reflection.Emit;
using BasicCore.ExecutorWrapper;
using BytecodeDynamicMethodsCompiler.Compilers;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.ServiceCollection;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class WistCilDialectBackendServiceProvider : DialectBackendRuntimeRegistrarBase<DynamicMethod>
{
    public override DialectBackendId BackendId => WistDialectBackendIds.Cil;

    public override IReadOnlyList<string> SupportedIntrinsics => AbstractMethodsCompilerImpl.SupportedIntrinsicIds;

    protected override void RegisterBackendDefaults(IServiceCollection services)
        => services.AddCompilerBackendDefaults();

    protected override AbstractMethodsCompilerImpl ResolveBackendCompiler(IServiceProvider provider)
        => provider.GetRequiredService<AbstractMethodsCompilerImpl>();

    protected override Func<IExecutor<DynamicMethod>> ResolveExecutorFactory(IServiceProvider provider)
        => provider.GetRequiredService<Func<IExecutor<DynamicMethod>>>();
}
