using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Immutable execution-facing projection of a dialect build plan and selected runtime plan.
/// </summary>
public sealed class SelectedRuntimeExecutionShape
{
    private readonly ReadOnlyCollection<RuntimeComponentManifestEntry> _backendEntries;
    private readonly ReadOnlyCollection<Type> _frontendModuleTypes;
    private readonly ReadOnlyCollection<Type> _irModuleTypes;
    private readonly ReadOnlyCollection<RuntimeComponentManifestEntry> _optimizerEntries;
    private readonly ReadOnlyCollection<Type> _requiredFrontendInfrastructureModuleTypes;
    private readonly ReadOnlyCollection<Type> _requiredIrInfrastructureModuleTypes;
    private readonly ReadOnlyCollection<Type> _selectedFrontendModuleTypes;
    private readonly ReadOnlyCollection<Type> _selectedIrModuleTypes;

    public SelectedRuntimeExecutionShape(
        string dialectName,
        IEnumerable<Type> requiredFrontendInfrastructureModuleTypes,
        IEnumerable<Type> selectedFrontendModuleTypes,
        IEnumerable<Type> requiredIrInfrastructureModuleTypes,
        IEnumerable<Type> selectedIrModuleTypes,
        IEnumerable<RuntimeComponentManifestEntry> optimizerEntries,
        IEnumerable<RuntimeComponentManifestEntry> backendEntries)
    {
        if (string.IsNullOrWhiteSpace(dialectName))
        {
            Thrower.Argument(nameof(dialectName), "Dialect name must not be empty.");
        }

        DialectName = dialectName;
        _requiredFrontendInfrastructureModuleTypes = new ReadOnlyCollection<Type>(SnapshotTypes(requiredFrontendInfrastructureModuleTypes, nameof(requiredFrontendInfrastructureModuleTypes)));
        _selectedFrontendModuleTypes = new ReadOnlyCollection<Type>(SnapshotTypes(selectedFrontendModuleTypes, nameof(selectedFrontendModuleTypes)));
        _requiredIrInfrastructureModuleTypes = new ReadOnlyCollection<Type>(SnapshotTypes(requiredIrInfrastructureModuleTypes, nameof(requiredIrInfrastructureModuleTypes)));
        _selectedIrModuleTypes = new ReadOnlyCollection<Type>(SnapshotTypes(selectedIrModuleTypes, nameof(selectedIrModuleTypes)));
        _frontendModuleTypes = new ReadOnlyCollection<Type>(SnapshotTypes(
            _requiredFrontendInfrastructureModuleTypes.Concat(_selectedFrontendModuleTypes),
            nameof(selectedFrontendModuleTypes)));
        _irModuleTypes = new ReadOnlyCollection<Type>(SnapshotTypes(
            _requiredIrInfrastructureModuleTypes.Concat(_selectedIrModuleTypes),
            nameof(selectedIrModuleTypes)));
        _optimizerEntries = new ReadOnlyCollection<RuntimeComponentManifestEntry>(SnapshotEntries(optimizerEntries, RuntimeComponentKind.Optimizer, nameof(optimizerEntries)));
        _backendEntries = new ReadOnlyCollection<RuntimeComponentManifestEntry>(SnapshotEntries(backendEntries, RuntimeComponentKind.Backend, nameof(backendEntries)));
    }

    public string DialectName { get; }

    public IReadOnlyList<Type> RequiredFrontendInfrastructureModuleTypes => _requiredFrontendInfrastructureModuleTypes;

    public IReadOnlyList<Type> SelectedFrontendModuleTypes => _selectedFrontendModuleTypes;

    public IReadOnlyList<Type> RequiredIRInfrastructureModuleTypes => _requiredIrInfrastructureModuleTypes;

    public IReadOnlyList<Type> SelectedIRModuleTypes => _selectedIrModuleTypes;

    public IReadOnlyList<Type> FrontendModuleTypes => _frontendModuleTypes;

    public IReadOnlyList<Type> IRModuleTypes => _irModuleTypes;

    public IReadOnlyList<RuntimeComponentManifestEntry> OptimizerEntries => _optimizerEntries;

    public IReadOnlyList<RuntimeComponentManifestEntry> BackendEntries => _backendEntries;

    private static List<Type> SnapshotTypes(IEnumerable<Type> values, string paramName)
    {
        var snapshot = new List<Type>();
        var seen = new HashSet<Type>();

        foreach (var value in values)
        {
            var type = value.NotNull(paramName);
            if (seen.Add(type))
            {
                snapshot.Add(type);
            }
        }

        return snapshot;
    }

    private static List<RuntimeComponentManifestEntry> SnapshotEntries(
        IEnumerable<RuntimeComponentManifestEntry> values,
        RuntimeComponentKind expectedKind,
        string paramName)
    {
        var snapshot = new List<RuntimeComponentManifestEntry>();
        var seen = new HashSet<RuntimeComponentManifestEntry>();

        foreach (var value in values)
        {
            var entry = ValidateKind(value.NotNull(paramName), expectedKind);
            if (seen.Add(entry))
            {
                snapshot.Add(entry);
            }
        }

        return snapshot;
    }

    private static RuntimeComponentManifestEntry ValidateKind(RuntimeComponentManifestEntry entry, RuntimeComponentKind expectedKind)
    {
        if (entry.Kind != expectedKind)
        {
            Thrower.InvalidOpEx(
                $"Runtime component '{entry.CanonicalAlias}' has kind '{RuntimeComponentKindCodec.Format(entry.Kind)}', but '{RuntimeComponentKindCodec.Format(expectedKind)}' was expected.");
        }

        return entry;
    }
}
