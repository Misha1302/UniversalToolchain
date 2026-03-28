using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public readonly record struct RuntimeComponentId
{
    public RuntimeComponentId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Thrower.Argument(nameof(value), "Runtime component id must not be empty.");

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
