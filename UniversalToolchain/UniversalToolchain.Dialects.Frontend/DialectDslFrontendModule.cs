using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslFrontendModule : IFrontendCoreModule
{
    private readonly DialectDefinitionSliceParser _sliceParser = new();

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
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        _sliceParser.Parse(astRoot);
        return astRoot;
    }

}
