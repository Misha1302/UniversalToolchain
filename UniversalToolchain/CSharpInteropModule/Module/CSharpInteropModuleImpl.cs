using UniversalToolchain.Dialects.Abstractions;

namespace CSharpInteropModule.Module;

[DialectModuleAlias("CSharpInterop")]
[DialectRuntimeExport("FrontendModule", "CSharpInterop")]
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