using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
///     Represents one intrinsic allow or forbid directive with backend scope.
/// </summary>
public sealed class IntrinsicDirectiveSyntax
{
    public IntrinsicDirectiveSyntax(string name, bool allowed, DialectBackendId target)
        : this(name, allowed, DialectBackendSelector.For(target))
    {
    }

    public IntrinsicDirectiveSyntax(string name, bool allowed, DialectBackendSelector target)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Intrinsic name must not be empty.");

        Name = name;
        Allowed = allowed;
        Target = target;
    }

    public string Name { get; }

    public bool Allowed { get; }

    public DialectBackendSelector Target { get; }
}