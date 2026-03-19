using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Immutable execution configuration resolved from a dialect build plan and runtime composition.
/// </summary>
public sealed class WistDialectExecutionConfiguration
{
    private readonly ReadOnlyCollection<string> _allowedIntrinsics;
    private readonly ReadOnlyCollection<DialectBackendTarget> _enabledBackends;
    private readonly ReadOnlyCollection<string> _forbiddenIntrinsics;
    private readonly ReadOnlyCollection<Type> _frontendModules;
    private readonly ReadOnlyCollection<Type> _irModules;
    private readonly ReadOnlyCollection<Type> _optimizers;

    public WistDialectExecutionConfiguration(
        string dialectName,
        IEnumerable<Type> frontendModules,
        IEnumerable<Type> irModules,
        IEnumerable<Type> optimizers,
        IEnumerable<DialectBackendTarget> enabledBackends,
        IEnumerable<string> allowedIntrinsics,
        IEnumerable<string> forbiddenIntrinsics)
    {
        if (string.IsNullOrWhiteSpace(dialectName))
            Thrower.Argument(nameof(dialectName), "Dialect name must not be empty.");

        DialectName = dialectName;
        _frontendModules = new ReadOnlyCollection<Type>(SnapshotTypes(frontendModules, nameof(frontendModules)));
        _irModules = new ReadOnlyCollection<Type>(SnapshotTypes(irModules, nameof(irModules)));
        _optimizers = new ReadOnlyCollection<Type>(SnapshotTypes(optimizers, nameof(optimizers)));
        _enabledBackends = new ReadOnlyCollection<DialectBackendTarget>(SnapshotValues(enabledBackends, nameof(enabledBackends)));
        _allowedIntrinsics = new ReadOnlyCollection<string>(SnapshotStrings(allowedIntrinsics, nameof(allowedIntrinsics)));
        _forbiddenIntrinsics = new ReadOnlyCollection<string>(SnapshotStrings(forbiddenIntrinsics, nameof(forbiddenIntrinsics)));
    }

    public string DialectName { get; }

    public IReadOnlyList<Type> FrontendModules => _frontendModules;

    public IReadOnlyList<Type> IrModules => _irModules;

    public IReadOnlyList<Type> Optimizers => _optimizers;

    public IReadOnlyList<DialectBackendTarget> EnabledBackends => _enabledBackends;

    public IReadOnlyList<string> AllowedIntrinsics => _allowedIntrinsics;

    public IReadOnlyList<string> ForbiddenIntrinsics => _forbiddenIntrinsics;

    private static List<Type> SnapshotTypes(IEnumerable<Type> values, string paramName)
    {
        if (values == null)
            Thrower.ArgumentNull(paramName);

        return values.Select(x => x.NotNull(paramName)).Distinct().OrderBy(x => x.FullName, StringComparer.Ordinal).ToList();
    }

    private static List<string> SnapshotStrings(IEnumerable<string> values, string paramName)
    {
        if (values == null)
            Thrower.ArgumentNull(paramName);

        return values
            .Select(x => x.NotNull(paramName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    private static List<T> SnapshotValues<T>(IEnumerable<T> values, string paramName)
        where T : struct
    {
        if (values == null)
            Thrower.ArgumentNull(paramName);

        return values.Distinct().OrderBy(x => x).ToList();
    }
}