using System.Text;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Represents deterministic demo output for a framework-native dialect pipeline run.
/// </summary>
public sealed class DialectFrameworkDemoReport
{
    public DialectFrameworkDemoReport(string sourceName, DialectFrameworkCompositionResult? compositionResult, string? compilationError)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            Thrower.Argument(nameof(sourceName), "Source name must not be empty.");

        SourceName = sourceName;
        CompositionResult = compositionResult;
        CompilationError = compilationError;
    }

    public string SourceName { get; }

    public DialectFrameworkCompositionResult? CompositionResult { get; }

    public string? CompilationError { get; }

    public bool IsSuccess => CompositionResult != null && CompositionResult.IsSuccess && string.IsNullOrWhiteSpace(CompilationError);

    public string ToDeterministicText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Source: {SourceName}");

        if (!string.IsNullOrWhiteSpace(CompilationError))
        {
            sb.AppendLine("Compile: failed");
            sb.AppendLine($"Compilation error: {CompilationError}");
            return sb.ToString().TrimEnd();
        }

        if (CompositionResult == null)
        {
            sb.AppendLine("Compile: failed");
            sb.AppendLine("Compilation error: <none>");
            return sb.ToString().TrimEnd();
        }

        var result = CompositionResult;
        sb.AppendLine("Compile: ok");
        sb.AppendLine($"Semantic: {(result.SemanticDiagnostics.Count == 0 ? "ok" : "failed")}");
        sb.AppendLine($"Resolve: {(result.ResolutionDiagnostics.Count == 0 && result.RuntimeComposition != null ? "ok" : "failed")}");

        AppendDiagnostics(sb, "Semantic diagnostics", result.SemanticDiagnostics);
        AppendDiagnostics(sb, "Resolution diagnostics", result.ResolutionDiagnostics);

        if (result.BuildPlan != null)
        {
            sb.AppendLine("Build plan:");
            sb.AppendLine($"  Dialect: {result.BuildPlan.Name}");
            sb.AppendLine($"  Ordered modules: {JoinPreserveOrder(result.BuildPlan.OrderedModules)}");
            sb.AppendLine($"  Enabled backends: {JoinSorted(result.BuildPlan.EnabledBackends.Select(DialectBackendSelectorText.ToText))}");
        }

        if (result.RuntimeComposition != null)
        {
            sb.AppendLine("Runtime composition:");
            sb.AppendLine($"  Modules: {JoinPreserveOrder(result.RuntimeComposition.OrderedModules.Select(x => x.CanonicalId))}");
            sb.AppendLine($"  Backends: {JoinPreserveOrder(result.RuntimeComposition.EnabledBackends.Select(x => x.CanonicalId))}");
            sb.AppendLine($"  Optimizers: {JoinPreserveOrder(result.RuntimeComposition.EnabledOptimizers.Select(x => x.CanonicalId))}");
            sb.AppendLine($"  Intrinsics: {JoinPreserveOrder(result.RuntimeComposition.AllowedIntrinsics.Select(x => x.CanonicalId))}");
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
}