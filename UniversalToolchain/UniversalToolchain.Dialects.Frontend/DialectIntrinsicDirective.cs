using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectIntrinsicDirective
{
    public DialectIntrinsicDirective(string name, bool allowed, DialectBackendId target)
        : this(name, allowed, DialectBackendSelector.For(target), null)
    {
    }

    public DialectIntrinsicDirective(string name, bool allowed, DialectBackendSelector target)
        : this(name, allowed, target, null)
    {
    }

    public DialectIntrinsicDirective(
        string name,
        bool allowed,
        DialectBackendSelector target,
        DialectSourceLocation? sourceLocation)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Intrinsic name must not be empty.");

        Name = name;
        Allowed = allowed;
        Target = target;
        SourceLocation = sourceLocation;
    }

    public string Name { get; }

    public bool Allowed { get; }

    public DialectBackendSelector Target { get; }

    public DialectSourceLocation? SourceLocation { get; }
}
