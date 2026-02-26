using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace WhitespacesModule;

[AutoRegisterService]
public class WhitespaceModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        var lexemes = (List<LexemePattern>)
        [
            new LexemePattern(" ", LexemeType.CreateOrGet("Space")),
            new LexemePattern(@"\n", LexemeType.CreateOrGet("NewLine"))
        ];

        foreach (var lexemePattern in lexemes) lexer.Configuration.TryUncheckedAddPattern(lexemePattern);
        lexer.Configuration.LexemesToIgnore.AddRange(lexemes.Select(x => x.LexemeType));
    }
}