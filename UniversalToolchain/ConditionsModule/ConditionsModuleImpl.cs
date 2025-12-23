using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace ConditionsModule;

public class ConditionsModuleImpl : IFrontendCoreModule
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
        parser.Configuration.NodeCreators.Add(15f, new IfNodeCreator());
        parser.Configuration.NodeCreators.Add(15f, new ElifNodeCreator());
        parser.Configuration.NodeCreators.Add(15f, new ElseNodeCreator());
        parser.Configuration.NodeCreators.Add(16f, new CondNodesCombiner());
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