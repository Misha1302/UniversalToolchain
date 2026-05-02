using System.Text;

namespace UniversalToolchain.Capabilities.Core;

public static class DialectFeatureExplanationFormatter
{
    public static string FormatDeterministic(DialectFeatureExplanation explanation)
    {
        ArgumentNullException.ThrowIfNull(explanation);

        var builder = new StringBuilder();
        builder.AppendLine($"Dialect: {explanation.DialectName}");
        builder.AppendLine($"Available features: {Join(explanation.AvailableFeatures.Select(static x => x.FeatureId.Value))}");
        builder.AppendLine($"Unavailable known features: {FormatUnavailableFeatures(explanation.UnavailableKnownFeatures)}");
        builder.AppendLine($"Available symbols: {Join(explanation.AvailableSymbols.Select(static x => x.Name))}");
        builder.AppendLine($"Available functions: {Join(explanation.AvailableFunctions.Select(static x => x.Name))}");
        builder.AppendLine($"Backend support: {Join(explanation.BackendSupport)}");
        return builder.ToString().TrimEnd();
    }

    private static string FormatUnavailableFeatures(IEnumerable<DialectFeatureExplanation.UnavailableFeatureExplanation> unavailableFeatures)
    {
        var materialized = unavailableFeatures
            .Select(static x => $"{x.Feature.FeatureId.Value}[{string.Join("; ", x.Reasons)}]")
            .ToList();
        return materialized.Count == 0 ? "none" : string.Join(" | ", materialized);
    }

    private static string Join(IEnumerable<string> values)
    {
        var materialized = values.ToList();
        return materialized.Count == 0 ? "none" : string.Join(", ", materialized);
    }
}