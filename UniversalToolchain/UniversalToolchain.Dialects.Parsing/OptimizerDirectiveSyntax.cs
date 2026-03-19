using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
///     Represents one optimizer enable or disable directive with backend scope.
/// </summary>
public sealed class OptimizerDirectiveSyntax
{
    public OptimizerDirectiveSyntax(string name, bool enabled, DialectBackendId target)
        : this(name, enabled, DialectBackendSelector.For(target))
    {
    }

    public OptimizerDirectiveSyntax(string name, bool enabled, DialectBackendSelector target)
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
