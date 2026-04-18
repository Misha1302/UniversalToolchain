namespace UniversalToolchain.Intrinsics.Capabilities;

public interface IIntrinsicCapabilitySetFactory
{
    IIntrinsicCapabilitySet Create<TCompilationOutput>(IAbstractIrCompiler<TCompilationOutput> compiler);
}