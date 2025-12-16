// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace ScopesModule;

public class ScopesModuleImpl : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\(", LexemeType.CreateOrGet("OpenPar")));
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\)", LexemeType.CreateOrGet("ClosePar")));
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-100_000f, new ScopesCreator());
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new ScopeAstVisitor());
    }
}