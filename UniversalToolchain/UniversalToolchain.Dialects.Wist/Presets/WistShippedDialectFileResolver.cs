using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist.Presets;

/// <summary>
///     Resolves shipped Wist dialect preset descriptors to files copied beside the application.
/// </summary>
public sealed class WistShippedDialectFileResolver
{
    /// <summary>
    ///     Resolves a shipped Wist dialect preset to an absolute dialect file path.
    /// </summary>
    public string Resolve(WistShippedDialectPreset preset)
    {
        preset = preset.ArgNotNull();

        if (string.IsNullOrWhiteSpace(preset.RelativeDialectFilePath))
            Thrower.Argument(nameof(preset), "Preset dialect file path must not be empty.");

        if (Path.IsPathRooted(preset.RelativeDialectFilePath))
            Thrower.Argument(nameof(preset), "Preset dialect file path must be relative.");

        var filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, preset.RelativeDialectFilePath));
        if (!File.Exists(filePath))
            Thrower.FileNotFound(filePath, $"Shipped Wist dialect file was not found: '{filePath}'.");

        return filePath;
    }
}
