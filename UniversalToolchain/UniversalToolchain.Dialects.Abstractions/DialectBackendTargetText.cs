using ExceptionsManager;

namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
/// Provides deterministic textual mapping for dialect backend targets.
/// </summary>
public static class DialectBackendTargetText
{
    public static string ToText(DialectBackendTarget target)
    {
        return target switch
        {
            DialectBackendTarget.Interpreter => "interpreter",
            DialectBackendTarget.Cil => "cil",
            _ => "any"
        };
    }

    public static bool TryParse(string text, bool allowAny, out DialectBackendTarget target)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Thrower.Argument(nameof(text), "Backend token must not be empty.");
        }

        var normalized = text.Trim();
        if (string.Equals(normalized, "interpreter", StringComparison.Ordinal))
        {
            target = DialectBackendTarget.Interpreter;
            return true;
        }

        if (string.Equals(normalized, "cil", StringComparison.Ordinal))
        {
            target = DialectBackendTarget.Cil;
            return true;
        }

        if (allowAny && string.Equals(normalized, "any", StringComparison.Ordinal))
        {
            target = DialectBackendTarget.Any;
            return true;
        }

        target = DialectBackendTarget.Any;
        return false;
    }
}
