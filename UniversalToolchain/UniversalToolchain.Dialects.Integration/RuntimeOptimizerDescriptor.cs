using BasicCore.Contracts;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Describes one explicitly registered optimizer entity resolvable by dialect optimizer name.
/// </summary>
public sealed class RuntimeOptimizerDescriptor
{
    public RuntimeOptimizerDescriptor(string name, Type implementationType)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Optimizer descriptor name must not be empty.");

        if (implementationType == null)
            Thrower.ArgumentNull(nameof(implementationType));

        if (!typeof(IIRProcessingModule).IsAssignableFrom(implementationType))
            Thrower.Argument(nameof(implementationType), "Optimizer type must implement IIRProcessingModule.");

        Name = name;
        ImplementationType = implementationType;
    }

    public string Name { get; }

    public Type ImplementationType { get; }
}
