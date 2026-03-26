using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     End-to-end Wist workflow that composes dialect DSL and materializes a runnable runtime host.
/// </summary>
public sealed class WistDialectExecutionWorkflow
{
    private readonly IDialectCompiledDialectBuildPlanBuilder _buildPlanBuilder;
    private readonly DialectDslCompiler _compiler;
    private readonly DialectRuntimeProviderFactory _providerFactory;
    private readonly DialectRuntimeSelectionResolver _selectionResolver;

    public WistDialectExecutionWorkflow(
        DialectDslCompiler compiler,
        IDialectCompiledDialectBuildPlanBuilder buildPlanBuilder,
        DialectRuntimeSelectionResolver selectionResolver,
        DialectRuntimeProviderFactory providerFactory)
    {
        if (compiler == null)
            Thrower.ArgumentNull(nameof(compiler));

        if (buildPlanBuilder == null)
            Thrower.ArgumentNull(nameof(buildPlanBuilder));

        if (selectionResolver == null)
            Thrower.ArgumentNull(nameof(selectionResolver));

        if (providerFactory == null)
            Thrower.ArgumentNull(nameof(providerFactory));

        _compiler = compiler;
        _buildPlanBuilder = buildPlanBuilder;
        _selectionResolver = selectionResolver;
        _providerFactory = providerFactory;
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
        if (sourceText == null)
            Thrower.ArgumentNull(nameof(sourceText));

        if (string.IsNullOrWhiteSpace(sourceName))
            Thrower.Argument(nameof(sourceName), "Source name must not be empty.");

        var compiled = _compiler.Compile(sourceText);
        var buildPlan = _buildPlanBuilder.Build(compiled);
        var selection = _selectionResolver.Resolve(buildPlan);

        var runtimeComposition = new DialectRuntimeComposition(
            buildPlan.Name,
            selection.OrderedModules.Select(x => new RuntimeModuleDescriptor(x.CanonicalAlias, x.ImplementationType, x.Aliases)),
            selection.EnabledBackends.Select(x => new RuntimeBackendDescriptor(x.CanonicalId, x.ImplementationType, x.Aliases)),
            selection.EnabledOptimizers.Select(x => new RuntimeOptimizerDescriptor(x.CanonicalAlias, x.ImplementationType, x.Aliases)),
            [],
            new DialectValidationResult(selection.Diagnostics));

        var semanticErrors = buildPlan.ValidationResult.Diagnostics.Where(x => x.Severity == DialectDiagnosticSeverity.Error).ToList();
        var resolutionErrors = selection.Diagnostics.Where(x => x.Severity == DialectDiagnosticSeverity.Error).Except(semanticErrors).ToList();
        return new DialectFrameworkCompositionResult(sourceName, compiled, buildPlan, runtimeComposition, semanticErrors, resolutionErrors);
    }

    public WistDialectExecutionHost CreateHost(DialectFrameworkCompositionResult compositionResult)
    {
        if (compositionResult == null)
            Thrower.ArgumentNull(nameof(compositionResult));

        if (!compositionResult.IsSuccess || compositionResult.BuildPlan == null || compositionResult.RuntimeComposition == null)
            Thrower.Argument(nameof(compositionResult), "Dialect composition result must be successful before a runtime host can be created.");

        var selection = _selectionResolver.Resolve(compositionResult.BuildPlan);
        var configuration = _providerFactory.CreateConfiguration(selection, compositionResult.BuildPlan);
        var provider = _providerFactory.CreateProvider(selection, compositionResult.BuildPlan);

        return new WistDialectExecutionHost(provider, configuration);
    }
}
