using System.Reflection;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Immutable normalized CLR assembly identity used by runtime loading policy.
/// </summary>
public sealed record RuntimeAssemblyIdentity(
    string Name,
    Version? Version,
    string CultureName,
    string PublicKeyToken)
{
    public static RuntimeAssemblyIdentity FromAssemblyName(AssemblyName assemblyName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);
        if (string.IsNullOrWhiteSpace(assemblyName.Name))
            throw new ArgumentException("Assembly identity must contain a simple name.", nameof(assemblyName));

        return new RuntimeAssemblyIdentity(
            assemblyName.Name,
            assemblyName.Version,
            NormalizeCulture(assemblyName.CultureName),
            Convert.ToHexString(assemblyName.GetPublicKeyToken() ?? []).ToLowerInvariant());
    }

    public bool Matches(AssemblyName assemblyName) => this == FromAssemblyName(assemblyName);

    public override string ToString()
    {
        var version = Version?.ToString() ?? "<null>";
        var culture = CultureName.Length == 0 ? "neutral" : CultureName;
        var token = PublicKeyToken.Length == 0 ? "null" : PublicKeyToken;
        return $"{Name}, Version={version}, Culture={culture}, PublicKeyToken={token}";
    }

    private static string NormalizeCulture(string? cultureName) =>
        string.IsNullOrWhiteSpace(cultureName) ? string.Empty : cultureName.Trim().ToLowerInvariant();
}
