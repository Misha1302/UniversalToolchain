using System.Reflection;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistLanguagePackIdentity
{
    private const string VersionMetadataKey = "UniversalToolchain.PackageVersion";

    public static string Version { get; } = typeof(WistLanguagePackIdentity).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => StringComparer.Ordinal.Equals(attribute.Key, VersionMetadataKey))
        .Value ?? throw new InvalidOperationException("The Wist language-pack version metadata is missing.");
}
