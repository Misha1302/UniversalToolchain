using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.Registration;

namespace IdentifierModule;

[AutoRegisterService]
public class IdentifierModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> LexemeRegistrations =
    [
        new(
            @"[@a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?",
            "Identifier",
            Priority: 100f
        )
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(LexemeRegistrations);
}