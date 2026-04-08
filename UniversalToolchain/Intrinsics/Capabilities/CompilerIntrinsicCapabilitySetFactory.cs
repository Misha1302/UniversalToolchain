using BasicCore.Contracts;

namespace UniversalToolchain.Intrinsics.Capabilities;

public sealed class CompilerIntrinsicCapabilitySetFactory : IIntrinsicCapabilitySetFactory
{
    public IIntrinsicCapabilitySet Create<TCompilationOutput>(IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        return new CompilerIntrinsicCapabilityAdapter<TCompilationOutput>(compiler);
    }
}
