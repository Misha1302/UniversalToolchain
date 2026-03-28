using System.Collections.ObjectModel;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Immutable execution configuration for one enabled runtime backend.
/// </summary>
public class DialectBackendRuntimeConfiguration
{
    private readonly ReadOnlyCollection<string> _allowedIntrinsics;
    private readonly ReadOnlyCollection<string> _forbiddenIntrinsics;

    public DialectBackendRuntimeConfiguration(
        RuntimeBackendDescriptor backendDescriptor,
        IEnumerable<string> allowedIntrinsics,
        IEnumerable<string> forbiddenIntrinsics,
        bool hasExplicitAllowList)
    {
        if (backendDescriptor == null)
            Thrower.ArgumentNull(nameof(backendDescriptor));

        BackendDescriptor = backendDescriptor;
        _allowedIntrinsics = new ReadOnlyCollection<string>(Snapshot(allowedIntrinsics, nameof(allowedIntrinsics)));
        _forbiddenIntrinsics = new ReadOnlyCollection<string>(Snapshot(forbiddenIntrinsics, nameof(forbiddenIntrinsics)));
        HasExplicitAllowList = hasExplicitAllowList;
    }

    public RuntimeBackendDescriptor BackendDescriptor { get; }

    public IReadOnlyList<string> AllowedIntrinsics => _allowedIntrinsics;

    public IReadOnlyList<string> ForbiddenIntrinsics => _forbiddenIntrinsics;

    public bool HasExplicitAllowList { get; }

    private static List<string> Snapshot(IEnumerable<string> values, string paramName)
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
