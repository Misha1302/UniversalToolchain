using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Backend selector used by dialect directives. The selector can target one backend or all backends.
/// </summary>
public readonly record struct DialectBackendSelector : IComparable<DialectBackendSelector>
{
    private DialectBackendSelector(DialectBackendId backendId, bool isAny)
    {
        if (!isAny && string.IsNullOrWhiteSpace(backendId.Value))
            Thrower.Argument(nameof(backendId), "Backend selector must contain a backend identifier.");

        BackendId = backendId;
        IsAny = isAny;
    }

    public static DialectBackendSelector Any { get; } = new(new DialectBackendId("*"), true);

    public DialectBackendId BackendId { get; }

    public bool IsAny { get; }

    public static DialectBackendSelector For(DialectBackendId backendId) => new(backendId, false);

    public bool Matches(DialectBackendId backendId)
    {
        if (IsAny)
            return true;

        return BackendId == backendId;
    }

    public int CompareTo(DialectBackendSelector other)
    {
        if (IsAny && !other.IsAny)
            return -1;

        if (!IsAny && other.IsAny)
            return 1;

        return BackendId.CompareTo(other.BackendId);
    }

    public override string ToString() => DialectBackendSelectorText.ToText(this);
}
