using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Represents a normalized intrinsic directive in a build plan.
/// </summary>
public sealed class IntrinsicBuildDirective
{
    public IntrinsicBuildDirective(string name, bool allowed, DialectBackendId target)
        : this(name, allowed, DialectBackendSelector.For(target))
    {
    }

    public IntrinsicBuildDirective(string name, bool allowed, DialectBackendSelector target)
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