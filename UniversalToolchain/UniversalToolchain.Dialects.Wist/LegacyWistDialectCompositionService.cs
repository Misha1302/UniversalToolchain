using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist;

public sealed class LegacyWistDialectCompositionService
{
    private readonly DialectFrameworkCompositionWorkflow _compositionWorkflow;
    private readonly DialectRuntimeDescriptorRegistry _registry;

    public LegacyWistDialectCompositionService(
        DialectFrameworkCompositionWorkflow compositionWorkflow,
        DialectRuntimeDescriptorRegistry registry)
    {
        _compositionWorkflow = compositionWorkflow ?? throw new ArgumentNullException(nameof(compositionWorkflow));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public DialectFrameworkCompositionResult ComposeText(string sourceText, string sourceName = "inline")
    {
        if (sourceText == null)
            Thrower.ArgumentNull(nameof(sourceText));

        if (string.IsNullOrWhiteSpace(sourceName))
            Thrower.Argument(nameof(sourceName), "Source name must not be empty.");

        return _compositionWorkflow.ComposeText(sourceText, _registry, sourceName);
    }
}
