namespace UniversalToolchain.Dialects.Integration;

/// <summary>
///     Provides a minimal end-to-end inspect workflow for dialect DSL sources.
/// </summary>
public sealed class DialectInspectWorkflow
{
    private readonly IDialectBuildPlanBuilder _buildPlanBuilder;
    private readonly IDialectDefinitionParser _parser;
    private readonly IDialectRuntimeCompositionResolver _resolver;

    public DialectInspectWorkflow(
        IDialectDefinitionParser parser,
        IDialectBuildPlanBuilder buildPlanBuilder,
        IDialectRuntimeCompositionResolver resolver)
    {
        if (parser == null)
            Thrower.ArgumentNull(nameof(parser));

        if (buildPlanBuilder == null)
            Thrower.ArgumentNull(nameof(buildPlanBuilder));

        if (resolver == null)
            Thrower.ArgumentNull(nameof(resolver));

        _parser = parser;
        _buildPlanBuilder = buildPlanBuilder;
        _resolver = resolver;
    }

    public DialectInspectResult InspectFile(string filePath, DialectRuntimeDescriptorRegistry registry)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            Thrower.Argument(nameof(filePath), "File path must not be empty.");

        if (!File.Exists(filePath))
            Thrower.FileNotFound(filePath);

        var text = File.ReadAllText(filePath);
        return InspectText(text, Path.GetFileName(filePath), registry);
    }

    public DialectInspectResult InspectText(string text, string sourceName, DialectRuntimeDescriptorRegistry registry)
    {
        if (text == null)
            Thrower.ArgumentNull(nameof(text));

        if (string.IsNullOrWhiteSpace(sourceName))
            Thrower.Argument(nameof(sourceName), "Source name must not be empty.");

        if (registry == null)
            Thrower.ArgumentNull(nameof(registry));

        var parseResult = _parser.Parse(text);
        var parseDiagnostics = parseResult.Diagnostics.Where(x => x.Severity == DialectDiagnosticSeverity.Error).ToList();
        if (!parseResult.IsSuccess || parseResult.Document == null)
            return new DialectInspectResult(sourceName, null, null, parseDiagnostics, [], []);

        var buildPlan = _buildPlanBuilder.Build(parseResult.Document);
        var semanticDiagnostics = buildPlan.ValidationResult.Diagnostics.Where(x => x.Severity == DialectDiagnosticSeverity.Error).ToList();
        if (!buildPlan.CanBuild)
            return new DialectInspectResult(sourceName, buildPlan, null, [], semanticDiagnostics, []);

        var composition = _resolver.Resolve(buildPlan, registry);
        var resolutionDiagnostics = composition.Diagnostics.Diagnostics
            .Where(x => x.Severity == DialectDiagnosticSeverity.Error)
            .Where(x => !semanticDiagnostics.Contains(x))
            .ToList();

        return new DialectInspectResult(sourceName, buildPlan, composition, [], semanticDiagnostics, resolutionDiagnostics);
    }
}