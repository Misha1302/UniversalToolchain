using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace ScopesModule;

[AutoRegisterService]
public class ScopesModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\(", LexemeType.CreateOrGet("OpenPar")));
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\)", LexemeType.CreateOrGet("ClosePar")));
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-100_000f, new ScopesCreator());
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new ScopeAstVisitor());
    }
}