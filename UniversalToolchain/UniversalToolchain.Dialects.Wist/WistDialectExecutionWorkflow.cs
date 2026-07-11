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
        compilerFactory = compilerFactory.ArgNotNull();

        buildPlanBuilder = buildPlanBuilder.ArgNotNull();

        resolver = resolver.ArgNotNull();

        configurationBuilder = configurationBuilder.ArgNotNull();

        serviceProviderFactory = serviceProviderFactory.ArgNotNull();

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
        sourceText = sourceText.ArgNotNull();

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

    public DialectFrameworkCompositionResult ComposeText(
        string sourceText,
        string sourceName,
        RuntimeProfileDefinition runtimeProfile,
        RuntimeProfileOverridePolicy overridePolicy = RuntimeProfileOverridePolicy.ExplicitSourceWins)
    {
        sourceText = sourceText.ArgNotNull();
        runtimeProfile = runtimeProfile.ArgNotNull();

        var compiledSource = CompileSourceText(sourceText);
        var profiledSource = new WistRuntimeProfileApplicator().Apply(
            sourceText,
            compiledSource,
            runtimeProfile,
            overridePolicy);
        if (!profiledSource.CanCompose)
        {
            return new DialectFrameworkCompositionResult(
                sourceName,
                compiledSource,
                _buildPlanBuilder.Build(compiledSource),
                profiledSource.Diagnostics.Where(static x => x.Severity == DialectDiagnosticSeverity.Error),
                []);
        }

        return ComposeText(profiledSource.SourceText, sourceName);
    }

    private DialectDefinitionSlice CompileSourceText(string sourceText)
    {
        sourceText = sourceText.ArgNotNull();

        using var compiler = _compilerFactory.Create();
        return compiler.Compile(sourceText);
    }

    public WistDialectExecutionHost CreateHost(DialectFrameworkCompositionResult compositionResult)
        => CreateHost(compositionResult, new WistRuntimeServiceOptions());

    public WistDialectExecutionHost CreateHost(
        DialectFrameworkCompositionResult compositionResult,
        WistRuntimeServiceOptions runtimeServiceOptions)
    {
        var configuration = GetConfiguration(compositionResult);
        return new WistDialectExecutionHost(CreateRuntimeHost(configuration, runtimeServiceOptions), configuration);
    }

    public ToolchainRuntimeHost CreateRuntimeHost(DialectFrameworkCompositionResult compositionResult)
        => CreateRuntimeHost(compositionResult, new WistRuntimeServiceOptions());

    public ToolchainRuntimeHost CreateRuntimeHost(
        DialectFrameworkCompositionResult compositionResult,
        WistRuntimeServiceOptions runtimeServiceOptions)
    {
        var configuration = GetConfiguration(compositionResult);
        return CreateRuntimeHost(configuration, runtimeServiceOptions);
    }

    private ToolchainRuntimeHost CreateRuntimeHost(
        WistDialectExecutionConfiguration configuration,
        WistRuntimeServiceOptions runtimeServiceOptions)
    {
        runtimeServiceOptions = runtimeServiceOptions.ArgNotNull();

        var provider = _serviceProviderFactory.Create(configuration, runtimeServiceOptions);
        return new ToolchainRuntimeHost(provider, configuration);
    }

    private WistDialectExecutionConfiguration GetConfiguration(DialectFrameworkCompositionResult compositionResult)
    {
        compositionResult = compositionResult.ArgNotNull();

        if (!compositionResult.IsSuccess || compositionResult.BuildPlan == null)
            Thrower.Argument(nameof(compositionResult), "Dialect composition result must be successful before a runtime host can be created.");

        if (compositionResult.RuntimeSelection is not SelectedRuntimePlan)
            Thrower.Argument(
                nameof(compositionResult),
                "Dialect composition result does not contain a selected runtime plan for Wist execution.");

        var selectedRuntimePlan = (SelectedRuntimePlan)compositionResult.RuntimeSelection;
        return _configurationBuilder.Build(compositionResult.BuildPlan, selectedRuntimePlan);
    }
}
