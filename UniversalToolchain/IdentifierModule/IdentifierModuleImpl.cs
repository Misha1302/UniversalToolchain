namespace IdentifierModule;

[UniversalToolchain.Dialects.Abstractions.DialectModuleAlias("Identifier")]
[UniversalToolchain.Dialects.Abstractions.DialectRuntimeExport("wist", "FrontendModule", "Identifier")]
[AutoRegisterService]
public class IdentifierModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(
            @"[@a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?",
            "Identifier",
            Priority: 100f
        )
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);
}