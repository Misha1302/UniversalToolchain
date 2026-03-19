using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectBackendDirective
{
    public DialectBackendDirective(DialectBackendId backend, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(backend.Value))
            Thrower.Argument(nameof(backend), "Backend identifier must not be empty.");

        Backend = backend;
        Enabled = enabled;
    }

    public DialectBackendId Backend { get; }

    public bool Enabled { get; }
}
