using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslFrontendModule : IFrontendCoreModule
{
    public DialectDslFrontendModule(DialectDslRegistry registry)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public DialectDslRegistry Registry { get; }

    public void InitLexer(ILexer lexer)
    {
        if (lexer == null)
            Thrower.ArgumentNull(nameof(lexer));

        lexer.AddLexemes(DialectDslLexemeRegistry.CreateRegistrations(Registry));
    }

    public void InitParser(IParser parser)
    {
        if (parser == null)
            Thrower.ArgumentNull(nameof(parser));

        parser.AddNodeCreators(DialectDslParserNodeRegistry.CreateRegistrations(Registry));
    }

    public AstNode ProcessAst(AstNode astRoot) => DialectAstPipelineValidator.Validate(astRoot, Registry);

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        if (translator == null)
            Thrower.ArgumentNull(nameof(translator));

        translator.AddVisitors(new DialectAstToBytecodeVisitor(Registry));
    }
}