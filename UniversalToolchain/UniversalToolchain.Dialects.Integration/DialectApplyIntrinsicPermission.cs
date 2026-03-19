using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Represents one intrinsic permission in apply-mode output.
/// </summary>
public sealed class DialectApplyIntrinsicPermission
{
    public DialectApplyIntrinsicPermission(string canonicalId, DialectBackendSelector target)
    {
        if (string.IsNullOrWhiteSpace(canonicalId))
            Thrower.Argument(nameof(canonicalId), "Intrinsic canonical identifier must not be empty.");

        CanonicalId = canonicalId;
        Target = target;
    }

    public string CanonicalId { get; }

    public string Name => CanonicalId;

    public DialectBackendSelector Target { get; }
}
