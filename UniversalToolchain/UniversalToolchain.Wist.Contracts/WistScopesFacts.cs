using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.Wist.Contracts;

[Obsolete("Use ScopesModule.Contracts.ScopesFacts. Wist.Contracts keeps this only as a compatibility alias for older consumers.")]
public static class WistScopesFacts
{
    public static CompilerFactId ScopesLocalsBound { get; } = new("wist.scopes.locals-bound");
}
