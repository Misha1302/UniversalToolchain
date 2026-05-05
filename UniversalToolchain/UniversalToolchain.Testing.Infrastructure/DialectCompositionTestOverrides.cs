using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Testing.Infrastructure;

internal static class DialectCompositionTestOverrides
{
    public static DialectFrameworkCompositionResult WithOnlyBackend(
        DialectFrameworkCompositionResult composition,
        SelectedRuntimePlanResolver resolver,
        string backend)
    {
        composition = composition.ArgNotNull();
        resolver = resolver.ArgNotNull();

        if (string.IsNullOrWhiteSpace(backend))
            Thrower.Argument(nameof(backend), "Backend must not be empty.");

        if (!composition.IsSuccess || composition.CompiledDialect == null || composition.BuildPlan == null)
            Thrower.Argument(nameof(composition), "Composition must be successful before applying a backend override.");

        var sourcePlan = composition.BuildPlan;
        var backendId = new DialectBackendId(backend.Trim());
        var overriddenPlan = new DialectBuildPlan(
            sourcePlan.Name,
            sourcePlan.Version,
            sourcePlan.OrderedModules,
            [backendId],
            sourcePlan.DisabledBackends.Where(x => x != backendId),
            sourcePlan.IntrinsicDirectives,
            sourcePlan.OptimizerDirectives,
            sourcePlan.SecurityProfile,
            sourcePlan.Capabilities,
            sourcePlan.ValidationResult);

        var selectedRuntimePlan = resolver.Resolve(overriddenPlan);
        var resolutionErrors = selectedRuntimePlan.Diagnostics
            .Where(static x => x.Severity == DialectDiagnosticSeverity.Error)
            .ToList();

        return new DialectFrameworkCompositionResult(
            composition.SourceName,
            composition.CompiledDialect,
            overriddenPlan,
            composition.SemanticDiagnostics,
            resolutionErrors,
            selectedRuntimePlan);
    }
}