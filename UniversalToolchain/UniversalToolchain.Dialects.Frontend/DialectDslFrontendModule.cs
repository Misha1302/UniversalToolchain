using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslFrontendModule : IFrontendCoreModule
{
    private readonly DialectDslRegistry _registry;

    public DialectDslFrontendModule(DialectDslRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public DialectDslRegistry Registry => _registry;

    public void InitLexer(ILexer lexer)
    {
        if (lexer == null)
        {
            Thrower.ArgumentNull(nameof(lexer));
        }

        lexer.AddLexemes(DialectDslLexemeRegistry.CreateRegistrations(_registry));
    }

    public void InitParser(IParser parser)
    {
        if (parser == null)
        {
            Thrower.ArgumentNull(nameof(parser));
        }

        parser.AddNodeCreators(DialectDslParserNodeRegistry.CreateRegistrations(_registry));
    }

    public AstNode ProcessAst(AstNode astRoot)
    {
        return DialectAstPipelineValidator.Validate(astRoot, _registry);
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        if (translator == null)
        {
            Thrower.ArgumentNull(nameof(translator));
        }

        translator.AddVisitors(new DialectAstToBytecodeVisitor(_registry));
    }
}
