using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.TranslatorWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace NumbersModule;

public class NumbersModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"[+-]?\d+(\.\d+)?([eE][+-]?\d+)?",
                LexemeType.CreateOrGet("Number"))
        );
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new NumberAstVisitor());
    }
}