using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Compiles dialect source, builds a validated plan and resolves exact runtime components.
/// The workflow is language-neutral and does not create a language-specific service provider.
/// </summary>
public sealed class ToolchainCompositionWorkflow
{
    private readonly IDialectCompiledDialectBuildPlanBuilder _buildPlanBuilder;
    private readonly IDialectDslCompilerFactory _compilerFactory;
    private readonly SelectedRuntimePlanResolver _resolver;

    public ToolchainCompositionWorkflow(
        IDialectDslCompilerFactory compilerFactory,
        IDialectCompiledDialectBuildPlanBuilder buildPlanBuilder,
        SelectedRuntimePlanResolver resolver)
    {
        _compilerFactory = compilerFactory.ArgNotNull();
        _buildPlanBuilder = buildPlanBuilder.ArgNotNull();
        _resolver = resolver.ArgNotNull();
    }

    public DialectFrameworkCompositionResult ComposeFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            Thrower.Argument(nameof(filePath), "Dialect file path must not be empty.");
        if (!File.Exists(filePath))
            Thrower.FileNotFound(filePath);
        return ComposeText(File.ReadAllText(filePath), Path.GetFileName(filePath));
    }

    public DialectFrameworkCompositionResult ComposeText(string sourceText, string sourceName)
    {
        sourceText = sourceText.ArgNotNull();
        if (string.IsNullOrWhiteSpace(sourceName))
            Thrower.Argument(nameof(sourceName), "Source name must not be empty.");

        using var compiler = _compilerFactory.Create();
        var compiled = compiler.Compile(sourceText);
        var buildPlan = _buildPlanBuilder.Build(compiled);
        return ComposeCompiled(sourceName, compiled, buildPlan);
    }

    public DialectFrameworkCompositionResult ComposeCompiled(
        string sourceName,
        DialectDefinitionSlice compiled,
        DialectBuildPlan buildPlan)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            Thrower.Argument(nameof(sourceName), "Source name must not be empty.");
        compiled = compiled.ArgNotNull();
        buildPlan = buildPlan.ArgNotNull();

        var semanticErrors = buildPlan.ValidationResult.Diagnostics
            .Where(static x => x.Severity == DialectDiagnosticSeverity.Error)
            .ToArray();
        if (!buildPlan.CanBuild)
            return new DialectFrameworkCompositionResult(sourceName, compiled, buildPlan, semanticErrors, []);

        var selectedRuntimePlan = _resolver.Resolve(buildPlan);
        var resolutionErrors = selectedRuntimePlan.Diagnostics
            .Where(static x => x.Severity == DialectDiagnosticSeverity.Error)
            .Where(x => !semanticErrors.Contains(x))
            .ToArray();

        return new DialectFrameworkCompositionResult(
            sourceName,
            compiled,
            buildPlan,
            semanticErrors,
            resolutionErrors,
            selectedRuntimePlan);
    }
}
