namespace Tests.Infrastructure;

internal static class TestSourcePaths
{
    public static string RepositoryRoot => LocateAncestor(
        static path => File.Exists(Path.Combine(path, "UniversalToolchain", "Wistc", "Wistc.csproj")),
        "repository source root");

    public static string ToolchainRoot => Path.Combine(RepositoryRoot, "UniversalToolchain");

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
