using BasicCore.ParserWrapper;

namespace BasicCore.TranslatorWrapper;

public interface IBytecodeTranslator
{
    BytecodeTranslatorConfiguration Configuration { get; }
    public Bytecode Translate(AstNode root);
}