using System.Text;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Deterministic formatting helpers for Wist dialect composition output.
/// </summary>
public static class WistDialectCompositionFormattingExtensions
{
    public static string ToDeterministicText(this DialectFrameworkCompositionResult result)
    {
        result = result.ArgNotNull();

        var builder = new StringBuilder();
        builder.AppendLine($"Source: {result.SourceName}");
        builder.AppendLine($"Success: {result.IsSuccess}");

        if (result.CompiledDialect != null)
            builder.AppendLine($"Dialect: {result.CompiledDialect.Name}");

        if (result.BuildPlan != null)
        {
            builder.AppendLine($"Ordered modules: {Join(result.BuildPlan.OrderedModules)}");
            builder.AppendLine($"Enabled backends: {Join(result.BuildPlan.EnabledBackends.Select(DialectBackendSelectorText.ToText))}");
            builder.AppendLine($"Enabled optimizers: {Join(result.BuildPlan.OptimizerDirectives.Where(x => x.Enabled).Select(x => x.Name))}");
        }

        builder.AppendLine($"Semantic diagnostics: {FormatDiagnostics(result.SemanticDiagnostics)}");
        builder.AppendLine($"Resolution diagnostics: {FormatDiagnostics(result.ResolutionDiagnostics)}");

        return builder.ToString().TrimEnd();
    }

    private static string Join(IEnumerable<string> values)
    {
        return string.Join(", ", values.OrderBy(x => x, StringComparer.Ordinal));
    }

    private static string FormatDiagnostics(IEnumerable<DialectDiagnostic> diagnostics)
    {
        var materialized = diagnostics.OrderBy(x => x.Code, StringComparer.Ordinal).ThenBy(x => x.Message, StringComparer.Ordinal).ToList();
        return materialized.Count == 0
            ? "none"
            : string.Join(" | ", materialized.Select(x => $"{x.Code}: {x.Message}"));
    }
}