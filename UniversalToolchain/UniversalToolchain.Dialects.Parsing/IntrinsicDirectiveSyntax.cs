using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
/// Represents one intrinsic allow/forbid directive with backend scope.
/// </summary>
public sealed class IntrinsicDirectiveSyntax
{
    public IntrinsicDirectiveSyntax(string name, bool allowed, DialectBackendTarget target)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Intrinsic name must not be empty.");

        if (!Enum.IsDefined(target))
            Thrower.Argument(nameof(target), "Backend target is not defined.");

        Name = name;
        Allowed = allowed;
        Target = target;
    }

    public string Name { get; }

    public bool Allowed { get; }

    public DialectBackendTarget Target { get; }
}
