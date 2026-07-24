namespace UniversalToolchain.PlanFuzz.Cli;

internal static class PlanFuzzOutputDirectory
{
    public static string PrepareEmpty(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Thrower.Argument<string>(parameterName, "Output directory must not be empty.");

        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath) && Directory.EnumerateFileSystemEntries(fullPath).Any())
        {
            return Thrower.Argument<string>(
                parameterName,
                $"Output directory '{fullPath}' must be empty to prevent stale evidence from contaminating the result.");
        }

        Directory.CreateDirectory(fullPath);
        return fullPath;
    }
}
