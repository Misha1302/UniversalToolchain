using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Immutable execution configuration resolved from a dialect build plan and runtime composition.
/// </summary>
public sealed class WistDialectExecutionConfiguration
{
    private readonly ReadOnlyCollection<DialectBackendRuntimeConfiguration> _backendConfigurations;
    private readonly ReadOnlyCollection<Type> _frontendModules;
    private readonly ReadOnlyCollection<Type> _irModules;
    private readonly Dictionary<string, DialectBackendId> _knownBackendNameMap;
    private readonly ReadOnlyCollection<Type> _optimizers;

    public WistDialectExecutionConfiguration(
        string dialectName,
        IEnumerable<Type> frontendModules,
        IEnumerable<Type> irModules,
        IEnumerable<Type> optimizers,
        IEnumerable<DialectBackendRuntimeConfiguration> backendConfigurations,
        IEnumerable<RuntimeBackendDescriptor> knownBackends)
    {
        if (string.IsNullOrWhiteSpace(dialectName))
            Thrower.Argument(nameof(dialectName), "Dialect name must not be empty.");

        DialectName = dialectName;
        _frontendModules = new ReadOnlyCollection<Type>(SnapshotTypes(frontendModules, nameof(frontendModules)));
        _irModules = new ReadOnlyCollection<Type>(SnapshotTypes(irModules, nameof(irModules)));
        _optimizers = new ReadOnlyCollection<Type>(SnapshotTypes(optimizers, nameof(optimizers)));
        _backendConfigurations = new ReadOnlyCollection<DialectBackendRuntimeConfiguration>(SnapshotBackends(backendConfigurations, nameof(backendConfigurations)));
        _knownBackendNameMap = SnapshotKnownBackends(knownBackends, nameof(knownBackends));
    }

    public string DialectName { get; }

    public IReadOnlyList<Type> FrontendModules => _frontendModules;

    public IReadOnlyList<Type> IrModules => _irModules;

    public IReadOnlyList<Type> Optimizers => _optimizers;

    public IReadOnlyList<DialectBackendRuntimeConfiguration> BackendConfigurations => _backendConfigurations;

    public IReadOnlyList<RuntimeBackendDescriptor> EnabledBackends => _backendConfigurations.Select(x => x.BackendDescriptor).ToList();

    public bool TryResolveKnownBackendId(string nameOrAlias, out DialectBackendId backendId)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
            Thrower.Argument(nameof(nameOrAlias), "Execution mode must not be empty.");

        return _knownBackendNameMap.TryGetValue(nameOrAlias, out backendId);
    }

    public bool TryGetEnabledBackend(DialectBackendId backendId, out DialectBackendRuntimeConfiguration backendConfiguration)
    {
        if (string.IsNullOrWhiteSpace(backendId.Value))
            Thrower.Argument(nameof(backendId), "Backend identifier must not be empty.");

        backendConfiguration = _backendConfigurations.FirstOrDefault(x => x.BackendDescriptor.BackendId == backendId)!;
        return backendConfiguration != null;
    }

    private static List<Type> SnapshotTypes(IEnumerable<Type> values, string paramName)
    {
        if (values == null)
            Thrower.ArgumentNull(paramName);

        return values.Select(x => x.NotNull(paramName)).Distinct().OrderBy(x => x.FullName, StringComparer.Ordinal).ToList();
    }


    private static Dictionary<string, DialectBackendId> SnapshotKnownBackends(IEnumerable<RuntimeBackendDescriptor> values, string paramName)
    {
        if (values == null)
            Thrower.ArgumentNull(paramName);

        var map = new Dictionary<string, DialectBackendId>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var descriptor = value.NotNull(paramName);
            foreach (var name in descriptor.AllNames)
                map[name] = descriptor.BackendId;
        }

        return map;
    }

    private static List<DialectBackendRuntimeConfiguration> SnapshotBackends(IEnumerable<DialectBackendRuntimeConfiguration> values, string paramName)
    {
        if (values == null)
            Thrower.ArgumentNull(paramName);

        return values
            .Select(x => x.NotNull(paramName))
            .OrderBy(x => x.BackendDescriptor.BackendId)
            .ToList();
    }
}