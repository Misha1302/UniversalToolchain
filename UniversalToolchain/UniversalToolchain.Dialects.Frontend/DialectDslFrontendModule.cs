using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;
using UniversalToolchain.Dialects.Frontend.Composition;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslFrontendModule : IFrontendCoreModule
{
    public DialectDslFrontendModule(DialectDslRegistry registry)
    {
        registry = registry.ArgNotNull();

        Registry = registry;
    }

    public DialectDslRegistry Registry { get; }

    public void InitLexer(ILexer lexer)
    {
        lexer = lexer.ArgNotNull();

        lexer.AddLexemes(DialectDslLexemeRegistry.CreateRegistrations(Registry));
    }

    public void InitParser(IParser parser)
    {
        parser = parser.ArgNotNull();

        parser.AddNodeCreators(DialectDslParserNodeRegistry.CreateRegistrations(Registry));
    }

    public AstNode ProcessAst(AstNode astRoot) => DialectAstPipelineValidator.Validate(astRoot, Registry);

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator = translator.ArgNotNull();

        translator.AddVisitors(new DialectAstToBytecodeVisitor(Registry));
    }
}