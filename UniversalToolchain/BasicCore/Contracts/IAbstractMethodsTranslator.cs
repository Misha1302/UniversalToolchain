using BasicCore.TranslatorWrapper;
using IntermediateRepresentationAbstractions;

namespace BasicCore;

public interface IAbstractMethodsTranslator
{
    public IAbstractIR Translate(Bytecode bytecode);
}