using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Immutable backend-specific runtime configuration for one enabled backend.
/// </summary>
public class DialectBackendRuntimeConfiguration
{
    private readonly ReadOnlyCollection<Type> _optimizerTypes;
    private readonly ReadOnlyCollection<string> _allowedIntrinsics;
    private readonly ReadOnlyCollection<string> _forbiddenIntrinsics;

    public DialectBackendRuntimeConfiguration(
        RuntimeBackendDescriptor backendDescriptor,
        IEnumerable<Type> optimizerTypes,
        IEnumerable<string> allowedIntrinsics,
        IEnumerable<string> forbiddenIntrinsics,
        bool hasExplicitAllowList)
    {
        backendDescriptor = backendDescriptor.ArgNotNull();

        BackendDescriptor = backendDescriptor;
        _optimizerTypes = new ReadOnlyCollection<Type>(SnapshotTypes(optimizerTypes, nameof(optimizerTypes)));
        _allowedIntrinsics = new ReadOnlyCollection<string>(Snapshot(allowedIntrinsics, nameof(allowedIntrinsics)));
        _forbiddenIntrinsics = new ReadOnlyCollection<string>(Snapshot(forbiddenIntrinsics, nameof(forbiddenIntrinsics)));
        HasExplicitAllowList = hasExplicitAllowList;
    }

    public RuntimeBackendDescriptor BackendDescriptor { get; }

    public IReadOnlyList<Type> OptimizerTypes => _optimizerTypes;

    public IReadOnlyList<string> AllowedIntrinsics => _allowedIntrinsics;

    public IReadOnlyList<string> ForbiddenIntrinsics => _forbiddenIntrinsics;

    public bool HasExplicitAllowList { get; }

    private static List<Type> SnapshotTypes(IEnumerable<Type> values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        if (values == null)
            Thrower.ArgumentNull(paramName);

        return values
            .Select(x => x.NotNull(paramName))
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static List<string> Snapshot(IEnumerable<string> values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
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
}