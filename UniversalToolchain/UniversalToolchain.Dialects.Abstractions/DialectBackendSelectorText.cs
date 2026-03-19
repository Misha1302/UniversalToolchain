using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Provides deterministic textual mapping for dialect backend identifiers and selectors.
/// </summary>
public static class DialectBackendSelectorText
{
    public static string ToText(DialectBackendId backendId)
    {
        if (string.IsNullOrWhiteSpace(backendId.Value))
            Thrower.Argument(nameof(backendId), "Backend identifier must not be empty.");

        return backendId.Value;
    }

    public static string ToText(DialectBackendSelector selector)
    {
        return selector.IsAny ? "any" : ToText(selector.BackendId);
    }

    public static bool TryParseId(string text, out DialectBackendId backendId)
    {
        if (string.IsNullOrWhiteSpace(text))
            Thrower.Argument(nameof(text), "Backend token must not be empty.");

        backendId = new DialectBackendId(text.Trim());
        return true;
    }

    public static bool TryParseSelector(string text, bool allowAny, out DialectBackendSelector selector)
    {
        if (string.IsNullOrWhiteSpace(text))
            Thrower.Argument(nameof(text), "Backend token must not be empty.");

        var normalized = text.Trim();
        if (allowAny && (string.Equals(normalized, "any", StringComparison.Ordinal) || string.Equals(normalized, "*", StringComparison.Ordinal)))
        {
            selector = DialectBackendSelector.Any;
            return true;
        }

        selector = DialectBackendSelector.For(new DialectBackendId(normalized));
        return true;
    }
}
