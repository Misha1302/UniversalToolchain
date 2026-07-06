using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Declares a named runtime profile as source-level dialect defaults.
/// </summary>
public sealed class RuntimeProfileDefinition
{
    private readonly ReadOnlyDictionary<string, bool> _capabilities;
    private readonly ReadOnlyCollection<DialectBackendId> _defaultBackends;
    private readonly ReadOnlyCollection<string> _defaultModules;
    private readonly ReadOnlyCollection<string> _defaultOptimizers;

    public RuntimeProfileDefinition(
        string name,
        IEnumerable<string>? defaultModules = null,
        IEnumerable<DialectBackendId>? defaultBackends = null,
        IEnumerable<string>? defaultOptimizers = null,
        SecurityProfile? defaultSecurityProfile = null,
        IEnumerable<KeyValuePair<string, bool>>? defaultCapabilities = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(name), "Runtime profile name must not be empty.");

        Name = name;
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        DefaultSecurityProfile = defaultSecurityProfile;
        _defaultModules = Snapshot(defaultModules ?? []);
        _defaultBackends = Snapshot(defaultBackends ?? []);
        _defaultOptimizers = Snapshot(defaultOptimizers ?? []);
        _capabilities = new ReadOnlyDictionary<string, bool>(SnapshotCapabilities(defaultCapabilities ?? []));
    }

    public string Name { get; }

    public string? Description { get; }

    public IReadOnlyList<string> DefaultModules => _defaultModules;

    public IReadOnlyList<DialectBackendId> DefaultBackends => _defaultBackends;

    public IReadOnlyList<string> DefaultOptimizers => _defaultOptimizers;

    public SecurityProfile? DefaultSecurityProfile { get; }

    public IReadOnlyDictionary<string, bool> DefaultCapabilities => _capabilities;

    private static ReadOnlyCollection<T> Snapshot<T>(IEnumerable<T> source)
    {
        source = source.ArgNotNull();
        return new ReadOnlyCollection<T>(source.Select(static x => x.NotNull()).ToList());
    }

    private static Dictionary<string, bool> SnapshotCapabilities(IEnumerable<KeyValuePair<string, bool>> source)
    {
        var dictionary = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var item in source.ArgNotNull())
        {
            if (string.IsNullOrWhiteSpace(item.Key))
                Thrower.Argument(nameof(source), "Capability name must not be empty.");

            dictionary[item.Key] = item.Value;
        }

        return dictionary;
    }
}
