using BasicCore.TranslatorWrapper;
using IntermediateRepresentationAbstractions;

namespace BasicCore.Contracts;

public interface IAbstractMethodsTranslator
{
    public IAbstractIR Translate(Bytecode bytecode);
}