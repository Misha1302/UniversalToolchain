using System.Collections.ObjectModel;
using BasicCore.Contracts;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Backend-neutral request for compiling and running source through a selected toolchain runtime.
/// </summary>
public sealed class ToolchainRuntimeRunRequest
{
    private readonly ReadOnlyDictionary<string, object?> _arguments;

    public ToolchainRuntimeRunRequest(
        string code,
        string backend,
        IReadOnlyDictionary<string, object?>? arguments = null,
        OrderedDictionary<string, Type>? declaredBindings = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            Thrower.Argument(nameof(code), "Source code must not be empty.");

        if (string.IsNullOrWhiteSpace(backend))
            Thrower.Argument(nameof(backend), "Backend name must not be empty.");

        Code = code;
        Backend = backend;
        _arguments = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(arguments ?? new Dictionary<string, object?>(), StringComparer.Ordinal));
        DeclaredBindings = declaredBindings;
    }

    public string Code { get; }

    public string Backend { get; }

    public IReadOnlyDictionary<string, object?> Arguments => _arguments;

    public OrderedDictionary<string, Type>? DeclaredBindings { get; }
}
