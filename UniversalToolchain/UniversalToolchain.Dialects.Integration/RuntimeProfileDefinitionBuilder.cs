using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Fluent builder for source-level runtime profile defaults.
/// </summary>
public sealed class RuntimeProfileDefinitionBuilder
{
    private readonly List<DialectBackendId> _defaultBackends = [];
    private readonly Dictionary<string, bool> _defaultCapabilities = new(StringComparer.Ordinal);
    private readonly List<string> _defaultModules = [];
    private readonly List<string> _defaultOptimizers = [];
    private string? _description;
    private SecurityProfile? _securityProfile;

    private RuntimeProfileDefinitionBuilder(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Runtime profile name must not be empty.");

        Name = name;
    }

    public string Name { get; }

    public static RuntimeProfileDefinitionBuilder Create(string name)
    {
        return new RuntimeProfileDefinitionBuilder(name);
    }

    public RuntimeProfileDefinitionBuilder Describe(string? description)
    {
        _description = string.IsNullOrWhiteSpace(description) ? null : description;
        return this;
    }

    public RuntimeProfileDefinitionBuilder UseModule(string module)
    {
        AddUnique(_defaultModules, module, nameof(module));
        return this;
    }

    public RuntimeProfileDefinitionBuilder UseModules(params string[] modules)
    {
        foreach (var module in modules.ArgNotNull())
            UseModule(module);

        return this;
    }

    public RuntimeProfileDefinitionBuilder EnableBackend(string backend)
    {
        if (string.IsNullOrWhiteSpace(backend))
            Thrower.Argument(nameof(backend), "Backend id must not be empty.");

        AddUnique(_defaultBackends, new DialectBackendId(backend), nameof(backend));
        return this;
    }

    public RuntimeProfileDefinitionBuilder EnableBackend(DialectBackendId backend)
    {
        AddUnique(_defaultBackends, backend.NotNull(), nameof(backend));
        return this;
    }

    public RuntimeProfileDefinitionBuilder EnableOptimizer(string optimizer)
    {
        AddUnique(_defaultOptimizers, optimizer, nameof(optimizer));
        return this;
    }

    public RuntimeProfileDefinitionBuilder Security(SecurityProfile securityProfile)
    {
        _securityProfile = securityProfile;
        return this;
    }

    public RuntimeProfileDefinitionBuilder Capability(string capability, bool enabled = true)
    {
        if (string.IsNullOrWhiteSpace(capability))
            Thrower.Argument(nameof(capability), "Capability name must not be empty.");

        _defaultCapabilities[capability] = enabled;
        return this;
    }

    public RuntimeProfileDefinition Build()
    {
        return new RuntimeProfileDefinition(
            Name,
            _defaultModules,
            _defaultBackends,
            _defaultOptimizers,
            _securityProfile,
            _defaultCapabilities,
            _description);
    }

    private static void AddUnique<T>(ICollection<T> target, T value, string parameterName)
    {
        value = value.NotNull(parameterName);
        if (target.Contains(value))
            return;

        target.Add(value);
    }

    private static void AddUnique(ICollection<string> target, string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            Thrower.Argument(parameterName, "Runtime profile value must not be empty.");

        if (target.Contains(value, StringComparer.Ordinal))
            return;

        target.Add(value);
    }
}
