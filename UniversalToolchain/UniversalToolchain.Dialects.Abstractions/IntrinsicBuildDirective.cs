using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Represents a normalized intrinsic directive in a build plan.
/// </summary>
public sealed class IntrinsicBuildDirective
{
    public IntrinsicBuildDirective(string name, bool allowed, DialectBackendTarget target)
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