// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace ConditionsModule;

public class BooleanOperations : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(new LexemePattern("true", ExtensibleEnum<LexemeTag>.CreateOrGet("True")));
        lexer.Configuration.TryAddPattern(new LexemePattern("false", ExtensibleEnum<LexemeTag>.CreateOrGet("False")));
        lexer.Configuration.TryAddPattern(new LexemePattern("and", ExtensibleEnum<LexemeTag>.CreateOrGet("And")));
        lexer.Configuration.TryAddPattern(new LexemePattern("or", ExtensibleEnum<LexemeTag>.CreateOrGet("Or")));
        lexer.Configuration.TryAddPattern(new LexemePattern("not", ExtensibleEnum<LexemeTag>.CreateOrGet("Not")));
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-100f,
            new BooleanNodeCreator("True", BooleanNodeCreator.BooleanStatementType.Constant));
        parser.Configuration.NodeCreators.Add(-100f,
            new BooleanNodeCreator("False", BooleanNodeCreator.BooleanStatementType.Constant));
        parser.Configuration.NodeCreators.Add(-11f,
            new BooleanNodeCreator("Not", BooleanNodeCreator.BooleanStatementType.UnaryOperation));
        parser.Configuration.NodeCreators.Add(-10f,
            new BooleanNodeCreator("And", BooleanNodeCreator.BooleanStatementType.BinaryOperation));
        parser.Configuration.NodeCreators.Add(-9f,
            new BooleanNodeCreator("Or", BooleanNodeCreator.BooleanStatementType.BinaryOperation));
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new BooleanVisitor());
    }
}