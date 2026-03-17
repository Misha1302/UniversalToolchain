using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
/// Represents one backend enable or disable directive.
/// </summary>
public sealed class BackendDirectiveSyntax
{
    public BackendDirectiveSyntax(DialectBackendTarget backend, bool enabled)
    {
        if (backend == DialectBackendTarget.Any)
            Thrower.Argument(nameof(backend), "Backend directive supports only interpreter or cil targets.");

        Backend = backend;
        Enabled = enabled;
    }

    public DialectBackendTarget Backend { get; }

    public bool Enabled { get; }
}
