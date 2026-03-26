using BasicCore.Contracts;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

public sealed record DialectRuntimeModuleDescriptor(
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    Type ImplementationType,
    string? AssemblyName = null)
{
    public IReadOnlyList<string> AllAliases => [CanonicalAlias, .. Aliases];

    public bool IsFrontendModule => typeof(IFrontendCoreModule).IsAssignableFrom(ImplementationType);

    public bool IsIrProcessingModule => typeof(IIRProcessingModule).IsAssignableFrom(ImplementationType);

    public static DialectRuntimeModuleDescriptor Create(string canonicalAlias, IReadOnlyList<string> aliases, Type implementationType, string? assemblyName = null)
    {
        if (string.IsNullOrWhiteSpace(canonicalAlias))
            Thrower.Argument(nameof(canonicalAlias), "Module canonical alias must not be empty.");

        if (implementationType == null)
            Thrower.ArgumentNull(nameof(implementationType));

        if (!typeof(IFrontendCoreModule).IsAssignableFrom(implementationType) && !typeof(IIRProcessingModule).IsAssignableFrom(implementationType))
            Thrower.Argument(nameof(implementationType), "Module implementation type must implement IFrontendCoreModule or IIRProcessingModule.");

        return new DialectRuntimeModuleDescriptor(canonicalAlias, NormalizeAliases(aliases, canonicalAlias), implementationType, assemblyName);
    }

    private static IReadOnlyList<string> NormalizeAliases(IReadOnlyList<string> aliases, string canonicalAlias)
    {
        var values = (aliases ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).Where(x => x != canonicalAlias).OrderBy(x => x, StringComparer.Ordinal).ToList();
        return values;
    }
}
