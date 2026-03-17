using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslFrontendModule : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        if (lexer == null)
        {
            Thrower.ArgumentNull(nameof(lexer));
        }

        lexer.AddLexemes(DialectDslLexemeRegistry.Registrations);
    }

    public void InitParser(IParser parser)
    {
        if (parser == null)
        {
            Thrower.ArgumentNull(nameof(parser));
        }

        parser.AddNodeCreators(DialectDslParserNodeRegistry.Registrations);
    }

    public AstNode ProcessAst(AstNode astRoot)
    {
        return DialectAstPipelineValidator.Validate(astRoot);
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        if (translator == null)
        {
            Thrower.ArgumentNull(nameof(translator));
        }

        translator.AddVisitors(new DialectAstToBytecodeVisitor());
    }
}
