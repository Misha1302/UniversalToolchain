using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Immutable execution configuration resolved from a dialect build plan and runtime composition.
/// </summary>
public sealed class WistDialectExecutionConfiguration : IToolchainRuntimeConfiguration
{
    private readonly ReadOnlyCollection<DialectBackendRuntimeConfiguration> _backendConfigurations;
    private readonly ReadOnlyCollection<Type> _frontendModules;
    private readonly ReadOnlyCollection<Type> _irModules;
    private readonly Dictionary<string, DialectBackendId> _knownBackendNameMap;
    private readonly ReadOnlyCollection<Type> _optimizers;
    private readonly ReadOnlyCollection<Type> _requiredInfrastructureModules;

    public WistDialectExecutionConfiguration(
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
        _requiredInfrastructureModules = new ReadOnlyCollection<Type>(SnapshotTypes(requiredInfrastructureModules ?? Enumerable.Empty<Type>(), nameof(requiredInfrastructureModules)));
        _frontendModules = new ReadOnlyCollection<Type>(SnapshotTypes(frontendModules, nameof(frontendModules)));
        _irModules = new ReadOnlyCollection<Type>(SnapshotTypes(irModules, nameof(irModules)));
        _optimizers = new ReadOnlyCollection<Type>(SnapshotTypes(optimizers, nameof(optimizers)));
        _backendConfigurations = new ReadOnlyCollection<DialectBackendRuntimeConfiguration>(backendSnapshot);
        _knownBackendNameMap = SnapshotKnownBackends(knownBackends ?? backendSnapshot.Select(x => x.BackendDescriptor));
    }

    public string DialectName { get; }

    public IReadOnlyList<Type> RequiredInfrastructureModules => _requiredInfrastructureModules;

    public IReadOnlyList<Type> FrontendModules => _frontendModules;

    public IReadOnlyList<Type> IrModules => _irModules;

    public IReadOnlyList<Type> Optimizers => _optimizers;

    public IReadOnlyList<DialectBackendRuntimeConfiguration> BackendConfigurations => _backendConfigurations;

    public IReadOnlyList<RuntimeBackendDescriptor> EnabledBackends => _backendConfigurations.Select(x => x.BackendDescriptor).ToList();

    public bool TryResolveKnownBackendId(string nameOrAlias, out DialectBackendId backendId)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
            Thrower.Argument(nameof(nameOrAlias), "Backend name must not be empty.");

        return _knownBackendNameMap.TryGetValue(nameOrAlias, out backendId);
    }

    public bool TryGetEnabledBackend(DialectBackendId backendId, [MaybeNullWhen(false)] out DialectBackendRuntimeConfiguration backendConfiguration)
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

    private static List<Type> SnapshotTypes(IEnumerable<Type> values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        var snapshot = new List<Type>();
        var seen = new HashSet<Type>();

        foreach (var value in values)
        {
            var type = value.NotNull(paramName.NotNull());
            if (seen.Add(type))
                snapshot.Add(type);
        }

        return snapshot;
    }

    private static Dictionary<string, DialectBackendId> SnapshotKnownBackends(IEnumerable<RuntimeBackendDescriptor> values, [CallerArgumentExpression(nameof(values))] string paramName = null!)
    {
        var map = new Dictionary<string, DialectBackendId>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var descriptor = value.NotNull(paramName);
            foreach (var name in descriptor.AllNames)
                map[name] = descriptor.BackendId;
        }

        return map;
    }

    private static List<DialectBackendRuntimeConfiguration> SnapshotBackends(IEnumerable<DialectBackendRuntimeConfiguration> values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        var snapshot = new List<DialectBackendRuntimeConfiguration>();
        var seen = new HashSet<DialectBackendId>();

        foreach (var value in values)
        {
            var backend = value.NotNull(paramName.NotNull());
            if (seen.Add(backend.BackendDescriptor.BackendId))
                snapshot.Add(backend);
        }

        return snapshot;
    }
}
