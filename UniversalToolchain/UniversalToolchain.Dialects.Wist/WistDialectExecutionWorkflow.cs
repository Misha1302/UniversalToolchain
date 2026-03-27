using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed class WistDialectExecutionWorkflow
{
    private readonly IDialectCompiledDialectBuildPlanBuilder _buildPlanBuilder;
    private readonly DialectDslCompiler _compiler;
    private readonly WistDialectExecutionConfigurationBuilder _configurationBuilder;
    private readonly DialectFrameworkCompositionWorkflow? _legacyCompositionWorkflow;
    private readonly DialectRuntimeDescriptorRegistry? _legacyRegistry;
    private readonly SelectedRuntimePlanResolver _resolver;
    private readonly WistDialectServiceProviderFactory _serviceProviderFactory;

    public WistDialectExecutionWorkflow(
        DialectDslCompiler compiler,
        IDialectCompiledDialectBuildPlanBuilder buildPlanBuilder,
        SelectedRuntimePlanResolver resolver,
        WistDialectExecutionConfigurationBuilder configurationBuilder,
        WistDialectServiceProviderFactory serviceProviderFactory,
        DialectFrameworkCompositionWorkflow? legacyCompositionWorkflow = null,
        DialectRuntimeDescriptorRegistry? legacyRegistry = null)
    {
        _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
        _buildPlanBuilder = buildPlanBuilder ?? throw new ArgumentNullException(nameof(buildPlanBuilder));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _configurationBuilder = configurationBuilder ?? throw new ArgumentNullException(nameof(configurationBuilder));
        _serviceProviderFactory = serviceProviderFactory ?? throw new ArgumentNullException(nameof(serviceProviderFactory));
        _legacyCompositionWorkflow = legacyCompositionWorkflow;
        _legacyRegistry = legacyRegistry;
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

        if (_legacyCompositionWorkflow != null && _legacyRegistry != null)
        {
            var legacy = _legacyCompositionWorkflow.ComposeText(sourceText, _legacyRegistry, sourceName);
            if (legacy.BuildPlan == null || !legacy.BuildPlan.CanBuild)
                return legacy;

            var selection = _resolver.Resolve(legacy.BuildPlan);
            return new DialectFrameworkCompositionResult(
                legacy.SourceName,
                legacy.CompiledDialect,
                legacy.BuildPlan,
                legacy.RuntimeComposition,
                legacy.SemanticDiagnostics,
                legacy.ResolutionDiagnostics,
                selection);
        }

        var compiled = _compiler.Compile(sourceText);
        var buildPlan = _buildPlanBuilder.Build(compiled);
        var semanticErrors = buildPlan.ValidationResult.Diagnostics.Where(x => x.Severity == DialectDiagnosticSeverity.Error).ToList();

        if (!buildPlan.CanBuild)
            return new DialectFrameworkCompositionResult(sourceName, compiled, buildPlan, null, semanticErrors, [], null);

        var selectedRuntimePlan = _resolver.Resolve(buildPlan);
        var resolutionErrors = selectedRuntimePlan.Diagnostics
            .Where(x => x.Severity == DialectDiagnosticSeverity.Error)
            .Where(x => !semanticErrors.Contains(x))
            .ToList();

        return new DialectFrameworkCompositionResult(sourceName, compiled, buildPlan, null, semanticErrors, resolutionErrors, selectedRuntimePlan);
    }

    public WistDialectExecutionHost CreateHost(DialectFrameworkCompositionResult compositionResult)
    {
        if (compositionResult == null)
            Thrower.ArgumentNull(nameof(compositionResult));

        if (!compositionResult.IsSuccess || compositionResult.BuildPlan == null)
            Thrower.Argument(nameof(compositionResult), "Dialect composition result must be successful before a runtime host can be created.");

        if (compositionResult.RuntimeSelection is not SelectedRuntimePlan selectedRuntimePlan)
            throw new ArgumentException("Dialect composition result does not contain a selected runtime plan for Wist execution.", nameof(compositionResult));

        var configuration = _configurationBuilder.Build(compositionResult.BuildPlan, selectedRuntimePlan);
        var provider = _serviceProviderFactory.Create(configuration);
        return new WistDialectExecutionHost(provider, configuration);
    }
}
