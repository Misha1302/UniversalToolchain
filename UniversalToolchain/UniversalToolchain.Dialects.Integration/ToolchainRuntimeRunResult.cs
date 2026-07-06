using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Result envelope for backend-neutral toolchain execution.
/// </summary>
public sealed class ToolchainRuntimeRunResult
{
    public ToolchainRuntimeRunResult(string dialectName, string backend, object? value)
    {
        if (string.IsNullOrWhiteSpace(dialectName))
            Thrower.Argument(nameof(dialectName), "Dialect name must not be empty.");

        if (string.IsNullOrWhiteSpace(backend))
            Thrower.Argument(nameof(backend), "Backend name must not be empty.");

        DialectName = dialectName;
        Backend = backend;
        Value = value;
    }

    public string DialectName { get; }

    public string Backend { get; }

    public object? Value { get; }
}
