using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;

namespace CSharpInteropModule.Module;

[DialectModuleAlias("CSharpInterop")]
[DialectCapabilityProvider(typeof(CSharpInteropCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "CSharpInterop")]
[AutoRegisterService]
public class CSharpInteropModuleImpl : IFrontendCoreModule
{
    private readonly IMethodResolver _methodResolver;

    public CSharpInteropModuleImpl(IMethodResolver methodResolver)
    {
        _methodResolver = methodResolver.ArgNotNull();
    }

    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(",", LexemeType.CreateOrGet("Comma")),
            true
        );
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-1000, new CSharpFunctionCallsNodeCreator(_methodResolver));
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new CSharpFunctionCallsAstVisitor(_methodResolver));
    }
}
