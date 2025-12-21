using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace CSharpInteropModule;

public class CSharpInteropModuleImpl : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(",", LexemeType.CreateOrGet("Comma")),
            true
        );
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-1000, new CSharpFunctionCallsNodeCreator());
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new CSharpFunctionCallsAstVisitor());
    }
}