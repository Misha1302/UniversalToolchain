using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace EqualityModule;

public class EqualityModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\=", LexemeType.CreateOrGet("Equality")), priority: 100
        );
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(10f, new ValuesSetNodeCreator());
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new EqualityAstVisitor());
    }
}