using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Represents end-to-end composition result from framework-native DSL source through semantic build-plan to runtime
///     composition.
/// </summary>
public sealed class DialectFrameworkCompositionResult
{
    private readonly ReadOnlyCollection<DialectDiagnostic> _resolutionDiagnostics;
    private readonly ReadOnlyCollection<DialectDiagnostic> _semanticDiagnostics;

    public DialectFrameworkCompositionResult(
        string sourceName,
        DialectDefinitionSlice? compiledDialect,
        DialectBuildPlan? buildPlan,
        IEnumerable<DialectDiagnostic> semanticDiagnostics,
        IEnumerable<DialectDiagnostic> resolutionDiagnostics,
        IDialectRuntimeSelection? runtimeSelection = null)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            Thrower.Argument(nameof(sourceName), "Source name must not be empty.");

        SourceName = sourceName;
        CompiledDialect = compiledDialect;
        BuildPlan = buildPlan;
        _semanticDiagnostics = new ReadOnlyCollection<DialectDiagnostic>(Snapshot(semanticDiagnostics, nameof(semanticDiagnostics)));
        _resolutionDiagnostics = new ReadOnlyCollection<DialectDiagnostic>(Snapshot(resolutionDiagnostics, nameof(resolutionDiagnostics)));
        RuntimeSelection = runtimeSelection;
    }

    public string SourceName { get; }

    public DialectDefinitionSlice? CompiledDialect { get; }

    public DialectBuildPlan? BuildPlan { get; }

    public IDialectRuntimeSelection? RuntimeSelection { get; }

    public IReadOnlyList<DialectDiagnostic> SemanticDiagnostics => _semanticDiagnostics;

    public IReadOnlyList<DialectDiagnostic> ResolutionDiagnostics => _resolutionDiagnostics;

    public bool IsSuccess =>
        CompiledDialect != null &&
        BuildPlan != null &&
        !_semanticDiagnostics.Any(x => x.Severity == DialectDiagnosticSeverity.Error) &&
        !_resolutionDiagnostics.Any(x => x.Severity == DialectDiagnosticSeverity.Error);

    private static List<DialectDiagnostic> Snapshot(IEnumerable<DialectDiagnostic> diagnostics, [CallerArgumentExpression(nameof(diagnostics))] string? paramName = null)
    {
        if (diagnostics == null)
            Thrower.ArgumentNull(paramName);

        var result = new List<DialectDiagnostic>();
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic == null)
                Thrower.Argument(paramName, "Diagnostics collection must not contain null values.");

            result.Add(diagnostic);
        }

        return result;
    }
}