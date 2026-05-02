using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public static class DialectCompositionExplanationProjector
{
    public static DialectCompositionExplanation Project(DialectFrameworkCompositionResult result)
    {
        result = result.ArgNotNull();

        var buildPlan = result.BuildPlan == null
            ? null
            : new DialectBuildPlanExplanation(
                result.BuildPlan.CanBuild,
                result.BuildPlan.OrderedModules,
                result.BuildPlan.EnabledBackends,
                result.BuildPlan.DisabledBackends,
                result.BuildPlan.IntrinsicDirectives,
                result.BuildPlan.OptimizerDirectives,
                result.BuildPlan.SecurityProfile,
                result.BuildPlan.Capabilities);

        var runtimeSelection = ProjectRuntimeSelection(result.RuntimeSelection);

        return new DialectCompositionExplanation(
            result.SourceName,
            result.IsSuccess,
            result.CompiledDialect?.Name,
            result.BuildPlan?.Version,
            buildPlan,
            runtimeSelection,
            result.SemanticDiagnostics,
            result.ResolutionDiagnostics);
    }

    private static DialectRuntimeSelectionExplanation? ProjectRuntimeSelection(IDialectRuntimeSelection? runtimeSelection)
    {
        if (runtimeSelection == null)
            return null;

        var selectionType = runtimeSelection.GetType();
        var selectionKind = selectionType.FullName ?? selectionType.Name;
        if (runtimeSelection is IDialectResolvedRuntimeSelection resolvedSelection)
            return new DialectRuntimeSelectionExplanation(
                selectionKind,
                runtimeSelection.IsResolved,
                true,
                runtimeSelection.Diagnostics,
                resolvedSelection.OrderedModules,
                resolvedSelection.EnabledOptimizers,
                resolvedSelection.EnabledBackends);

        return new DialectRuntimeSelectionExplanation(
            selectionKind,
            runtimeSelection.IsResolved,
            false,
            runtimeSelection.Diagnostics,
            [],
            [],
            []);
    }
}