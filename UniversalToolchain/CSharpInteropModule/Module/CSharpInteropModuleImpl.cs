using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using CSharpInteropModule.Creators;
using CSharpInteropModule.Visitors;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace CSharpInteropModule.Module;

[AutoRegisterService]
public class CSharpInteropModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(",", LexemeType.CreateOrGet("Comma")),
            true
        );
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-1000, new CSharpFunctionCallsNodeCreator());
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new CSharpFunctionCallsAstVisitor());
    }
}