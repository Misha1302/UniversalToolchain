using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Describes one explicitly available intrinsic capability.
/// </summary>
public sealed class RuntimeIntrinsicDescriptor
{
    public RuntimeIntrinsicDescriptor(string name, DialectBackendTarget target)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Intrinsic descriptor name must not be empty.");

        if (!Enum.IsDefined(target))
            Thrower.Argument(nameof(target), "Backend target is not defined.");

        Name = name;
        Target = target;
    }

    public string Name { get; }

    public DialectBackendTarget Target { get; }
}
