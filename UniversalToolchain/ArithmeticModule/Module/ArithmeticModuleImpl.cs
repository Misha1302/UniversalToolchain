using UniversalToolchain.Capabilities.Abstractions;

namespace ArithmeticModule.Module;
[DialectCapabilityProvider(typeof(ArithmeticCapabilityProvider))]
[DialectComponentContract("FrontendModule", "Arithmetic")]
[AutoRegisterService]
public class ArithmeticModuleImpl : IFrontendCoreModule
{
    public static readonly IReadOnlyList<string> Ops = ["Addition", "Subtraction", "Multiplication", "Division"];

    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"\+", "Addition"),
        new(@"\-", "Subtraction"),
        new(@"\*", "Multiplication"),
        new(@"\/", "Division")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-40f, new UnaryMinusOperationNodeCreator()),
        new(-31f, new MultiplicationOperationNodeCreator()),
        new(-31f, new DivisionOperationNodeCreator()),
        new(-30f, new AdditionOperationNodeCreator()),
        new(-30f, new SubtractionOperationNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) =>
        translator.AddVisitors(new UnaryMinusAstVisitor(), new ArithmeticAstVisitor());
}