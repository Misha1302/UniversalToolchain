using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed record RuntimeTypeReference
{
    public RuntimeTypeReference(string assemblySimpleName, string typeFullName)
    {
        AssemblySimpleName = NormalizeRequired(assemblySimpleName, nameof(assemblySimpleName));
        TypeFullName = NormalizeRequired(typeFullName, nameof(typeFullName));
    }

    public string AssemblySimpleName { get; }

    public string TypeFullName { get; }

    private static string NormalizeRequired(string value, string paramName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            Thrower.Argument(paramName, $"{paramName} must not be empty.");

        return normalized;
    }
}
