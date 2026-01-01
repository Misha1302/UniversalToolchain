using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace LabelsModule;

[AutoRegisterService]
public class LabelsModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(":", LexemeType.CreateOrGet("Colon"))
        );
        lexer.Configuration.TryAddPattern(
            new LexemePattern("goto", LexemeType.CreateOrGet("Goto")),
            priority: -10f
        );
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-2, new LabelsNodeCreator());
        parser.Configuration.NodeCreators.Add(-2, new GotoNodeCreator());
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        var labelsSharedData = new LabelsSharedData();
        translator.Configuration.Visitors.Add(new LabelsVisitor(labelsSharedData));
        translator.Configuration.Visitors.Add(new GotoVisitor(labelsSharedData));
    }
}