using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectOptimizerDirective
{
    public DialectOptimizerDirective(string name, bool enabled, DialectBackendId target)
        : this(name, enabled, DialectBackendSelector.For(target))
    {
    }

    public DialectOptimizerDirective(string name, bool enabled, DialectBackendSelector target)
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
