namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Binds one declared independent extension identity to the exact semantic surfaces and runtime owners it introduces.
/// </summary>
public sealed class PlanFuzzIndependentExtensionEvidence
{
    public PlanFuzzIndependentExtensionEvidence(
        string extensionId,
        IEnumerable<string> surfaceIds,
        IEnumerable<string> ownerIds)
    {
        if (string.IsNullOrWhiteSpace(extensionId) || !StringComparer.Ordinal.Equals(extensionId, extensionId.Trim()))
            Thrower.Argument(nameof(extensionId), "Independent extension ID must be non-empty and canonical.");

        ExtensionId = extensionId;
        SurfaceIds = Snapshot(surfaceIds, nameof(surfaceIds));
        OwnerIds = Snapshot(ownerIds, nameof(ownerIds));
        if (SurfaceIds.Count == 0)
            Thrower.Argument(nameof(surfaceIds), "Independent extension evidence must contain at least one surface ID.");
        if (OwnerIds.Count == 0)
            Thrower.Argument(nameof(ownerIds), "Independent extension evidence must contain at least one owner ID.");
    }

    public string ExtensionId { get; }
    public IReadOnlyList<string> SurfaceIds { get; }
    public IReadOnlyList<string> OwnerIds { get; }

    internal string CanonicalIdentity =>
        $"{ExtensionId}|surfaces:{string.Join(',', SurfaceIds)}|owners:{string.Join(',', OwnerIds)}";

    private static IReadOnlyList<string> Snapshot(IEnumerable<string> values, string parameterName)
    {
        if (values == null)
            return Thrower.Argument<IReadOnlyList<string>>(parameterName, $"Argument '{parameterName}' must not be null.");

        var snapshot = values.ToArray();
        if (snapshot.Any(static value => string.IsNullOrWhiteSpace(value)))
            return Thrower.Argument<IReadOnlyList<string>>(parameterName, $"Argument '{parameterName}' must not contain empty IDs.");
        if (snapshot.Any(static value => !StringComparer.Ordinal.Equals(value, value.Trim())))
            return Thrower.Argument<IReadOnlyList<string>>(parameterName, $"Argument '{parameterName}' must contain canonical IDs without surrounding whitespace.");
        if (snapshot.Distinct(StringComparer.Ordinal).Count() != snapshot.Length)
            return Thrower.Argument<IReadOnlyList<string>>(parameterName, $"Argument '{parameterName}' must not contain duplicate IDs.");

        Array.Sort(snapshot, StringComparer.Ordinal);
        return new ReadOnlyCollection<string>(snapshot);
    }
}
