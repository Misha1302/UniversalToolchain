using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Explicit and deterministic description of what apply-mode would wire for a resolved dialect.
/// </summary>
public sealed class DialectApplyDescription
{
    private readonly ReadOnlyCollection<Type> _frontendModules;
    private readonly ReadOnlyCollection<DialectApplyIntrinsicPermission> _intrinsics;
    private readonly ReadOnlyCollection<Type> _irProcessingModules;
    private readonly ReadOnlyCollection<Type> _optimizers;
    private readonly ReadOnlyCollection<string> _runtimeBackends;

    public DialectApplyDescription(
        string dialectName,
        IEnumerable<Type> frontendModules,
        IEnumerable<Type> irProcessingModules,
        IEnumerable<Type> optimizers,
        IEnumerable<string> runtimeBackends,
        IEnumerable<DialectApplyIntrinsicPermission> intrinsics)
    {
        if (string.IsNullOrWhiteSpace(dialectName))
            Thrower.Argument(nameof(dialectName), "Dialect name must not be empty.");

        DialectName = dialectName;
        _frontendModules = new ReadOnlyCollection<Type>(Snapshot(frontendModules, nameof(frontendModules)));
        _irProcessingModules = new ReadOnlyCollection<Type>(Snapshot(irProcessingModules, nameof(irProcessingModules)));
        _optimizers = new ReadOnlyCollection<Type>(Snapshot(optimizers, nameof(optimizers)));
        _runtimeBackends = new ReadOnlyCollection<string>(SnapshotStrings(runtimeBackends, nameof(runtimeBackends)));
        _intrinsics = new ReadOnlyCollection<DialectApplyIntrinsicPermission>(Snapshot(intrinsics, nameof(intrinsics)));
    }

    public string DialectName { get; }

    public IReadOnlyList<Type> FrontendModules => _frontendModules;

    public IReadOnlyList<Type> IrProcessingModules => _irProcessingModules;

    public IReadOnlyList<Type> Optimizers => _optimizers;

    public IReadOnlyList<string> RuntimeBackends => _runtimeBackends;

    public IReadOnlyList<DialectApplyIntrinsicPermission> Intrinsics => _intrinsics;

    public string ToDeterministicText()
    {
        return string.Join(
            Environment.NewLine,
            $"Dialect: {DialectName}",
            $"Frontend modules: {JoinTypeNames(FrontendModules)}",
            $"IR modules: {JoinTypeNames(IrProcessingModules)}",
            $"Optimizers: {JoinTypeNames(Optimizers)}",
            $"Backends: {JoinStrings(RuntimeBackends)}",
            $"Intrinsics: {JoinStrings(Intrinsics.Select(x => $"{x.Name}@{DialectBackendTargetText.ToText(x.Target)}"))}");
    }

    private static List<T> Snapshot<T>(IEnumerable<T> source, string paramName)
    {
        if (source == null)
            Thrower.ArgumentNull(paramName);

        var list = new List<T>();
        foreach (var item in source)
        {
            if (item == null)
                Thrower.Argument(paramName, "Collection must not contain null entries.");

            list.Add(item);
        }

        return list;
    }

    private static List<string> SnapshotStrings(IEnumerable<string> source, string paramName)
    {
        if (source == null)
            Thrower.ArgumentNull(paramName);

        var list = new List<string>();
        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item))
                Thrower.Argument(paramName, "Collection must not contain empty values.");

            list.Add(item);
        }

        return list;
    }

    private static string JoinTypeNames(IEnumerable<Type> types) =>
        JoinStrings(types.Select(type => type.FullName ?? type.Name));

    private static string JoinStrings(IEnumerable<string> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? "<none>" : string.Join(", ", list);
    }
}