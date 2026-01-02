using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule;

[AutoRegisterService]
public class ArithmeticModuleImpl : IFrontendCoreModule
{
    public static readonly IReadOnlyList<string> Ops = ["Addition", "Substraction", "Multiplication", "Division"];

    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\+", ExtensibleEnum<LexemeTag>.CreateOrGet("Addition")));
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\-", ExtensibleEnum<LexemeTag>.CreateOrGet("Substraction")));
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\*", ExtensibleEnum<LexemeTag>.CreateOrGet("Multiplication")));
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\/", ExtensibleEnum<LexemeTag>.CreateOrGet("Division")));
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-31, new MultiplicationOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-31, new DivisionOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-30, new AdditionOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-30, new SubstractionOperationNodeCreator());
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new ArithmeticAstVisitor());
    }
}