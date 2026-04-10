using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistDialectExecutionWorkflow
{
    private readonly IDialectCompiledDialectBuildPlanBuilder _buildPlanBuilder;
    private readonly IDialectDslCompilerFactory _compilerFactory;
    private readonly WistDialectExecutionConfigurationBuilder _configurationBuilder;
    private readonly SelectedRuntimePlanResolver _resolver;
    private readonly WistDialectServiceProviderFactory _serviceProviderFactory;

    public WistDialectExecutionWorkflow(
        IDialectDslCompilerFactory compilerFactory,
        IDialectCompiledDialectBuildPlanBuilder buildPlanBuilder,
        SelectedRuntimePlanResolver resolver,
        WistDialectExecutionConfigurationBuilder configurationBuilder,
        WistDialectServiceProviderFactory serviceProviderFactory)
    {
        if (compilerFactory == null)
            Thrower.ArgumentNull(nameof(compilerFactory));

        if (buildPlanBuilder == null)
            Thrower.ArgumentNull(nameof(buildPlanBuilder));

        if (resolver == null)
            Thrower.ArgumentNull(nameof(resolver));

        if (configurationBuilder == null)
            Thrower.ArgumentNull(nameof(configurationBuilder));

        if (serviceProviderFactory == null)
            Thrower.ArgumentNull(nameof(serviceProviderFactory));

        _compilerFactory = compilerFactory;
        _buildPlanBuilder = buildPlanBuilder;
        _resolver = resolver;
        _configurationBuilder = configurationBuilder;
        _serviceProviderFactory = serviceProviderFactory;
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

        var compiled = CompileSourceText(sourceText);
        var buildPlan = _buildPlanBuilder.Build(compiled);
        var semanticErrors = buildPlan.ValidationResult.Diagnostics.Where(x => x.Severity == DialectDiagnosticSeverity.Error).ToList();

        if (!buildPlan.CanBuild)
            return new DialectFrameworkCompositionResult(sourceName, compiled, buildPlan, semanticErrors, []);

        var selectedRuntimePlan = _resolver.Resolve(buildPlan);
        var resolutionErrors = selectedRuntimePlan.Diagnostics
            .Where(x => x.Severity == DialectDiagnosticSeverity.Error)
            .Where(x => !semanticErrors.Contains(x))
            .ToList();

        return new DialectFrameworkCompositionResult(sourceName, compiled, buildPlan, semanticErrors, resolutionErrors, selectedRuntimePlan);
    }


    private DialectDefinitionSlice CompileSourceText(string sourceText)
    {
        if (sourceText == null)
            Thrower.ArgumentNull(nameof(sourceText));

        using var compiler = _compilerFactory.Create();
        return compiler.Compile(sourceText);
    }

    public WistDialectExecutionHost CreateHost(DialectFrameworkCompositionResult compositionResult)
    {
        if (compositionResult == null)
            Thrower.ArgumentNull(nameof(compositionResult));

        if (!compositionResult.IsSuccess || compositionResult.BuildPlan == null)
            Thrower.Argument(nameof(compositionResult), "Dialect composition result must be successful before a runtime host can be created.");

        if (compositionResult.RuntimeSelection is not SelectedRuntimePlan)
            Thrower.Argument(
                nameof(compositionResult),
                "Dialect composition result does not contain a selected runtime plan for Wist execution.");

        var selectedRuntimePlan = (SelectedRuntimePlan)compositionResult.RuntimeSelection;
        var configuration = _configurationBuilder.Build(compositionResult.BuildPlan, selectedRuntimePlan);
        var provider = _serviceProviderFactory.Create(configuration);
        return new WistDialectExecutionHost(provider, configuration);
    }
}