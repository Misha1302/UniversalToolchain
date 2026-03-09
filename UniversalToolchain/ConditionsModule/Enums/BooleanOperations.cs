using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using ConditionsModule.Creators;
using ConditionsModule.Visitors;

namespace ConditionsModule.Enums;

[AutoRegisterService]
public class BooleanOperations : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new("true", "True"),
        new("false", "False"),
        new("and", "And"),
        new("or", "Or"),
        new("not", "Not")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-100f, new BooleanNodeCreator("True", BooleanNodeCreator.BooleanStatementType.Constant)),
        new(-100f, new BooleanNodeCreator("False", BooleanNodeCreator.BooleanStatementType.Constant)),
        new(-11f, new BooleanNodeCreator("Not", BooleanNodeCreator.BooleanStatementType.UnaryOperation)),
        new(-10f, new BooleanNodeCreator("And", BooleanNodeCreator.BooleanStatementType.BinaryOperation)),
        new(-9f, new BooleanNodeCreator("Or", BooleanNodeCreator.BooleanStatementType.BinaryOperation))
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new BooleanVisitor());
}