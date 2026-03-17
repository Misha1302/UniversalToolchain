using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Represents one intrinsic permission in apply-mode output.
/// </summary>
public sealed class DialectApplyIntrinsicPermission
{
    public DialectApplyIntrinsicPermission(string name, DialectBackendTarget target)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Intrinsic name must not be empty.");

        if (!Enum.IsDefined(target))
            Thrower.Argument(nameof(target), "Backend target is not defined.");

        Name = name;
        Target = target;
    }

    public string Name { get; }

    public DialectBackendTarget Target { get; }
}
