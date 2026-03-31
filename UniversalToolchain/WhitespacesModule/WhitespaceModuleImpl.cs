using BasicCore.Registration;
using UniversalToolchain.Dialects.Abstractions;

namespace WhitespacesModule;

[DialectModuleAlias("Whitespaces")]
[DialectRuntimeExport("FrontendModule", "Whitespaces")]
[AutoRegisterService]
public class WhitespaceModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"[ \t\r\n]+", "Whitespace", Ignore: true)
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);
}
