using UniversalToolchain.ModuleContracts;

namespace ScopesModule.Contracts;

public static class ScopesEffects
{
    public static CompilerEffectId BindScopeLocals { get; } = new("wist.scopes.effect.bind-scope-locals");
}
