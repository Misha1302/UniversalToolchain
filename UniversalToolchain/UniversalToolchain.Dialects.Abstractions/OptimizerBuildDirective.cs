namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Represents a normalized optimizer directive in a build plan.
/// </summary>
public sealed class OptimizerBuildDirective
{
    public OptimizerBuildDirective(string name, bool enabled, DialectBackendId target)
        : this(name, enabled, DialectBackendSelector.For(target))
    {
    }

    public OptimizerBuildDirective(string name, bool enabled, DialectBackendSelector target)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Optimizer name must not be empty.");

        Name = name;
        Enabled = enabled;
        Target = target;
    }

    public string Name { get; }

    public bool Enabled { get; }

    public DialectBackendSelector Target { get; }
}
