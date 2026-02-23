using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;

namespace ArithmeticModule;

[AutoRegisterService]
public class ArithmeticModuleImpl : IFrontendCoreModule
{
    public static readonly IReadOnlyList<string> Ops = ["Addition", "Substraction", "Multiplication", "Division"];

    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new(@"\+", "Addition"),
        new(@"\-", "Substraction"),
        new(@"\*", "Multiplication"),
        new(@"\/", "Division")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> NodeCreatorRegistrations =
    [
        new(-31f, new MultiplicationOperationNodeCreator()),
        new(-31f, new DivisionOperationNodeCreator()),
        new(-30f, new AdditionOperationNodeCreator()),
        new(-30f, new SubstractionOperationNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(NodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new ArithmeticAstVisitor());
}