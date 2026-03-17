using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Represents a normalized optimizer directive in a build plan.
/// </summary>
public sealed class OptimizerBuildDirective
{
    public OptimizerBuildDirective(string name, bool enabled, DialectBackendTarget target)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Optimizer name must not be empty.");

        if (!Enum.IsDefined(target))
            Thrower.Argument(nameof(target), "Backend target is not defined.");

        Name = name;
        Enabled = enabled;
        Target = target;
    }

    public string Name { get; }

    public bool Enabled { get; }

    public DialectBackendTarget Target { get; }
}
