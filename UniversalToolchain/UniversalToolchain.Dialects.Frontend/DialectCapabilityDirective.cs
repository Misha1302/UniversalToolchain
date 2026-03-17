using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectCapabilityDirective
{
    public DialectCapabilityDirective(string name, bool value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Thrower.Argument(nameof(name), "Capability name must not be empty.");
        }

        Name = name;
        Value = value;
    }

    public string Name { get; }

    public bool Value { get; }
}
