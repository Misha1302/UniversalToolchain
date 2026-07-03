using UniversalToolchain.ModuleContracts;

namespace IdentifierModule.Contracts;

public static class IdentifierEffects
{
    public static CompilerEffectId RegisterIdentifierSyntax { get; } = new("wist.identifiers.effect.register-identifier-syntax");
}
