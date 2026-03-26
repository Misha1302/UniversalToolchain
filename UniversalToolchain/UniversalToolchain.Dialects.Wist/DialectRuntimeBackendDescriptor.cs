using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

public sealed record DialectRuntimeBackendDescriptor(
    DialectBackendId CanonicalId,
    IReadOnlyList<string> Aliases,
    Type ImplementationType,
    string? AssemblyName = null)
{
    public IReadOnlyList<string> AllAliases => [CanonicalId.Value, .. Aliases];

    public static DialectRuntimeBackendDescriptor Create(DialectBackendId canonicalId, IReadOnlyList<string> aliases, Type implementationType, string? assemblyName = null)
    {
        if (string.IsNullOrWhiteSpace(canonicalId.Value))
            Thrower.Argument(nameof(canonicalId), "Backend canonical identifier must not be empty.");

        if (implementationType == null)
            Thrower.ArgumentNull(nameof(implementationType));

        var values = (aliases ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).Where(x => x != canonicalId.Value).OrderBy(x => x, StringComparer.Ordinal).ToList();
        return new DialectRuntimeBackendDescriptor(canonicalId, values, implementationType, assemblyName);
    }
}
