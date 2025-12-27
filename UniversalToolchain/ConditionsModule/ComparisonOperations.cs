using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace ConditionsModule;

public class ComparisonOperations : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\=\=",
            ExtensibleEnum<LexemeTag>.CreateOrGet("Equal")));
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\!\=",
            ExtensibleEnum<LexemeTag>.CreateOrGet("NotEqual")));
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\>",
            ExtensibleEnum<LexemeTag>.CreateOrGet("Greater")));
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\<",
            ExtensibleEnum<LexemeTag>.CreateOrGet("Less")));
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\>\=",
            ExtensibleEnum<LexemeTag>.CreateOrGet("GreaterOrEqual")), priority: -1);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\<\=",
            ExtensibleEnum<LexemeTag>.CreateOrGet("LessOrEqual")), priority: -1);
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-20f, new ComparisonNodeCreator("Equal"));
        parser.Configuration.NodeCreators.Add(-20f, new ComparisonNodeCreator("NotEqual"));
        parser.Configuration.NodeCreators.Add(-20f, new ComparisonNodeCreator("Greater"));
        parser.Configuration.NodeCreators.Add(-20f, new ComparisonNodeCreator("Less"));
        parser.Configuration.NodeCreators.Add(-20f, new ComparisonNodeCreator("GreaterOrEqual"));
        parser.Configuration.NodeCreators.Add(-20f, new ComparisonNodeCreator("LessOrEqual"));
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new ComparisonVisitor());
    }
}