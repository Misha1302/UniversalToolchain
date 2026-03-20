namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     End-to-end framework-native workflow: compile DSL source through UniversalToolchain, build validated plan, resolve
///     runtime composition.
/// </summary>
public sealed class DialectFrameworkCompositionWorkflow
{
    private readonly IDialectCompiledDialectBuildPlanBuilder _buildPlanBuilder;
    private readonly DialectDslCompiler _compiler;
    private readonly IDialectRuntimeCompositionResolver _resolver;

    public DialectFrameworkCompositionWorkflow(
        DialectDslCompiler compiler,
        IDialectCompiledDialectBuildPlanBuilder buildPlanBuilder,
        IDialectRuntimeCompositionResolver resolver)
    {
        if (compiler == null)
            Thrower.ArgumentNull(nameof(compiler));

        if (buildPlanBuilder == null)
            Thrower.ArgumentNull(nameof(buildPlanBuilder));

        if (resolver == null)
            Thrower.ArgumentNull(nameof(resolver));

        _compiler = compiler;
        _buildPlanBuilder = buildPlanBuilder;
        _resolver = resolver;
    }

    public DialectFrameworkCompositionResult ComposeText(
        string sourceText,
        DialectRuntimeDescriptorRegistry registry,
        string sourceName = "inline")
    {
        if (sourceText == null)
            Thrower.ArgumentNull(nameof(sourceText));

        if (registry == null)
            Thrower.ArgumentNull(nameof(registry));

        if (string.IsNullOrWhiteSpace(sourceName))
            Thrower.Argument(nameof(sourceName), "Source name must not be empty.");

        var compiled = _compiler.Compile(sourceText);
        var buildPlan = _buildPlanBuilder.Build(compiled);

        var semanticErrors = buildPlan.ValidationResult.Diagnostics
            .Where(x => x.Severity == DialectDiagnosticSeverity.Error)
            .ToList();

        if (!buildPlan.CanBuild)
            return new DialectFrameworkCompositionResult(sourceName, compiled, buildPlan, null, semanticErrors, []);

        var composition = _resolver.Resolve(buildPlan, registry);
        var resolutionErrors = composition.Diagnostics.Diagnostics
            .Where(x => x.Severity == DialectDiagnosticSeverity.Error)
            .Where(x => !semanticErrors.Contains(x))
            .ToList();

        return new DialectFrameworkCompositionResult(sourceName, compiled, buildPlan, composition, semanticErrors, resolutionErrors);
    }
}