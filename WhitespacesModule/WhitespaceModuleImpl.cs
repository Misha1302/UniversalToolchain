// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.LexerWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace WhitespacesModule;

public class WhitespaceModuleImpl : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        var lexemes = (List<LexemePattern>)
        [
            new LexemePattern(" ", LexemeType.CreateOrGet("Space")),
            new LexemePattern(@"\n", LexemeType.CreateOrGet("NewLine"))
        ];

        lexer.Configuration.Patterns.AddRange(lexemes);
        lexer.Configuration.LexemesToIgnore.AddRange(lexemes.Select(x => x.LexemeType));
    }
}