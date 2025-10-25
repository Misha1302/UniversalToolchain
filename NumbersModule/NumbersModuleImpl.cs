// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.TranslatorWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace NumbersModule;

public class NumbersModuleImpl : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.Patterns.Add(new LexemePattern(@"(\+|-)?([0-9]+)(\.[0-9]+)?",
            LexemeType.CreateOrGet("Number")));
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new NumberAstVisitor());
    }
}