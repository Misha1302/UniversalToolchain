namespace BasicCore.Capabilities;

public sealed class CompilerIntrinsicCapabilitySetFactory : IIntrinsicCapabilitySetFactory
{
    public IIntrinsicCapabilitySet Create<TCompilationOutput>(IAbstractIrCompiler<TCompilationOutput> compiler) => new CompilerIntrinsicCapabilityAdapter<TCompilationOutput>(compiler);
}