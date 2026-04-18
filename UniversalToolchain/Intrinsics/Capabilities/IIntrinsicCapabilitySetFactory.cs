namespace BasicCore.Capabilities;

public interface IIntrinsicCapabilitySetFactory
{
    IIntrinsicCapabilitySet Create<TCompilationOutput>(IAbstractIrCompiler<TCompilationOutput> compiler);
}