using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.FeatureSdk;

public static class LanguageFeatureManifestSerializer
{
    public const int SchemaVersion = 5;
    public const string Canonicalization = "universaltoolchain-json-v1";
    public const string HashAlgorithm = "sha256";

    public static string Serialize(LanguagePackageDescriptor package)
    {
        var bytes = SerializeBytes(package, indented: true);
        return NormalizeLineEndings(Encoding.UTF8.GetString(bytes)) + "\n";
    }

    public static byte[] SerializeCanonical(LanguagePackageDescriptor package) =>
        SerializeBytes(package, indented: false);

    public static string ComputeSha256(LanguagePackageDescriptor package) =>
        Convert.ToHexString(SHA256.HashData(SerializeCanonical(package))).ToLowerInvariant();

    public static LanguagePackageDescriptor Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var schemaVersion = root.GetProperty("schemaVersion").GetInt32();
        if (schemaVersion is < 1 or > SchemaVersion)
            throw new InvalidDataException("Unsupported toolchain feature manifest schema version.");
        if (schemaVersion >= 5)
        {
            var canonicalization = root.GetProperty("canonicalization").GetString();
            var hashAlgorithm = root.GetProperty("hashAlgorithm").GetString();
            if (!StringComparer.Ordinal.Equals(canonicalization, Canonicalization) ||
                !StringComparer.Ordinal.Equals(hashAlgorithm, HashAlgorithm))
            {
                throw new InvalidDataException("Unsupported toolchain feature manifest canonicalization contract.");
            }
        }
        var package = root.GetProperty("package");
        var features = package.GetProperty("features").EnumerateArray()
            .Select(element => ReadFeature(element, schemaVersion))
            .ToArray();
        var contributions = schemaVersion >= 2 && package.TryGetProperty("contributions", out var contributionArray)
            ? contributionArray.EnumerateArray().Select(ReadContribution).ToArray()
            : [];
        return new LanguagePackageDescriptor(
            new LanguagePackageId(package.GetProperty("id").GetString()!),
            new LanguageVersion(package.GetProperty("version").GetString()!),
            new ToolchainApiVersion(package.GetProperty("toolchainApiMajor").GetInt32()),
            features,
            ReadMetadata(package),
            contributions);
    }

    private static byte[] SerializeBytes(LanguagePackageDescriptor package, bool indented)
    {
        ArgumentNullException.ThrowIfNull(package);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteString("canonicalization", Canonicalization);
            writer.WriteString("hashAlgorithm", HashAlgorithm);
            writer.WritePropertyName("package");
            WritePackage(writer, package);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static void WritePackage(Utf8JsonWriter writer, LanguagePackageDescriptor package)
    {
        writer.WriteStartObject();
        writer.WriteString("id", package.Id.Value);
        writer.WriteString("version", package.Version.Value);
        writer.WriteNumber("toolchainApiMajor", package.ToolchainApiVersion.Major);
        WriteMetadata(writer, package.Metadata);
        writer.WritePropertyName("features");
        writer.WriteStartArray();
        foreach (var feature in package.Features.OrderBy(static x => x.Id.Value, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("id", feature.Id.Value);
            WriteIds(writer, "requires", feature.Requires.Select(static x => x.Value));
            WriteIds(writer, "conflicts", feature.Conflicts.Select(static x => x.Value));
            WriteIds(writer, "supportedBackends", feature.SupportedBackends.Select(static x => x.Value));
            WriteIds(writer, "contributions", feature.Contributions.Select(static x => x.Value));
            WriteMetadata(writer, feature.Metadata);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WritePropertyName("contributions");
        writer.WriteStartArray();
        foreach (var contribution in package.Contributions.OrderBy(static x => x.Id.Value, StringComparer.Ordinal))
            WriteContribution(writer, contribution);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteContribution(Utf8JsonWriter writer, LanguageContributionDescriptor contribution)
    {
        writer.WriteStartObject();
        writer.WriteString("id", contribution.Id.Value);
        writer.WriteString("slot", contribution.Slot.Value);
        writer.WriteString("multiplicity", contribution.Multiplicity.ToString());
        writer.WriteString("mergePolicy", contribution.MergePolicy.ToString());
        writer.WriteNumber("order", contribution.Order);
        WriteIds(writer, "requiresContributions", contribution.RequiresContributions.Select(static x => x.Value));
        WriteIds(writer, "providesCapabilities", contribution.ProvidesCapabilities.Select(static x => x.Value));
        WriteIds(writer, "requiresCapabilities", contribution.RequiresCapabilities.Select(static x => x.Value));
        WriteIds(writer, "conflicts", contribution.Conflicts.Select(static x => x.Value));
        WriteIds(writer, "conflictsCapabilities", contribution.ConflictsCapabilities.Select(static x => x.Value));
        WriteIds(writer, "supportedBackends", contribution.SupportedBackends.Select(static x => x.Value));
        WriteIds(writer, "beforeContributions", contribution.BeforeContributions.Select(static x => x.Value));
        WriteIds(writer, "afterContributions", contribution.AfterContributions.Select(static x => x.Value));
        if (contribution.Transformation != null)
        {
            writer.WritePropertyName("transformation");
            writer.WriteStartObject();
            writer.WriteString("source", contribution.Transformation.Source.Value);
            writer.WriteString("target", contribution.Transformation.Target.Value);
            if (contribution.Transformation.SourceContract.ValueTypeIdentity != null)
                writer.WriteString("sourceType", contribution.Transformation.SourceContract.ValueTypeIdentity);
            if (contribution.Transformation.TargetContract.ValueTypeIdentity != null)
                writer.WriteString("targetType", contribution.Transformation.TargetContract.ValueTypeIdentity);
            writer.WriteNumber("cost", contribution.Transformation.Cost);
            writer.WriteEndObject();
        }
        if (contribution.BackendInputContract != null)
        {
            writer.WritePropertyName("backendInput");
            writer.WriteStartObject();
            writer.WriteString("kind", contribution.BackendInputContract.Value.Kind.Value);
            if (contribution.BackendInputContract.Value.ValueTypeIdentity != null)
                writer.WriteString("type", contribution.BackendInputContract.Value.ValueTypeIdentity);
            writer.WriteEndObject();
        }
        if (contribution.RuntimeProviderId != null)
        {
            writer.WritePropertyName("runtimeProvider");
            writer.WriteStartObject();
            writer.WriteString("id", contribution.RuntimeProviderId.Value.Value);
            writer.WriteString("version", contribution.RuntimeProviderVersion!.Value.Value);
            writer.WritePropertyName("inputs");
            writer.WriteStartObject();
            foreach (var input in contribution.RuntimeInputContracts.OrderBy(static x => x.Key.Value, StringComparer.Ordinal))
            {
                writer.WritePropertyName(input.Key.Value);
                writer.WriteStartObject();
                writer.WriteString("kind", input.Value.Kind.Value);
                if (input.Value.ValueTypeIdentity != null)
                    writer.WriteString("type", input.Value.ValueTypeIdentity);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        WriteMetadata(writer, contribution.Metadata);
        writer.WriteEndObject();
    }

    private static LanguageFeatureDescriptor ReadFeature(JsonElement element, int schemaVersion) => new(
        new LanguageFeatureId(element.GetProperty("id").GetString()!),
        ReadStrings(element, "requires").Select(static x => new LanguageFeatureId(x)),
        ReadStrings(element, "conflicts").Select(static x => new LanguageFeatureId(x)),
        ReadStrings(element, "supportedBackends").Select(static x => new BackendId(x)),
        ReadMetadata(element),
        schemaVersion >= 2
            ? ReadStrings(element, "contributions").Select(static x => new LanguageContributionId(x))
            : []);

    private static LanguageContributionDescriptor ReadContribution(JsonElement element)
    {
        ArtifactTransformationDescriptor? transformation = null;
        if (element.TryGetProperty("transformation", out var transformationElement))
        {
            transformation = new ArtifactTransformationDescriptor(
                new LanguageArtifactContract(
                    new LanguageArtifactKindId(transformationElement.GetProperty("source").GetString()!),
                    transformationElement.TryGetProperty("sourceType", out var sourceType) ? sourceType.GetString() : null),
                new LanguageArtifactContract(
                    new LanguageArtifactKindId(transformationElement.GetProperty("target").GetString()!),
                    transformationElement.TryGetProperty("targetType", out var targetType) ? targetType.GetString() : null),
                transformationElement.GetProperty("cost").GetInt32());
        }

        LanguageRuntimeProviderId? runtimeProviderId = null;
        LanguageVersion? runtimeProviderVersion = null;
        IReadOnlyDictionary<BackendId, LanguageArtifactKindId>? runtimeInputs = null;
        IReadOnlyDictionary<BackendId, LanguageArtifactContract>? runtimeInputContracts = null;
        if (element.TryGetProperty("runtimeProvider", out var runtimeProviderElement))
        {
            runtimeProviderId = new LanguageRuntimeProviderId(runtimeProviderElement.GetProperty("id").GetString()!);
            runtimeProviderVersion = new LanguageVersion(runtimeProviderElement.GetProperty("version").GetString()!);
            var inputProperties = runtimeProviderElement.GetProperty("inputs").EnumerateObject().ToArray();
            if (inputProperties.Length != 0 && inputProperties[0].Value.ValueKind == JsonValueKind.Object)
            {
                runtimeInputContracts = inputProperties.ToDictionary(
                    static x => new BackendId(x.Name),
                    static x => new LanguageArtifactContract(
                        new LanguageArtifactKindId(x.Value.GetProperty("kind").GetString()!),
                        x.Value.TryGetProperty("type", out var type) ? type.GetString() : null));
            }
            else
            {
                runtimeInputs = inputProperties.ToDictionary(
                    static x => new BackendId(x.Name),
                    static x => new LanguageArtifactKindId(x.Value.GetString()!));
            }
        }

        LanguageArtifactContract? backendInputContract = null;
        if (element.TryGetProperty("backendInput", out var backendInputElement))
        {
            backendInputContract = new LanguageArtifactContract(
                new LanguageArtifactKindId(backendInputElement.GetProperty("kind").GetString()!),
                backendInputElement.TryGetProperty("type", out var backendInputType) ? backendInputType.GetString() : null);
        }

        return new LanguageContributionDescriptor(
            new LanguageContributionId(element.GetProperty("id").GetString()!),
            new LanguageSlotId(element.GetProperty("slot").GetString()!),
            Enum.Parse<LanguageSlotMultiplicity>(element.GetProperty("multiplicity").GetString()!, ignoreCase: false),
            Enum.Parse<ContributionMergePolicy>(element.GetProperty("mergePolicy").GetString()!, ignoreCase: false),
            ReadStrings(element, "requiresContributions").Select(static x => new LanguageContributionId(x)),
            ReadStrings(element, "providesCapabilities").Select(static x => new LanguageCapabilityId(x)),
            ReadStrings(element, "requiresCapabilities").Select(static x => new LanguageCapabilityId(x)),
            ReadStrings(element, "conflicts").Select(static x => new LanguageContributionId(x)),
            ReadStrings(element, "conflictsCapabilities").Select(static x => new LanguageCapabilityId(x)),
            ReadStrings(element, "supportedBackends").Select(static x => new BackendId(x)),
            transformation,
            runtimeProviderId,
            runtimeProviderVersion,
            runtimeInputs,
            element.TryGetProperty("order", out var order) ? order.GetInt32() : 0,
            ReadMetadata(element),
            runtimeInputContracts,
            backendInputContract,
            ReadStrings(element, "beforeContributions").Select(static x => new LanguageContributionId(x)),
            ReadStrings(element, "afterContributions").Select(static x => new LanguageContributionId(x)));
    }

    private static IReadOnlyDictionary<string, string> ReadMetadata(JsonElement element)
    {
        if (!element.TryGetProperty("metadata", out var metadata))
            return new Dictionary<string, string>();
        return metadata.EnumerateObject().ToDictionary(
            static x => x.Name,
            static x => x.Value.GetString() ?? string.Empty,
            StringComparer.Ordinal);
    }

    private static IEnumerable<string> ReadStrings(JsonElement element, string name) =>
        element.TryGetProperty(name, out var values)
            ? values.EnumerateArray().Select(static x => x.GetString()!).ToArray()
            : [];

    private static void WriteIds(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values.OrderBy(static x => x, StringComparer.Ordinal))
            writer.WriteStringValue(value);
        writer.WriteEndArray();
    }

    private static void WriteMetadata(Utf8JsonWriter writer, IReadOnlyDictionary<string, string> metadata)
    {
        writer.WritePropertyName("metadata");
        writer.WriteStartObject();
        foreach (var pair in metadata.OrderBy(static x => x.Key, StringComparer.Ordinal))
            writer.WriteString(pair.Key, pair.Value);
        writer.WriteEndObject();
    }
}
