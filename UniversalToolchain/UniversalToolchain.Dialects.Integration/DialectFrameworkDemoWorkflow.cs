using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

/// <summary>
/// Executes a minimal framework-native dialect demo and returns deterministic output data.
/// </summary>
public sealed class DialectFrameworkDemoWorkflow
{
    private readonly DialectFrameworkCompositionWorkflow _compositionWorkflow;

    public DialectFrameworkDemoWorkflow(DialectFrameworkCompositionWorkflow compositionWorkflow)
    {
        if (compositionWorkflow == null)
        {
            Thrower.ArgumentNull(nameof(compositionWorkflow));
        }

        _compositionWorkflow = compositionWorkflow;
    }

    public DialectFrameworkDemoReport RunSource(string sourceText, DialectRuntimeDescriptorRegistry registry, string sourceName = "inline")
    {
        if (sourceText == null)
        {
            Thrower.ArgumentNull(nameof(sourceText));
        }

        if (registry == null)
        {
            Thrower.ArgumentNull(nameof(registry));
        }

        try
        {
            var result = _compositionWorkflow.ComposeText(sourceText, registry, sourceName);
            return new DialectFrameworkDemoReport(sourceName, result, null);
        }
        catch (Exception ex)
        {
            return new DialectFrameworkDemoReport(sourceName, null, ex.Message);
        }
    }

    public DialectFrameworkDemoReport RunScenario(DialectFrameworkDemoScenario scenario, DialectRuntimeDescriptorRegistry registry)
    {
        var source = DialectFrameworkDemoSources.GetSource(scenario);
        return RunSource(source, registry, scenario.ToString());
    }
}
