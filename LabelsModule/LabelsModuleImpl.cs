// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace LabelsModule;

public class LabelsModuleImpl : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(":", LexemeType.CreateOrGet("Colon"))
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern("goto", LexemeType.CreateOrGet("Goto")),
            insertToStart: true
        );
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-2, new LabelsNodeCreator());
        parser.Configuration.NodeCreators.Add(-2, new GotoNodeCreator());
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new LabelsVisitor());
        translator.Configuration.Visitors.Add(new GotoVisitor());
    }
}