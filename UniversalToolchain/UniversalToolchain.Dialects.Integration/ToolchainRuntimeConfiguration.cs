using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Immutable backend-neutral runtime configuration produced by a language or dialect adapter.
/// Concrete language packs may derive from this type to expose compatibility-specific metadata.
/// </summary>
public class ToolchainRuntimeConfiguration : IToolchainRuntimeConfiguration
{
    private readonly ReadOnlyCollection<DialectBackendRuntimeConfiguration> _backendConfigurations;
    private readonly ReadOnlyCollection<Type> _frontendModules;
    private readonly ReadOnlyCollection<Type> _irModules;
    private readonly Dictionary<string, DialectBackendId> _knownBackendNameMap;
    private readonly ReadOnlyCollection<Type> _optimizers;
    private readonly ReadOnlyCollection<Type> _requiredInfrastructureModules;

    public ToolchainRuntimeConfiguration(
        string dialectName,
        IEnumerable<Type> frontendModules,
        IEnumerable<Type> irModules,
        IEnumerable<Type> optimizers,
        IEnumerable<DialectBackendRuntimeConfiguration> backendConfigurations,
        IEnumerable<RuntimeBackendDescriptor>? knownBackends = null,
        IEnumerable<Type>? requiredInfrastructureModules = null)
    {
        if (string.IsNullOrWhiteSpace(dialectName))
            Thrower.Argument(nameof(dialectName), "Dialect name must not be empty.");

        var backendSnapshot = SnapshotBackends(backendConfigurations, nameof(backendConfigurations));

        DialectName = dialectName;
        _requiredInfrastructureModules = new ReadOnlyCollection<Type>(SnapshotTypes(requiredInfrastructureModules ?? [], nameof(requiredInfrastructureModules)));
        _frontendModules = new ReadOnlyCollection<Type>(SnapshotTypes(frontendModules, nameof(frontendModules)));
        _irModules = new ReadOnlyCollection<Type>(SnapshotTypes(irModules, nameof(irModules)));
        _optimizers = new ReadOnlyCollection<Type>(SnapshotTypes(optimizers, nameof(optimizers)));
        _backendConfigurations = new ReadOnlyCollection<DialectBackendRuntimeConfiguration>(backendSnapshot);
        _knownBackendNameMap = SnapshotKnownBackends(knownBackends ?? backendSnapshot.Select(static x => x.BackendDescriptor));
    }

    public string DialectName { get; }

    public IReadOnlyList<Type> RequiredInfrastructureModules => _requiredInfrastructureModules;

    public IReadOnlyList<Type> FrontendModules => _frontendModules;

    public IReadOnlyList<Type> IrModules => _irModules;

    public IReadOnlyList<Type> Optimizers => _optimizers;

    public IReadOnlyList<DialectBackendRuntimeConfiguration> BackendConfigurations => _backendConfigurations;

    public IReadOnlyList<RuntimeBackendDescriptor> EnabledBackends =>
        _backendConfigurations.Select(static x => x.BackendDescriptor).ToArray();

    public bool TryResolveKnownBackendId(string nameOrAlias, out DialectBackendId backendId)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
            Thrower.Argument(nameof(nameOrAlias), "Backend name must not be empty.");

        return _knownBackendNameMap.TryGetValue(nameOrAlias, out backendId);
    }

    public bool TryGetEnabledBackend(
        DialectBackendId backendId,
        [MaybeNullWhen(false)] out DialectBackendRuntimeConfiguration backendConfiguration)
    {
        if (string.IsNullOrWhiteSpace(backendId.Value))
            Thrower.Argument(nameof(backendId), "Backend identifier must not be empty.");

        var found = _backendConfigurations.FirstOrDefault(x => x.BackendDescriptor.BackendId == backendId);
        if (found == null)
        {
            backendConfiguration = null;
            return false;
        }

        backendConfiguration = found;
        return true;
    }

    private static List<Type> SnapshotTypes(IEnumerable<Type> values, string paramName)
    {
        ArgumentNullException.ThrowIfNull(values, paramName);
        var snapshot = new List<Type>();
        var seen = new HashSet<Type>();
        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value, paramName);
            if (seen.Add(value))
                snapshot.Add(value);
        }
        return snapshot;
    }

    private static Dictionary<string, DialectBackendId> SnapshotKnownBackends(IEnumerable<RuntimeBackendDescriptor> values)
    {
        var map = new Dictionary<string, DialectBackendId>(StringComparer.Ordinal);
        foreach (var descriptor in values)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            foreach (var name in descriptor.AllNames)
            {
                if (map.TryGetValue(name, out var existing) && existing != descriptor.BackendId)
                    Thrower.InvalidOpEx($"Backend alias '{name}' maps to both '{existing.Value}' and '{descriptor.BackendId.Value}'.");
                map[name] = descriptor.BackendId;
            }
        }
        return map;
    }

    private static List<DialectBackendRuntimeConfiguration> SnapshotBackends(
        IEnumerable<DialectBackendRuntimeConfiguration> values,
        string paramName)
    {
        ArgumentNullException.ThrowIfNull(values, paramName);
        var snapshot = new List<DialectBackendRuntimeConfiguration>();
        var seen = new HashSet<DialectBackendId>();
        foreach (var backend in values)
        {
            ArgumentNullException.ThrowIfNull(backend, paramName);
            if (!seen.Add(backend.BackendDescriptor.BackendId))
                Thrower.InvalidOpEx($"Backend '{backend.BackendDescriptor.BackendId.Value}' is configured more than once.");
            snapshot.Add(backend);
        }
        return snapshot;
    }
}
