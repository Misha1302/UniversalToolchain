using BasicCore.Contracts;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

public sealed record DialectRuntimeOptimizerDescriptor(
    string CanonicalAlias,
    IReadOnlyList<string> Aliases,
    Type ImplementationType,
    string? AssemblyName = null)
{
    public IReadOnlyList<string> AllAliases => [CanonicalAlias, .. Aliases];

    public static DialectRuntimeOptimizerDescriptor Create(string canonicalAlias, IReadOnlyList<string> aliases, Type implementationType, string? assemblyName = null)
    {
        if (string.IsNullOrWhiteSpace(canonicalAlias))
            Thrower.Argument(nameof(canonicalAlias), "Optimizer canonical alias must not be empty.");

        if (implementationType == null)
            Thrower.ArgumentNull(nameof(implementationType));

        if (!typeof(IIRProcessingModule).IsAssignableFrom(implementationType))
            Thrower.Argument(nameof(implementationType), "Optimizer implementation type must implement IIRProcessingModule.");

        var values = (aliases ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).Where(x => x != canonicalAlias).OrderBy(x => x, StringComparer.Ordinal).ToList();
        return new DialectRuntimeOptimizerDescriptor(canonicalAlias, values, implementationType, assemblyName);
    }
}
