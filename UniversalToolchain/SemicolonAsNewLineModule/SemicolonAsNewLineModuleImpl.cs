using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Abstractions;

namespace SemicolonAsNewLineModule;

[DialectModuleAlias("SemicolonAsNewLine")]
[DialectCapabilityProvider(typeof(SemicolonAsNewLineCapabilityProvider))]
[DialectRuntimeExport("FrontendModule", "SemicolonAsNewLine")]
[AutoRegisterService]
public class SemicolonAsNewLineModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryUncheckedAddPattern(
            new LexemePattern(";", ExtensibleEnum<LexemeTag>.CreateOrGet("NewLine"))
        );
    }
}
