using BasicCore.Contracts;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Describes one explicitly registered runtime module that can be resolved by dialect module name.
/// </summary>
public sealed class RuntimeModuleDescriptor
{
    public RuntimeModuleDescriptor(string name, Type implementationType)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Module descriptor name must not be empty.");

        if (implementationType == null)
            Thrower.ArgumentNull(nameof(implementationType));

        if (!typeof(IFrontendCoreModule).IsAssignableFrom(implementationType) &&
            !typeof(IIRProcessingModule).IsAssignableFrom(implementationType))
            Thrower.Argument(nameof(implementationType), "Module type must implement IFrontendCoreModule or IIRProcessingModule.");

        Name = name;
        ImplementationType = implementationType;
    }

    public string Name { get; }

    public Type ImplementationType { get; }

    public bool IsFrontendModule => typeof(IFrontendCoreModule).IsAssignableFrom(ImplementationType);

    public bool IsIrProcessingModule => typeof(IIRProcessingModule).IsAssignableFrom(ImplementationType);
}