using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

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

    public IReadOnlyList<Type> IrModuleTypes => _irModuleTypes;

    private static List<Type> Snapshot(IEnumerable<Type> values, string paramName)
    {
        var snapshot = new List<Type>();
        var seen = new HashSet<Type>();

        foreach (var value in values)
        {
            var type = value.NotNull(paramName);
            if (seen.Add(type))
                snapshot.Add(type);
        }

        return snapshot;
    }
}