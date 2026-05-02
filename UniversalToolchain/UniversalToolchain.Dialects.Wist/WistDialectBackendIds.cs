using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Stable identifiers for backends shipped by the Wist integration package.
///     This is not a global backend registry; external backends are identified by their runtime manifests.
/// </summary>
public static class WistDialectBackendIds
{
    public static DialectBackendId Cil { get; } = new("cil");

    public static DialectBackendId Interpreter { get; } = new("interpreter");
}
