using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistDialectPlanFactory
{
    private const string SecurityMetadata = "wist.security";
    private const string CapabilityPrefix = "wist.capability.";

    public static DialectDefinitionSlice Create(LanguagePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        WistModuleSelection.ValidateCanonicalPackageProvenance(plan);

        var security = plan.Definition.Metadata.TryGetValue(SecurityMetadata, out var securityValue)
            ? securityValue switch
            {
                "trusted" => DialectSecurityProfile.Trusted,
                "restricted" => DialectSecurityProfile.Restricted,
                _ => throw new InvalidOperationException($"Unsupported Wist security profile '{securityValue}'.")
            }
            : (DialectSecurityProfile?)null;

        var capabilities = plan.Definition.Metadata
            .Where(static pair => pair.Key.StartsWith(CapabilityPrefix, StringComparison.Ordinal))
            .ToDictionary(
                static pair => pair.Key[CapabilityPrefix.Length..],
                static pair => bool.Parse(pair.Value),
                StringComparer.Ordinal);
        if (plan.Definition.RuntimePolicy.AllowHostInterop &&
            WistModuleSelection.GetModuleAliases(plan).Contains("CSharpInterop", StringComparer.Ordinal))
        {
            capabilities["unsafe-interop"] = true;
        }

        return new DialectDefinitionSlice(
            plan.Definition.Id.Value,
            WistModuleSelection.GetModuleAliases(plan),
            [],
            [],
            plan.Definition.Backends.Select(static backend =>
                new DialectBackendDirective(new DialectBackendId(backend.Value), enabled: true)),
            [],
            WistModuleSelection.GetOptimizerAliases(plan).Select(static alias =>
                new DialectOptimizerDirective(alias, enabled: true, DialectBackendSelector.Any)),
            security,
            capabilities
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .Select(static pair => new DialectCapabilityDirective(pair.Key, pair.Value))
                .ToArray(),
            version: plan.Definition.Version.Value);
    }
}
