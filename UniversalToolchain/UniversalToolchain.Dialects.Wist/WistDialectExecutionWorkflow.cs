using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     End-to-end Wist workflow that composes dialect DSL and materializes a runnable runtime host.
/// </summary>
public sealed class WistDialectExecutionWorkflow
{
    private readonly DialectFrameworkCompositionWorkflow _compositionWorkflow;
    private readonly WistDialectExecutionConfigurationBuilder _configurationBuilder;
    private readonly DialectRuntimeDescriptorRegistry _registry;
    private readonly WistDialectServiceProviderFactory _serviceProviderFactory;

    public WistDialectExecutionWorkflow(
        DialectFrameworkCompositionWorkflow compositionWorkflow,
        DialectRuntimeDescriptorRegistry registry,
        WistDialectExecutionConfigurationBuilder configurationBuilder,
        WistDialectServiceProviderFactory serviceProviderFactory)
    {
        if (compositionWorkflow == null)
            Thrower.ArgumentNull(nameof(compositionWorkflow));

        if (registry == null)
            Thrower.ArgumentNull(nameof(registry));

        if (configurationBuilder == null)
            Thrower.ArgumentNull(nameof(configurationBuilder));

        if (serviceProviderFactory == null)
            Thrower.ArgumentNull(nameof(serviceProviderFactory));

        _compositionWorkflow = compositionWorkflow;
        _registry = registry;
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

        return _compositionWorkflow.ComposeText(sourceText, _registry, sourceName);
    }

    public WistDialectExecutionHost CreateHost(DialectFrameworkCompositionResult compositionResult)
    {
        if (compositionResult == null)
            Thrower.ArgumentNull(nameof(compositionResult));

        if (!compositionResult.IsSuccess || compositionResult.BuildPlan == null || compositionResult.RuntimeComposition == null)
            Thrower.Argument(nameof(compositionResult), "Dialect composition result must be successful before a runtime host can be created.");

        var configuration = _configurationBuilder.Build(compositionResult.BuildPlan, compositionResult.RuntimeComposition, _registry);
        var provider = _serviceProviderFactory.Create(configuration);
        return new WistDialectExecutionHost(provider, configuration);
    }
}