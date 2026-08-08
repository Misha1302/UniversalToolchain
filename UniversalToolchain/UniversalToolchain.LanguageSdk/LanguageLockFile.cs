using System.Text;
using System.Text.Json;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

public static class LanguageLockFile
{
    public const int SchemaVersion = 6;
    public const string Canonicalization = "universaltoolchain-json-v1";

    public static string Serialize(LanguagePlan plan)
    {
        var bytes = SerializeBytes(plan, indented: true);
        return NormalizeLineEndings(Encoding.UTF8.GetString(bytes)) + "\n";
    }

    public static byte[] SerializeCanonical(LanguagePlan plan) => SerializeBytes(plan, indented: false);

    private static byte[] SerializeBytes(LanguagePlan plan, bool indented)
    {
        ArgumentNullException.ThrowIfNull(plan);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteString("canonicalization", Canonicalization);
            writer.WriteString("languageId", plan.Definition.Id.Value);
            writer.WriteString("languageVersion", plan.Definition.Version.Value);
            writer.WriteNumber("toolchainApiMajor", plan.Definition.ToolchainApiVersion.Major);
            writer.WriteString("planHash", plan.PlanHash);
            writer.WritePropertyName("entryArtifact");
            WriteArtifactContract(writer, plan.Definition.EntryArtifact);
            if (plan.RuntimeProvider != null && plan.RuntimeProviderContribution != null)
            {
                writer.WritePropertyName("runtimeProvider");
                writer.WriteStartObject();
                writer.WriteString("id", plan.RuntimeProvider.ProviderId.Value);
                writer.WriteString("version", plan.RuntimeProvider.Version.Value);
                writer.WriteString("contribution", plan.RuntimeProviderContribution.Contribution.Id.Value);
                writer.WriteEndObject();
            }
            writer.WritePropertyName("policy");
            writer.WriteStartObject();
            writer.WriteBoolean("requireDeterminism", plan.Definition.RuntimePolicy.RequireDeterminism);
            writer.WriteBoolean("allowHostInterop", plan.Definition.RuntimePolicy.AllowHostInterop);
            if (plan.Definition.RuntimePolicy.MaximumSourceLength is int maxSource)
                writer.WriteNumber("maximumSourceLength", maxSource);
            if (plan.Definition.RuntimePolicy.MaximumExternalParameters is int maxParameters)
                writer.WriteNumber("maximumExternalParameters", maxParameters);
            writer.WriteEndObject();
            writer.WritePropertyName("backends");
            writer.WriteStartArray();
            foreach (var backend in plan.Definition.Backends.OrderBy(static x => x.Value, StringComparer.Ordinal))
                writer.WriteStringValue(backend.Value);
            writer.WriteEndArray();
            writer.WritePropertyName("features");
            writer.WriteStartArray();
            foreach (var feature in plan.Features.OrderBy(static x => x.Feature.Id.Value, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("id", feature.Feature.Id.Value);
                writer.WriteString("packageId", feature.PackageId.Value);
                writer.WriteString("packageVersion", feature.PackageVersion.Value);
                writer.WriteString("manifestSha256", feature.ManifestSha256);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("contributions");
            writer.WriteStartArray();
            foreach (var contribution in plan.Contributions)
            {
                writer.WriteStartObject();
                writer.WriteString("id", contribution.Contribution.Id.Value);
                writer.WriteString("slot", contribution.Contribution.Slot.Value);
                writer.WriteNumber("order", contribution.Contribution.Order);
                writer.WriteString("packageId", contribution.PackageId.Value);
                writer.WriteString("packageVersion", contribution.PackageVersion.Value);
                writer.WriteString("manifestSha256", contribution.ManifestSha256);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("routes");
            writer.WriteStartArray();
            foreach (var route in plan.Routes.Values.OrderBy(static x => x.Backend.Value, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("backend", route.Backend.Value);
                writer.WritePropertyName("source");
                WriteArtifactContract(writer, route.SourceContract);
                writer.WritePropertyName("target");
                WriteArtifactContract(writer, route.TargetContract);
                writer.WriteNumber("totalCost", route.TotalCost);
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
            writer.WritePropertyName("contributionOrderConstraints");
            writer.WriteStartArray();
            foreach (var constraint in plan.Definition.ContributionOrderConstraints)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", constraint.Kind.ToString());
                writer.WriteString("source", constraint.Source.Value);
                writer.WriteString("target", constraint.Target.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("intrinsicPolicy");
            writer.WriteStartArray();
            foreach (var directive in plan.Definition.IntrinsicPolicy)
            {
                writer.WriteStartObject();
                writer.WriteString("intrinsic", directive.Intrinsic.Value);
                writer.WriteBoolean("allowed", directive.Allowed);
                if (directive.Backend is { } backend)
                    writer.WriteString("backend", backend.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach (var pair in plan.Definition.Metadata.OrderBy(static x => x.Key, StringComparer.Ordinal))
                writer.WriteString(pair.Key, pair.Value);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static void WriteArtifactContract(Utf8JsonWriter writer, LanguageArtifactContract contract)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", contract.Kind.Value);
        if (contract.ValueTypeIdentity != null)
            writer.WriteString("type", contract.ValueTypeIdentity);
        writer.WriteEndObject();
    }
}
