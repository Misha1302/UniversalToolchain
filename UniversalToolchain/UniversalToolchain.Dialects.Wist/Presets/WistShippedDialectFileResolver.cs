using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist.Presets;

/// <summary>
///     Resolves shipped Wist dialect preset descriptors to files copied beside the application or NuGet contentFiles.
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

        foreach (var candidate in EnumerateCandidates(preset))
        {
            if (File.Exists(candidate))
                return candidate;
        }

        var searched = string.Join(Environment.NewLine, EnumerateCandidates(preset).Select(static x => " - " + x));
        Thrower.FileNotFound(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, preset.RelativeDialectFilePath)),
            $"Shipped Wist dialect file was not found. Searched:{Environment.NewLine}{searched}");
        return null!;
    }

    private static IEnumerable<string> EnumerateCandidates(WistShippedDialectPreset preset)
    {
        var baseDirectory = AppContext.BaseDirectory;
        yield return Path.GetFullPath(Path.Combine(baseDirectory, preset.RelativeDialectFilePath));

        var relativeParts = preset.RelativeDialectFilePath
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        var contentFileRelativeCandidates = new[] { "net10.0", "net9.0", "net8.0" }
            .Select(tfm => Path.Combine(new[] { "contentFiles", "any", tfm }.Concat(relativeParts).ToArray()))
            .ToArray();

        foreach (var contentFileRelative in contentFileRelativeCandidates)
            yield return Path.GetFullPath(Path.Combine(baseDirectory, contentFileRelative));

        var current = new DirectoryInfo(baseDirectory);
        for (var depth = 0; current != null && depth < 6; depth++, current = current.Parent)
        {
            yield return Path.GetFullPath(Path.Combine(current.FullName, preset.RelativeDialectFilePath));
            foreach (var contentFileRelative in contentFileRelativeCandidates)
                yield return Path.GetFullPath(Path.Combine(current.FullName, contentFileRelative));
        }
    }
}
