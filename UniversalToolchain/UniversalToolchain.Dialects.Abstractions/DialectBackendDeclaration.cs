using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

public abstract class DialectBackendDeclaration
{
    public DialectBackendId GetBackendId()
    {
        var backendId = BackendId;
        if (string.IsNullOrWhiteSpace(backendId.Value))
            Thrower.InvalidOpEx($"Backend declaration '{GetType().FullName}' produced an empty backend identifier.");

        return backendId;
    }

    public abstract DialectBackendId BackendId { get; }
}
