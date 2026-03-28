using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Parsing;

/// <summary>
///     Represents one backend enable or disable directive.
/// </summary>
public sealed class BackendDirectiveSyntax
{
    public BackendDirectiveSyntax(DialectBackendId backend, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(backend.Value))
            Thrower.Argument(nameof(backend), "Backend directive must contain a backend identifier.");

        Backend = backend;
        Enabled = enabled;
    }

    public DialectBackendId Backend { get; }

    public bool Enabled { get; }
}