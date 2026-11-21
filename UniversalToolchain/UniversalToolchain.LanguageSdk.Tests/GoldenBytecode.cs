using System.Text.Json;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// Reads pinned canonical bytecode for the lowering corpus.
/// <para>
/// The file is committed rather than generated so that a change in lowering output has to be an
/// explicit, reviewable edit. Regenerating it inside the test would make the assertion a tautology.
/// </para>
/// </summary>
internal static class GoldenBytecode
{
    private const string FileName = "GoldenWistBytecode.json";

    public static string Load(string caseName)
    {
        using var document = Read();
        var cases = document.RootElement.GetProperty("cases");
        if (!cases.TryGetProperty(caseName, out var value))
        {
            throw new InvalidOperationException(
                $"Golden bytecode case '{caseName}' is missing from {FileName}. Known cases: " +
                string.Join(", ", cases.EnumerateObject().Select(static property => property.Name)));
        }

        return value.GetString() ?? throw new InvalidOperationException($"Golden case '{caseName}' is null.");
    }

    private static JsonDocument Read()
    {
        var root = FindRepositoryRoot();
        var file = Path.Combine(
            root,
            "UniversalToolchain",
            "UniversalToolchain.LanguageSdk.Tests",
            FileName);
        Assert.That(File.Exists(file), Is.True, $"golden bytecode file is missing: {file}");
        return JsonDocument.Parse(File.ReadAllText(file));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalToolchain", "Wist.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root with UniversalToolchain/Wist.sln was not found.");
    }
}
