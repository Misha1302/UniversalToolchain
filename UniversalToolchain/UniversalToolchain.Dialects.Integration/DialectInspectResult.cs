using System.Collections.ObjectModel;
using System.Text;
using UniversalToolchain.Dialects.Abstractions;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Represents inspect/dry-run workflow output for one dialect source.
/// </summary>
public sealed class DialectInspectResult
{
    private readonly ReadOnlyCollection<DialectDiagnostic> _parseDiagnostics;
    private readonly ReadOnlyCollection<DialectDiagnostic> _semanticDiagnostics;
    private readonly ReadOnlyCollection<DialectDiagnostic> _resolutionDiagnostics;

    public DialectInspectResult(
        string source,
        DialectBuildPlan? buildPlan,
        DialectRuntimeComposition? runtimeComposition,
        IEnumerable<DialectDiagnostic> parseDiagnostics,
        IEnumerable<DialectDiagnostic> semanticDiagnostics,
        IEnumerable<DialectDiagnostic> resolutionDiagnostics)
    {
        if (string.IsNullOrWhiteSpace(source))
            Thrower.Argument(nameof(source), "Source name must not be empty.");

        Source = source;
        BuildPlan = buildPlan;
        RuntimeComposition = runtimeComposition;
        _parseDiagnostics = new ReadOnlyCollection<DialectDiagnostic>(Snapshot(parseDiagnostics, nameof(parseDiagnostics)));
        _semanticDiagnostics = new ReadOnlyCollection<DialectDiagnostic>(Snapshot(semanticDiagnostics, nameof(semanticDiagnostics)));
        _resolutionDiagnostics = new ReadOnlyCollection<DialectDiagnostic>(Snapshot(resolutionDiagnostics, nameof(resolutionDiagnostics)));
    }

    public string Source { get; }

    public DialectBuildPlan? BuildPlan { get; }

    public DialectRuntimeComposition? RuntimeComposition { get; }

    public IReadOnlyList<DialectDiagnostic> ParseDiagnostics => _parseDiagnostics;

    public IReadOnlyList<DialectDiagnostic> SemanticDiagnostics => _semanticDiagnostics;

    public IReadOnlyList<DialectDiagnostic> ResolutionDiagnostics => _resolutionDiagnostics;

    public bool IsSuccess => !_parseDiagnostics.Any() && !_semanticDiagnostics.Any() && !_resolutionDiagnostics.Any() && RuntimeComposition != null;

    public string ToDeterministicText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Source: {Source}");
        sb.AppendLine($"Parse: {(ParseDiagnostics.Count == 0 ? "ok" : "failed")}");
        sb.AppendLine($"Semantic: {(SemanticDiagnostics.Count == 0 ? "ok" : "failed")}");
        sb.AppendLine($"Resolve: {(ResolutionDiagnostics.Count == 0 && RuntimeComposition != null ? "ok" : "failed")}");

        AppendDiagnostics(sb, "Parse diagnostics", ParseDiagnostics);
        AppendDiagnostics(sb, "Semantic diagnostics", SemanticDiagnostics);
        AppendDiagnostics(sb, "Resolution diagnostics", ResolutionDiagnostics);

        if (BuildPlan != null)
        {
            sb.AppendLine("Build plan:");
            sb.AppendLine($"  Dialect: {BuildPlan.Name}");
            sb.AppendLine($"  Version: {(BuildPlan.Version ?? "<none>")}");
            sb.AppendLine($"  Ordered modules: {JoinPreserveOrder(BuildPlan.OrderedModules)}");
            sb.AppendLine($"  Enabled backends: {JoinSorted(BuildPlan.EnabledBackends.Select(DialectBackendTargetText.ToText))}");
            sb.AppendLine($"  Enabled optimizers: {JoinSorted(BuildPlan.OptimizerDirectives.Where(x => x.Enabled).Select(x => $"{x.Name}@{DialectBackendTargetText.ToText(x.Target)}"))}");
            sb.AppendLine($"  Allowed intrinsics: {JoinSorted(BuildPlan.IntrinsicDirectives.Where(x => x.Allowed).Select(x => $"{x.Name}@{DialectBackendTargetText.ToText(x.Target)}"))}");
        }

        if (RuntimeComposition != null)
        {
            sb.AppendLine("Runtime composition:");
            sb.AppendLine($"  Modules: {JoinPreserveOrder(RuntimeComposition.OrderedModules.Select(x => x.Name))}");
            sb.AppendLine($"  Backends: {JoinPreserveOrder(RuntimeComposition.EnabledBackends.Select(x => x.RuntimeName))}");
            sb.AppendLine($"  Optimizers: {JoinPreserveOrder(RuntimeComposition.EnabledOptimizers.Select(x => x.Name))}");
            sb.AppendLine($"  Intrinsics: {JoinPreserveOrder(RuntimeComposition.AllowedIntrinsics.Select(x => $"{x.Name}@{DialectBackendTargetText.ToText(x.Target)}"))}");
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendDiagnostics(StringBuilder sb, string title, IReadOnlyList<DialectDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            sb.AppendLine($"{title}: <none>");
            return;
        }

        sb.AppendLine($"{title}:");
        foreach (var diagnostic in diagnostics.OrderBy(x => x.Code, StringComparer.Ordinal).ThenBy(x => x.Message, StringComparer.Ordinal))
            sb.AppendLine($"  - {diagnostic.Code} [{diagnostic.Severity}] {diagnostic.Message}");
    }

    private static string JoinPreserveOrder(IEnumerable<string> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? "<none>" : string.Join(", ", list);
    }

    private static string JoinSorted(IEnumerable<string> values)
    {
        var list = values.OrderBy(x => x, StringComparer.Ordinal).ToList();
        return list.Count == 0 ? "<none>" : string.Join(", ", list);
    }
    private static List<DialectDiagnostic> Snapshot(IEnumerable<DialectDiagnostic> source, string paramName)
    {
        if (source == null)
            Thrower.ArgumentNull(paramName);

        var list = new List<DialectDiagnostic>();
        foreach (var item in source)
        {
            if (item == null)
                Thrower.Argument(paramName, "Diagnostics collection must not contain null entries.");

            list.Add(item);
        }

        return list;
    }

}
