using IntermediateRepresentationAbstractions;

namespace BasicCore;

public interface IAbstractIrCompiler<out TCompilationOutput>
{
    public TCompilationOutput Compile(IAbstractIR air);
}