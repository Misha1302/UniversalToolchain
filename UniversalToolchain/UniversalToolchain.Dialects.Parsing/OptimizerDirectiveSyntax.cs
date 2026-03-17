using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
/// Represents one optimizer enable/disable directive with backend scope.
/// </summary>
public sealed class OptimizerDirectiveSyntax
{
    public OptimizerDirectiveSyntax(string name, bool enabled, DialectBackendTarget target)
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
