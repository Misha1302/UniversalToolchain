using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Immutable role classification for runtime-selected module activation types.
/// </summary>
public sealed class SelectedRuntimeModuleClassification
{
    private readonly ReadOnlyCollection<Type> _frontendModuleTypes;
    private readonly ReadOnlyCollection<Type> _irModuleTypes;

    public SelectedRuntimeModuleClassification(
        IEnumerable<Type> frontendModuleTypes,
        IEnumerable<Type> irModuleTypes)
    {
        _frontendModuleTypes = new ReadOnlyCollection<Type>(Snapshot(frontendModuleTypes, nameof(frontendModuleTypes)));
        _irModuleTypes = new ReadOnlyCollection<Type>(Snapshot(irModuleTypes, nameof(irModuleTypes)));
    }

    public IReadOnlyList<Type> FrontendModuleTypes => _frontendModuleTypes;

    public IReadOnlyList<Type> IRModuleTypes => _irModuleTypes;

    private static List<Type> Snapshot(IEnumerable<Type> values, string paramName)
    {
        return values
            .Select(x => x.NotNull(paramName))
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }
}
