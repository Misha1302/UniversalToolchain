using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace CommentsModule;

[AutoRegisterService]
public class CommentsModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"//[^\n]*", ExtensibleEnum<LexemeTag>.CreateOrGet("SingleLineComment")),
            true,
            -100
        );
        
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"/\*[\s\S]*?\*/", ExtensibleEnum<LexemeTag>.CreateOrGet("MultiLineComment")),
            true,
            -100
        );
    }


    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        // Комментарии не попадают в AST
    }

    // Метод ProcessText для предварительной обработки (опционально)
    public string ProcessText(string curCode)
    {
        // Можно добавить предварительную обработку для удаления комментариев,
        // но лучше оставить это лексеру
        return curCode;
    }
}