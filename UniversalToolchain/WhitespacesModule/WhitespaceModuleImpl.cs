using BasicCore;
using BasicCore.LexerWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace WhitespacesModule;

public class WhitespaceModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        var lexemes = (List<LexemePattern>)
        [
            new LexemePattern(" ", LexemeType.CreateOrGet("Space")),
            new LexemePattern(@"\n", LexemeType.CreateOrGet("NewLine"))
        ];

        foreach (var lexemePattern in lexemes) lexer.Configuration.TryAddPattern(lexemePattern);
        lexer.Configuration.LexemesToIgnore.AddRange(lexemes.Select(x => x.LexemeType));
    }
}