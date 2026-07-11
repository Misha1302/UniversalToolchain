using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.Wist.Contracts;

[Obsolete("Use IdentifierModule.Contracts.IdentifierFacts. Wist.Contracts keeps this only as a compatibility alias for older consumers.")]
public static class WistIdentifierFacts
{
    public static CompilerFactId IdentifiersAvailable { get; } = new("wist.identifiers.available");
}
