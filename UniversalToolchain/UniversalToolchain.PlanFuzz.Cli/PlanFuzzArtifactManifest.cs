namespace UniversalToolchain.PlanFuzz.Cli;

internal static class PlanFuzzArtifactManifest
{
    public const string FileName = "MANIFEST.sha256";

    public static void Write(string rootDirectory)
    {
        rootDirectory = Path.GetFullPath(rootDirectory.ArgNotNull());
        var manifestPath = Path.Combine(rootDirectory, FileName);
        var lines = Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
            .Where(path => !StringComparer.Ordinal.Equals(Path.GetFullPath(path), manifestPath))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(path => $"{Compute(path)}  ./{Path.GetRelativePath(rootDirectory, path).Replace('\\', '/')}" )
            .ToArray();
        PlanFuzzAtomicFile.WriteAllText(manifestPath, string.Join("\n", lines) + "\n");
    }

    private static string Compute(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
