namespace UniversalToolchain.ModuleContracts;

internal static class ContractIdentifierValidation
{
    public static string RequireNonEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            Thrower.Argument(paramName, "Contract identifier value must not be empty.");

        return value;
    }

    public static string RequireDottedIdentifier(string value, string paramName)
    {
        value = RequireNonEmpty(value, paramName);

        if (value.Any(char.IsWhiteSpace))
            Thrower.Argument(paramName, $"Contract identifier '{value}' must not contain whitespace.");

        if (!value.Contains(".", StringComparison.Ordinal))
            Thrower.Argument(paramName, $"Contract identifier '{value}' must include an owning namespace prefix.");

        if (value.StartsWith(".", StringComparison.Ordinal) || value.EndsWith(".", StringComparison.Ordinal))
            Thrower.Argument(paramName, $"Contract identifier '{value}' must not start or end with '.'.");

        if (value.Contains("..", StringComparison.Ordinal))
            Thrower.Argument(paramName, $"Contract identifier '{value}' must not contain empty namespace segments.");

        return value;
    }
}
