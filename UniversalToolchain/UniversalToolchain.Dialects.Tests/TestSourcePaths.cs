namespace UniversalToolchain.Dialects.Tests;

internal static class TestSourcePaths
{
    public static string ToolchainRoot => LocateAncestor(
        static path =>
            File.Exists(Path.Combine(path, "Directory.Build.targets")) &&
            Directory.Exists(Path.Combine(path, "Dialects", "examples", "wist")),
        "UniversalToolchain source root");

    public static string RepositoryRoot => LocateAncestor(
        static path =>
            File.Exists(Path.Combine(path, "UniversalToolchain", "Directory.Build.targets")) &&
            Directory.Exists(Path.Combine(path, "UniversalToolchain", "Dialects", "examples", "wist")),
        "repository source root");

    public static string WistExamplesRoot => Path.Combine(ToolchainRoot, "Dialects", "examples", "wist");

    public static string WistExampleDirectory(string name)
    {
        var path = Path.Combine(WistExamplesRoot, name);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Wist example directory not found: {path}");

        return path;
    }

    public static string WistExampleDialectPath(string name) =>
        Path.Combine(WistExampleDirectory(name), "dialect.wistdialect");

    private static string LocateAncestor(Func<string, bool> predicate, string description)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (predicate(directory.FullName))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate {description} from '{TestContext.CurrentContext.TestDirectory}'.");
    }
}
