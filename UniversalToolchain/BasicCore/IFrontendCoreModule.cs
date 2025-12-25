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

    string ProcessText(string curCode)
    {
        return curCode;
    }

    List<LexemeValue> ProcessLexemes(List<LexemeValue> current)
    {
        return current;
    }

    AstNode ProcessAst(AstNode astRoot)
    {
        return astRoot;
    }


    Bytecode ProcessBytecode(Bytecode current)
    {
        return current;
    }

    void InitTranslator(IBytecodeTranslator translator)
    {
    }
}