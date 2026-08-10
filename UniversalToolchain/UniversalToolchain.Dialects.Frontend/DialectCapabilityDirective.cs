using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectCapabilityDirective
{
    public DialectCapabilityDirective(string name, bool value)
        : this(name, value, null)
    {
    }

    public DialectCapabilityDirective(string name, bool value, DialectSourceLocation? sourceLocation)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Capability name must not be empty.");

        Name = name;
        Value = value;
        SourceLocation = sourceLocation;
    }

    public string Name { get; }

    public bool Value { get; }

    public DialectSourceLocation? SourceLocation { get; }
}
