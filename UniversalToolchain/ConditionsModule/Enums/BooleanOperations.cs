using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;

namespace ConditionsModule;

[AutoRegisterService]
public class BooleanOperations : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new("true", "True"),
        new("false", "False"),
        new("and", "And"),
        new("or", "Or"),
        new("not", "Not")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> NodeCreatorRegistrations =
    [
        new(-100f, new BooleanNodeCreator("True", BooleanNodeCreator.BooleanStatementType.Constant)),
        new(-100f, new BooleanNodeCreator("False", BooleanNodeCreator.BooleanStatementType.Constant)),
        new(-11f, new BooleanNodeCreator("Not", BooleanNodeCreator.BooleanStatementType.UnaryOperation)),
        new(-10f, new BooleanNodeCreator("And", BooleanNodeCreator.BooleanStatementType.BinaryOperation)),
        new(-9f, new BooleanNodeCreator("Or", BooleanNodeCreator.BooleanStatementType.BinaryOperation))
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(NodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new BooleanVisitor());
}
