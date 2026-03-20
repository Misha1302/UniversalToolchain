namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Deterministic runtime composition description resolved from a validated dialect build plan.
/// </summary>
public sealed class DialectRuntimeComposition
{
    private readonly ReadOnlyCollection<RuntimeIntrinsicDescriptor> _allowedIntrinsics;
    private readonly ReadOnlyCollection<RuntimeBackendDescriptor> _enabledBackends;
    private readonly ReadOnlyCollection<RuntimeOptimizerDescriptor> _enabledOptimizers;
    private readonly ReadOnlyCollection<RuntimeModuleDescriptor> _orderedModules;

    public DialectRuntimeComposition(
        string dialectName,
        IEnumerable<RuntimeModuleDescriptor> orderedModules,
        IEnumerable<RuntimeBackendDescriptor> enabledBackends,
        IEnumerable<RuntimeOptimizerDescriptor> enabledOptimizers,
        IEnumerable<RuntimeIntrinsicDescriptor> allowedIntrinsics,
        DialectValidationResult diagnostics)
    {
        if (string.IsNullOrWhiteSpace(dialectName))
            Thrower.Argument(nameof(dialectName), "Dialect name must not be empty.");

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        DialectName = dialectName;
        _orderedModules = new ReadOnlyCollection<RuntimeModuleDescriptor>(Snapshot(orderedModules, nameof(orderedModules)));
        _enabledBackends = new ReadOnlyCollection<RuntimeBackendDescriptor>(Snapshot(enabledBackends, nameof(enabledBackends)));
        _enabledOptimizers = new ReadOnlyCollection<RuntimeOptimizerDescriptor>(Snapshot(enabledOptimizers, nameof(enabledOptimizers)));
        _allowedIntrinsics = new ReadOnlyCollection<RuntimeIntrinsicDescriptor>(Snapshot(allowedIntrinsics, nameof(allowedIntrinsics)));
        Diagnostics = diagnostics;
    }

    public string DialectName { get; }

    public IReadOnlyList<RuntimeModuleDescriptor> OrderedModules => _orderedModules;

    public IReadOnlyList<RuntimeBackendDescriptor> EnabledBackends => _enabledBackends;

    public IReadOnlyList<RuntimeOptimizerDescriptor> EnabledOptimizers => _enabledOptimizers;

    public IReadOnlyList<RuntimeIntrinsicDescriptor> AllowedIntrinsics => _allowedIntrinsics;

    public DialectValidationResult Diagnostics { get; }

    public bool IsResolved => Diagnostics.IsValid;

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
}