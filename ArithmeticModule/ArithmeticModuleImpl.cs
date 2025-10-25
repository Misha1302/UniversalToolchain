// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace ArithmeticModule;

// TODO: generic Math interfaces and impls
public class ArithmeticModuleImpl : ICoreModule
{
    public static IReadOnlyList<string> Ops => ["Addition", "Substraction", "Multiplication", "Division"];

    public void InitLexer(ILexer lexer)
    {
        var patterns = lexer.Configuration.Patterns;
        patterns.Add(new LexemePattern(@"\+", ExtensibleEnum<LexemeTag>.CreateOrGet(Ops[0])));
        patterns.Add(new LexemePattern(@"\-", ExtensibleEnum<LexemeTag>.CreateOrGet(Ops[1])));
        patterns.Add(new LexemePattern(@"\*", ExtensibleEnum<LexemeTag>.CreateOrGet(Ops[2])));
        patterns.Add(new LexemePattern(@"\/", ExtensibleEnum<LexemeTag>.CreateOrGet(Ops[3])));
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