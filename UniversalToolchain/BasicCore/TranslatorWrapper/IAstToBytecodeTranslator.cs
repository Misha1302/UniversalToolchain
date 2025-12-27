using BasicCore.ParserWrapper;

namespace BasicCore.TranslatorWrapper;

public interface IAstToBytecodeTranslator
{
    BytecodeTranslatorConfiguration Configuration { get; }
    public Bytecode Translate(AstNode root);
}