using System.Text;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public static class DialectCompositionExplanationFormatter
{
    public static string FormatDeterministic(DialectCompositionExplanation explanation)
    {
        explanation = explanation.ArgNotNull();

        var builder = new StringBuilder();
        builder.AppendLine($"Source: {explanation.SourceName}");
        builder.AppendLine($"Success: {explanation.IsSuccess}");
        builder.AppendLine($"Dialect: {ToTextOrNone(explanation.DialectName)}");
        builder.AppendLine($"Version: {ToTextOrNone(explanation.DialectVersion)}");

        FormatBuildPlan(builder, explanation.BuildPlan);
        FormatRuntimeSelection(builder, explanation.RuntimeSelection);

        builder.AppendLine($"Semantic diagnostics: {FormatDiagnostics(explanation.SemanticDiagnostics)}");
        builder.AppendLine($"Resolution diagnostics: {FormatDiagnostics(explanation.ResolutionDiagnostics)}");
        return builder.ToString().TrimEnd();
    }

    private static void FormatBuildPlan(StringBuilder builder, DialectBuildPlanExplanation? buildPlan)
    {
        if (buildPlan == null)
        {
            builder.AppendLine("Build plan: none");
            return;
        }

        builder.AppendLine("Build plan: present");
        builder.AppendLine($"Build can run: {buildPlan.CanBuild}");
        builder.AppendLine($"Ordered modules: {JoinStrings(buildPlan.OrderedModules)}");
        builder.AppendLine($"Enabled backends: {JoinBackends(buildPlan.EnabledBackends)}");
        builder.AppendLine($"Disabled backends: {JoinBackends(buildPlan.DisabledBackends)}");
        builder.AppendLine($"Intrinsic directives: {JoinIntrinsicDirectives(buildPlan.IntrinsicDirectives)}");
        builder.AppendLine($"Optimizer directives: {JoinOptimizerDirectives(buildPlan.OptimizerDirectives)}");
        builder.AppendLine($"Security profile: {buildPlan.SecurityProfile?.ToString() ?? "none"}");
        builder.AppendLine($"Capabilities: {JoinCapabilities(buildPlan.Capabilities)}");
    }

    private static void FormatRuntimeSelection(StringBuilder builder, DialectRuntimeSelectionExplanation? runtimeSelection)
    {
        if (runtimeSelection == null)
        {
            builder.AppendLine("Runtime selection: none");
            return;
        }

        builder.AppendLine($"Runtime selection kind: {runtimeSelection.SelectionKind}");
        builder.AppendLine($"Runtime selection resolved: {runtimeSelection.IsResolved}");
        builder.AppendLine($"Runtime components resolved: {runtimeSelection.HasResolvedRuntimeComponents}");
        builder.AppendLine($"Runtime diagnostics: {FormatDiagnostics(runtimeSelection.Diagnostics)}");

        if (!runtimeSelection.HasResolvedRuntimeComponents)
        {
            builder.AppendLine("Runtime ordered modules: <not-available>");
            builder.AppendLine("Runtime enabled optimizers: <not-available>");
            builder.AppendLine("Runtime enabled backends: <not-available>");
            return;
        }

        builder.AppendLine($"Runtime ordered modules: {JoinRuntimeEntries(runtimeSelection.OrderedModules)}");
        builder.AppendLine($"Runtime enabled optimizers: {JoinRuntimeEntries(runtimeSelection.EnabledOptimizers)}");
        builder.AppendLine($"Runtime enabled backends: {JoinRuntimeEntries(runtimeSelection.EnabledBackends)}");
    }

    private static string JoinStrings(IEnumerable<string> values)
    {
        var materialized = values.ToList();
        return materialized.Count == 0 ? "none" : string.Join(", ", materialized);
    }

    private static string JoinBackends(IEnumerable<DialectBackendId> values)
    {
        var materialized = values.Select(static x => x.ToString()).OrderBy(static x => x, StringComparer.Ordinal).ToList();
        return materialized.Count == 0 ? "none" : string.Join(", ", materialized);
    }

    private static string JoinIntrinsicDirectives(IEnumerable<IntrinsicBuildDirective> values)
    {
        var materialized = values
            .Select(static x => $"{x.Name}[allowed={x.Allowed};target={x.Target}]")
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
        return materialized.Count == 0 ? "none" : string.Join(" | ", materialized);
    }

    private static string JoinOptimizerDirectives(IEnumerable<OptimizerBuildDirective> values)
    {
        var materialized = values
            .Select(static x => $"{x.Name}[enabled={x.Enabled};target={x.Target}]")
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();
        return materialized.Count == 0 ? "none" : string.Join(" | ", materialized);
    }

    private static string JoinRuntimeEntries(IEnumerable<RuntimeComponentManifestEntry> values)
    {
        var materialized = values.Select(static x => x.CanonicalAlias).ToList();
        return materialized.Count == 0 ? "none" : string.Join(", ", materialized);
    }

    private static string JoinCapabilities(IReadOnlyDictionary<string, bool> capabilities)
    {
        var materialized = capabilities
            .OrderBy(static x => x.Key, StringComparer.Ordinal)
            .Select(static x => $"{x.Key}={x.Value}")
            .ToList();
        return materialized.Count == 0 ? "none" : string.Join(", ", materialized);
    }

    private static string FormatDiagnostics(IEnumerable<DialectDiagnostic> diagnostics)
    {
        var materialized = diagnostics.ToList();
        return materialized.Count == 0
            ? "none"
            : string.Join(" | ", materialized.Select(static x => $"{x.Code}: {x.Message} ({x.Severity})"));
    }

    private static string ToTextOrNone(string? value) => string.IsNullOrWhiteSpace(value) ? "none" : value;
}
