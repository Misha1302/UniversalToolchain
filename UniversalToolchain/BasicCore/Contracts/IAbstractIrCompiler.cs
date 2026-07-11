namespace BasicCore.Contracts;

public interface IAbstractIrCompiler<out TCompilationOutput>
{
    public IReadOnlyList<string> SupportedIntrinsics => CoreDefaultIntrinsicNames.Value;

    public TCompilationOutput Compile(IAbstractIR air, CompilationInput input);
}