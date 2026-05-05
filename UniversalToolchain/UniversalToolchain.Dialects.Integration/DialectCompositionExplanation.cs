using System.Collections.ObjectModel;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DialectCompositionExplanation
{
    private readonly ReadOnlyCollection<DialectDiagnostic> _resolutionDiagnostics;
    private readonly ReadOnlyCollection<DialectDiagnostic> _semanticDiagnostics;

    public DialectCompositionExplanation(
        string sourceName,
        bool isSuccess,
        string? dialectName,
        string? dialectVersion,
        DialectBuildPlanExplanation? buildPlan,
        DialectRuntimeSelectionExplanation? runtimeSelection,
        IEnumerable<DialectDiagnostic> semanticDiagnostics,
        IEnumerable<DialectDiagnostic> resolutionDiagnostics)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            Thrower.Argument(nameof(sourceName), "Source name must not be empty.");

        SourceName = sourceName;
        IsSuccess = isSuccess;
        DialectName = dialectName;
        DialectVersion = dialectVersion;
        BuildPlan = buildPlan;
        RuntimeSelection = runtimeSelection;
        _semanticDiagnostics = new ReadOnlyCollection<DialectDiagnostic>(Snapshot(semanticDiagnostics));
        _resolutionDiagnostics = new ReadOnlyCollection<DialectDiagnostic>(Snapshot(resolutionDiagnostics));
    }

    public string SourceName { get; }

    public bool IsSuccess { get; }

    public string? DialectName { get; }

    public string? DialectVersion { get; }

    public DialectBuildPlanExplanation? BuildPlan { get; }

    public DialectRuntimeSelectionExplanation? RuntimeSelection { get; }

    public IReadOnlyList<DialectDiagnostic> SemanticDiagnostics => _semanticDiagnostics;

    public IReadOnlyList<DialectDiagnostic> ResolutionDiagnostics => _resolutionDiagnostics;

    private static List<T> Snapshot<T>(IEnumerable<T> source)
    {
        source = source.ArgNotNull();
        return source.Select(item => item.NotNull()).ToList();
    }
}