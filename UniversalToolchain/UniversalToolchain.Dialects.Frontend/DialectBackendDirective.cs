using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectBackendDirective
{
    public DialectBackendDirective(DialectBackendTarget backend, bool enabled)
    {
        Backend = backend;
        Enabled = enabled;
    }

    public DialectBackendTarget Backend { get; }

    public bool Enabled { get; }
}
