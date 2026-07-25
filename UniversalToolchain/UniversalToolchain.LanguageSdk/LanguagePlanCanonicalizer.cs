using System.Security.Cryptography;
using System.Text.Json;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

internal static class LanguagePlanCanonicalizer
{
    public const int FormatVersion = 1;

    public static string ComputeHash(
        LanguageDefinition definition,
        IReadOnlyList<ResolvedLanguageFeature> features,
        IReadOnlyList<ResolvedLanguageContribution> contributions,
        ResolvedLanguageContribution? runtimeProvider,
        IEnumerable<LanguageArtifactRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(contributions);
        ArgumentNullException.ThrowIfNull(routes);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("formatVersion", FormatVersion);
            writer.WriteString("languageId", definition.Id.Value);
            writer.WriteString("languageVersion", definition.Version.Value);
            writer.WriteNumber("toolchainApiMajor", definition.ToolchainApiVersion.Major);
            if (runtimeProvider != null)
            {
                writer.WriteString("runtimeProviderId", runtimeProvider.Contribution.RuntimeProviderId!.Value.Value);
                writer.WriteString("runtimeProviderVersion", runtimeProvider.Contribution.RuntimeProviderVersion!.Value.Value);
            }
            writer.WritePropertyName("entryArtifact");
            WriteArtifactContract(writer, definition.EntryArtifact);
            writer.WritePropertyName("policy");
            writer.WriteStartObject();
            writer.WriteBoolean("requireDeterminism", definition.RuntimePolicy.RequireDeterminism);
            writer.WriteBoolean("allowHostInterop", definition.RuntimePolicy.AllowHostInterop);
            if (definition.RuntimePolicy.MaximumSourceLength is int maxSource)
                writer.WriteNumber("maximumSourceLength", maxSource);
            if (definition.RuntimePolicy.MaximumExternalParameters is int maxParameters)
                writer.WriteNumber("maximumExternalParameters", maxParameters);
            writer.WriteEndObject();
            WriteStrings(writer, "backends", definition.Backends.Select(static x => x.Value));
            writer.WritePropertyName("features");
            writer.WriteStartArray();
            foreach (var feature in features.OrderBy(static x => x.Feature.Id.Value, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("id", feature.Feature.Id.Value);
                writer.WriteString("packageId", feature.PackageId.Value);
                writer.WriteString("packageVersion", feature.PackageVersion.Value);
                writer.WriteString("manifestSha256", feature.ManifestSha256);
                WriteRegistrationImplementation(writer, feature.PackageIdentity.ImplementationType);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("contributions");
            writer.WriteStartArray();
            foreach (var contribution in contributions
                         .OrderBy(static x => x.Contribution.Slot.Value, StringComparer.Ordinal)
                         .ThenBy(static x => x.Contribution.Order)
                         .ThenBy(static x => x.Contribution.Id.Value, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("id", contribution.Contribution.Id.Value);
                writer.WriteString("slot", contribution.Contribution.Slot.Value);
                writer.WriteString("packageId", contribution.PackageId.Value);
                writer.WriteString("packageVersion", contribution.PackageVersion.Value);
                writer.WriteString("manifestSha256", contribution.ManifestSha256);
                WriteRegistrationImplementation(writer, contribution.PackageIdentity.ImplementationType);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("routes");
            writer.WriteStartArray();
            foreach (var route in routes.OrderBy(static x => x.Backend.Value, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("backend", route.Backend.Value);
                writer.WritePropertyName("source");
                WriteArtifactContract(writer, route.SourceContract);
                writer.WritePropertyName("target");
                WriteArtifactContract(writer, route.TargetContract);
                writer.WritePropertyName("steps");
                writer.WriteStartArray();
                foreach (var step in route.Steps)
                {
                    writer.WriteStartObject();
                    writer.WriteString("contribution", step.ContributionId.Value);
                    writer.WritePropertyName("source");
                    WriteArtifactContract(writer, step.SourceContract);
                    writer.WritePropertyName("target");
                    WriteArtifactContract(writer, step.TargetContract);
                    writer.WriteNumber("cost", step.Cost);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("slotOverrides");
            writer.WriteStartArray();
            foreach (var item in definition.SlotOverrides)
            {
                writer.WriteStartObject();
                writer.WriteString("slot", item.Slot.Value);
                writer.WriteString("contribution", item.Contribution.Value);
                if (item.ExpectedCurrentOwner != null)
                    writer.WriteString("expectedCurrentOwner", item.ExpectedCurrentOwner.Value.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("capabilityProviders");
            writer.WriteStartObject();
            foreach (var item in definition.CapabilityProviders.OrderBy(static x => x.Key.Value, StringComparer.Ordinal))
                writer.WriteString(item.Key.Value, item.Value.Value);
            writer.WriteEndObject();
            WriteStrings(writer, "excludedContributions", definition.ExcludedContributions.Select(static x => x.Value));
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var pair in definition.Metadata.OrderBy(static x => x.Key, StringComparer.Ordinal))
                writer.WriteString(pair.Key, pair.Value);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static void WriteRegistrationImplementation(Utf8JsonWriter writer, Type? implementationType)
    {
        if (implementationType == null)
        {
            writer.WriteNull("registrationImplementation");
            return;
        }

        writer.WriteString(
            "registrationImplementation",
            $"{implementationType.Assembly.GetName().Name}:{implementationType.FullName}");
    }

    private static void WriteArtifactContract(Utf8JsonWriter writer, LanguageArtifactContract contract)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", contract.Kind.Value);
        if (contract.ValueTypeIdentity != null)
            writer.WriteString("type", contract.ValueTypeIdentity);
        writer.WriteEndObject();
    }

    private static void WriteStrings(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values.OrderBy(static x => x, StringComparer.Ordinal))
            writer.WriteStringValue(value);
        writer.WriteEndArray();
    }
}
