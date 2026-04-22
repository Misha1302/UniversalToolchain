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

    public SelectedRuntimeExecutionShape(
        string dialectName,
        IEnumerable<Type> frontendModuleTypes,
        IEnumerable<Type> irModuleTypes,
        IEnumerable<RuntimeComponentManifestEntry> optimizerEntries,
        IEnumerable<RuntimeComponentManifestEntry> backendEntries)
    {
        if (string.IsNullOrWhiteSpace(dialectName))
        {
            Thrower.Argument(nameof(dialectName), "Dialect name must not be empty.");
        }

        DialectName = dialectName;
        _frontendModuleTypes = new ReadOnlyCollection<Type>(SnapshotTypes(frontendModuleTypes, nameof(frontendModuleTypes)));
        _irModuleTypes = new ReadOnlyCollection<Type>(SnapshotTypes(irModuleTypes, nameof(irModuleTypes)));
        _optimizerEntries = new ReadOnlyCollection<RuntimeComponentManifestEntry>(SnapshotEntries(optimizerEntries, RuntimeComponentKind.Optimizer, nameof(optimizerEntries)));
        _backendEntries = new ReadOnlyCollection<RuntimeComponentManifestEntry>(SnapshotEntries(backendEntries, RuntimeComponentKind.Backend, nameof(backendEntries)));
    }

    public string DialectName { get; }

    public IReadOnlyList<Type> FrontendModuleTypes => _frontendModuleTypes;

    public IReadOnlyList<Type> IRModuleTypes => _irModuleTypes;

    public IReadOnlyList<RuntimeComponentManifestEntry> OptimizerEntries => _optimizerEntries;

    public IReadOnlyList<RuntimeComponentManifestEntry> BackendEntries => _backendEntries;

    private static List<Type> SnapshotTypes(IEnumerable<Type> values, string paramName)
    {
        return values
            .Select(x => x.NotNull(paramName))
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static List<RuntimeComponentManifestEntry> SnapshotEntries(
        IEnumerable<RuntimeComponentManifestEntry> values,
        RuntimeComponentKind expectedKind,
        string paramName)
    {
        return values
            .Select(x => x.NotNull(paramName))
            .Select(x => ValidateKind(x, expectedKind))
            .Distinct()
            .OrderBy(x => x.CanonicalAlias, StringComparer.Ordinal)
            .ToList();
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
