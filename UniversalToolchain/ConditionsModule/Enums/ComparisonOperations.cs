using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;

namespace ConditionsModule;

[AutoRegisterService]
public class ComparisonOperations : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new(@"\=\=", "Equal"),
        new(@"\!\=", "NotEqual"),
        new(@"\>", "Greater"),
        new(@"\<", "Less"),
        new(@"\>\=", "GreaterOrEqual", Priority: -1f),
        new(@"\<\=", "LessOrEqual", Priority: -1f)
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> NodeCreatorRegistrations =
    [
        new(-20f, new ComparisonNodeCreator("Equal")),
        new(-20f, new ComparisonNodeCreator("NotEqual")),
        new(-20f, new ComparisonNodeCreator("Greater")),
        new(-20f, new ComparisonNodeCreator("Less")),
        new(-20f, new ComparisonNodeCreator("GreaterOrEqual")),
        new(-20f, new ComparisonNodeCreator("LessOrEqual"))
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(NodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new ComparisonVisitor());
}