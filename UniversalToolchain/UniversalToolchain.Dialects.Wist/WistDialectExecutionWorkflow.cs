using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Wist adapter over the language-neutral composition workflow and Wist runtime activation.
/// </summary>
public sealed class WistDialectExecutionWorkflow
{
    private readonly WistDialectExecutionConfigurationBuilder _configurationBuilder;
    private readonly WistDialectServiceProviderFactory _serviceProviderFactory;
    private readonly ToolchainCompositionWorkflow _workflow;

    public WistDialectExecutionWorkflow(
        ToolchainCompositionWorkflow workflow,
        WistDialectExecutionConfigurationBuilder configurationBuilder,
        WistDialectServiceProviderFactory serviceProviderFactory)
    {
        _workflow = workflow.ArgNotNull();
        _configurationBuilder = configurationBuilder.ArgNotNull();
        _serviceProviderFactory = serviceProviderFactory.ArgNotNull();
    }

    public DialectFrameworkCompositionResult ComposeFile(string filePath) => _workflow.ComposeFile(filePath);

    public DialectFrameworkCompositionResult ComposeText(string sourceText, string sourceName) =>
        _workflow.ComposeText(sourceText, sourceName);

    public DialectFrameworkCompositionResult ComposeText(
        string sourceText,
        string sourceName,
        RuntimeProfileDefinition runtimeProfile,
        RuntimeProfileOverridePolicy overridePolicy = RuntimeProfileOverridePolicy.ExplicitSourceWins)
    {
        sourceText = sourceText.ArgNotNull();
        runtimeProfile = runtimeProfile.ArgNotNull();

        var baseline = _workflow.ComposeText(sourceText, sourceName);
        var compiledSource = baseline.CompiledDialect ?? throw new InvalidOperationException("Dialect source did not compile.");
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
                baseline.BuildPlan!,
                profiledSource.Diagnostics.Where(static x => x.Severity == DialectDiagnosticSeverity.Error),
                []);
        }

        return _workflow.ComposeText(profiledSource.SourceText, sourceName);
    }

    public WistDialectExecutionHost CreateHost(DialectFrameworkCompositionResult compositionResult) =>
        CreateHost(compositionResult, new WistRuntimeServiceOptions());

    public WistDialectExecutionHost CreateHost(
        DialectFrameworkCompositionResult compositionResult,
        WistRuntimeServiceOptions runtimeServiceOptions)
    {
        var configuration = GetConfiguration(compositionResult);
        return new WistDialectExecutionHost(CreateRuntimeHost(configuration, runtimeServiceOptions), configuration);
    }

    public ToolchainRuntimeHost CreateRuntimeHost(DialectFrameworkCompositionResult compositionResult) =>
        CreateRuntimeHost(compositionResult, new WistRuntimeServiceOptions());

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
        return new ToolchainRuntimeHost(provider, configuration, ServiceProviderOwnership.Owned);
    }

    private WistDialectExecutionConfiguration GetConfiguration(DialectFrameworkCompositionResult compositionResult)
    {
        compositionResult = compositionResult.ArgNotNull();
        if (!compositionResult.IsSuccess || compositionResult.BuildPlan == null)
            Thrower.Argument(nameof(compositionResult), "Dialect composition result must be successful before a runtime host can be created.");
        if (compositionResult.RuntimeSelection is not SelectedRuntimePlan)
            Thrower.Argument(nameof(compositionResult), "Dialect composition result does not contain a selected runtime plan for Wist execution.");
        var selectedRuntimePlan = (SelectedRuntimePlan)compositionResult.RuntimeSelection;
        return _configurationBuilder.Build(compositionResult.BuildPlan, selectedRuntimePlan);
    }
}
