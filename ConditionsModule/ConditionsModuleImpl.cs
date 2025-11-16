// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace ConditionsModule;

public class ConditionsModuleImpl : ICoreModule
{
    private IParser _parser = null!;

    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(new LexemePattern("if", ExtensibleEnum<LexemeTag>.CreateOrGet("If")));
        lexer.Configuration.TryAddPattern(new LexemePattern("elif", ExtensibleEnum<LexemeTag>.CreateOrGet("Elif")));
        lexer.Configuration.TryAddPattern(new LexemePattern("else", ExtensibleEnum<LexemeTag>.CreateOrGet("Else")));
    }

    public void InitParser(IParser parser)
    {
        _parser = parser;
        parser.Configuration.NodeCreators.Add(-5f, new IfNodeCreator());
        parser.Configuration.NodeCreators.Add(-5f, new ElifNodeCreator());
        parser.Configuration.NodeCreators.Add(-5f, new ElseNodeCreator());
        parser.Configuration.NodeCreators.Add(-4.9f, new CondNodesCombiner());
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new ConditionsVisitor());
    }

    public AstNode ProcessAst(AstNode astRoot)
    {
        _parser.ParseScope(astRoot, [new CondNodesCombiner()], _ => true);
        return astRoot;
    }
}