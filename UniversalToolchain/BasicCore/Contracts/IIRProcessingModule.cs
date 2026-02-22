using IntermediateRepresentationAbstractions;

namespace BasicCore;

// ReSharper disable once InconsistentNaming
public interface IIRProcessingModule
{
    IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler) => current;


    void InitMethodsTranslator(IAbstractMethodsTranslator methodsTranslator)
    {
    }
}