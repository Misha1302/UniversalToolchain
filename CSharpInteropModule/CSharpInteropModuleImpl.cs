// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace CSharpInteropModule;

public class CSharpInteropModuleImpl : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        if (lexer.Configuration.Patterns.All(x => x.LexemeType != ExtensibleEnum<LexemeTag>.CreateOrGet("Identifier")))
            lexer.Configuration.Patterns.Add(
                new LexemePattern(@"[a-zA-Z_][a-zA-Z_\.1-9]*", ExtensibleEnum<LexemeTag>.CreateOrGet("Identifier"))
            );

        if (lexer.Configuration.Patterns.All(x => x.LexemeType != ExtensibleEnum<LexemeTag>.CreateOrGet("Comma")))
        {
            lexer.Configuration.Patterns.Add(
                new LexemePattern(@",", ExtensibleEnum<LexemeTag>.CreateOrGet("Comma"))
            );
            lexer.Configuration.LexemesToIgnore.Add(ExtensibleEnum<LexemeTag>.CreateOrGet("Comma"));
        }
    }

    public void InitParser(IParser parser)
    {
        // TODO: add project configuration with priority injection
        parser.Configuration.NodeCreators.Add(-10, new CSharpFunctionCallsNodeCreator());
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new CSharpFunctionCallsAstVisitor());
    }
}