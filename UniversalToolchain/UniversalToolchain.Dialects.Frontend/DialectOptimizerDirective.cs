using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectOptimizerDirective
{
    public DialectOptimizerDirective(string name, bool enabled, DialectBackendTarget target)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Thrower.Argument(nameof(name), "Optimizer name must not be empty.");
        }

        Name = name;
        Enabled = enabled;
        Target = target;
    }

    public string Name { get; }

    public bool Enabled { get; }

    public DialectBackendTarget Target { get; }
}
