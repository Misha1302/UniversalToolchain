using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Immutable backend-specific runtime configuration for one enabled backend.
/// </summary>
public class DialectBackendRuntimeConfiguration
{
    private readonly ReadOnlyCollection<string> _allowedIntrinsics;
    private readonly ReadOnlyCollection<string> _forbiddenIntrinsics;
    private readonly ReadOnlyCollection<Type> _optimizerTypes;

    public DialectBackendRuntimeConfiguration(
        RuntimeBackendDescriptor backendDescriptor,
        IEnumerable<Type> optimizerTypes,
        IEnumerable<string> allowedIntrinsics,
        IEnumerable<string> forbiddenIntrinsics,
        bool hasExplicitAllowList)
        : this(null, backendDescriptor, optimizerTypes, allowedIntrinsics, forbiddenIntrinsics, hasExplicitAllowList)
    {
    }

    public DialectBackendRuntimeConfiguration(
        RuntimeComponentManifestEntry? backendManifestEntry,
        RuntimeBackendDescriptor backendDescriptor,
        IEnumerable<Type> optimizerTypes,
        IEnumerable<string> allowedIntrinsics,
        IEnumerable<string> forbiddenIntrinsics,
        bool hasExplicitAllowList)
    {
        backendDescriptor = backendDescriptor.ArgNotNull();

        BackendManifestEntry = backendManifestEntry;
        BackendDescriptor = backendDescriptor;
        _optimizerTypes = new ReadOnlyCollection<Type>(SnapshotTypes(optimizerTypes, nameof(optimizerTypes)));
        _allowedIntrinsics = new ReadOnlyCollection<string>(Snapshot(allowedIntrinsics, nameof(allowedIntrinsics)));
        _forbiddenIntrinsics = new ReadOnlyCollection<string>(Snapshot(forbiddenIntrinsics, nameof(forbiddenIntrinsics)));
        HasExplicitAllowList = hasExplicitAllowList;
    }

    public RuntimeComponentManifestEntry? BackendManifestEntry { get; }

    public RuntimeBackendDescriptor BackendDescriptor { get; }

    public IReadOnlyList<Type> OptimizerTypes => _optimizerTypes;

    public IReadOnlyList<string> AllowedIntrinsics => _allowedIntrinsics;

    public IReadOnlyList<string> ForbiddenIntrinsics => _forbiddenIntrinsics;

    public bool HasExplicitAllowList { get; }

    private static List<Type> SnapshotTypes(IEnumerable<Type> values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        return values
            .Select(x => x.NotNull(paramName.NotNull()))
            .Distinct()
            .OrderBy(x => x.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static List<string> Snapshot(IEnumerable<string> values, [CallerArgumentExpression(nameof(values))] string? paramName = null)
    {
        return values
            .Select(x => x.NotNull(paramName.NotNull()))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }
}
