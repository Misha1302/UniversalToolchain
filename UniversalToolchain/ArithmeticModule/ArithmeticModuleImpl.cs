using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule;

public class ArithmeticModuleImpl : ICoreModule
{
    public static IReadOnlyList<string> Ops => ["Addition", "Substraction", "Multiplication", "Division"];

    public void InitLexer(ILexer lexer)
    {
        var config = lexer.Configuration;
        config.TryAddPattern(new LexemePattern(@"\+", ExtensibleEnum<LexemeTag>.CreateOrGet(Ops[0])));
        config.TryAddPattern(new LexemePattern(@"\-", ExtensibleEnum<LexemeTag>.CreateOrGet(Ops[1])));
        config.TryAddPattern(new LexemePattern(@"\*", ExtensibleEnum<LexemeTag>.CreateOrGet(Ops[2])));
        config.TryAddPattern(new LexemePattern(@"\/", ExtensibleEnum<LexemeTag>.CreateOrGet(Ops[3])));
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-1, new MultiplicationOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-1, new DivisionOperationNodeCreator());

        parser.Configuration.NodeCreators.Add(0, new AdditionOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(0, new SubstractionOperationNodeCreator());
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new ArithmeticAstVisitor());
    }
}