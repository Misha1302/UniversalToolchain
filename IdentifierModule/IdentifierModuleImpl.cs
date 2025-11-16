// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.LexerWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace IdentifierModule;

public class IdentifierModuleImpl : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(
                @"[@a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?",
                LexemeType.CreateOrGet("Identifier")
            ),
            priority: 100
        );
    }
}