using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;

namespace BasicCore;

public interface IFrontendCoreModule
{
    void InitLexer(ILexer lexer)
    {
    }

    void InitParser(IParser parser)
    {
    }

    string ProcessText(string curCode) => curCode;

    List<LexemeValue> ProcessLexemes(List<LexemeValue> current) => current;

    AstNode ProcessAst(AstNode astRoot) => astRoot;


    Bytecode ProcessBytecode(Bytecode current) => current;

    void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
    }
}