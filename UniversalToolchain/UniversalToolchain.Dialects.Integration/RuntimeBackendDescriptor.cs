using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Describes one explicitly registered runtime backend.
/// </summary>
public sealed class RuntimeBackendDescriptor
{
    public RuntimeBackendDescriptor(DialectBackendTarget backendTarget, string runtimeName)
    {
        if (backendTarget == DialectBackendTarget.Any)
            Thrower.Argument(nameof(backendTarget), "Runtime backend descriptor must target interpreter or cil.");

        if (string.IsNullOrWhiteSpace(runtimeName))
            Thrower.Argument(nameof(runtimeName), "Runtime backend name must not be empty.");

        BackendTarget = backendTarget;
        RuntimeName = runtimeName;
    }

    public DialectBackendTarget BackendTarget { get; }

    public string RuntimeName { get; }
}
