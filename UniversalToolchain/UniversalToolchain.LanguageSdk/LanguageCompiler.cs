using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.LanguageSdk;

/// <summary>
/// The single public semantic planner for language definitions. Internal phases are deterministic
/// implementation details and never materialize runtimes or expose alternative planning entrypoints.
/// </summary>
public sealed class LanguageCompiler
{
    private readonly LanguageFeatureResolutionPhase _features;
    private readonly LanguageContributionResolutionPhase _contributions;

    public LanguageCompiler(LanguagePackageRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _features = new LanguageFeatureResolutionPhase(registry);
        _contributions = new LanguageContributionResolutionPhase(registry);
    }

    public LanguageBuildResult Compile(LanguageDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var diagnostics = new List<LanguageDiagnostic>();
        ValidateToolchainApi(definition, diagnostics);

        var resolvedFeatures = _features.Resolve(definition, diagnostics);
        if (LanguagePlanningDiagnostics.HasErrors(diagnostics))
            return LanguageBuildResult.Failure(diagnostics);

        var contributionResult = _contributions.Resolve(definition, resolvedFeatures, diagnostics);
        if (LanguagePlanningDiagnostics.HasErrors(diagnostics))
            return LanguageBuildResult.Failure(diagnostics);

        var routes = contributionResult.RuntimeProvider == null
            ? []
            : LanguageArtifactRoutePhase.Build(
                definition,
                contributionResult.Contributions,
                contributionResult.RuntimeProvider,
                diagnostics);
        if (LanguagePlanningDiagnostics.HasErrors(diagnostics))
            return LanguageBuildResult.Failure(diagnostics);

        return LanguageBuildResult.Success(new LanguagePlan(
            definition,
            resolvedFeatures,
            contributionResult.Contributions,
            contributionResult.RuntimeProvider,
            routes));
    }

    private static void ValidateToolchainApi(
        LanguageDefinition definition,
        ICollection<LanguageDiagnostic> diagnostics)
    {
        if (definition.ToolchainApiVersion == ToolchainApi.Current)
            return;

        diagnostics.Add(LanguagePlanningDiagnostics.Error(
            "UTL1501",
            "planning",
            $"Language targets Toolchain API {definition.ToolchainApiVersion.Major}, but this SDK supports {ToolchainApi.Current.Major}.",
            definition.Id.Value,
            "Target the installed Toolchain API or install a compatible SDK."));
    }
}
