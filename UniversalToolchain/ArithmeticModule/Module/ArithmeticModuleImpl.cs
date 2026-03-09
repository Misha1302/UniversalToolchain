using ArithmeticModule.Creators;
using ArithmeticModule.Visitors;
using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;

namespace ArithmeticModule.Module;

[AutoRegisterService]
public class ArithmeticModuleImpl : IFrontendCoreModule
{
    public static readonly IReadOnlyList<string> Ops = ["Addition", "Substraction", "Multiplication", "Division"];

    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"\+", "Addition"),
        new(@"\-", "Substraction"),
        new(@"\*", "Multiplication"),
        new(@"\/", "Division")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-31f, new MultiplicationOperationNodeCreator()),
        new(-31f, new DivisionOperationNodeCreator()),
        new(-30f, new AdditionOperationNodeCreator()),
        new(-30f, new SubstractionOperationNodeCreator())
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new ArithmeticAstVisitor());
}