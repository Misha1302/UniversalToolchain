using System.Text.Json;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// Reads the pinned Wist plan hashes.
/// <para>
/// The file is committed rather than generated so that a change in planner output has to be an
/// explicit, reviewable edit. Regenerating it inside the test would make the assertion a tautology.
/// </para>
/// </summary>
internal static class GoldenPlanHashes
{
    private const string FileName = "GoldenWistPlanHashes.json";

    public static IReadOnlyDictionary<string, string> Load()
    {
        using var document = Read();
        var plans = document.RootElement.GetProperty("plans");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in plans.EnumerateObject())
            result.Add(property.Name, property.Value.GetString()!);
        return result;
    }

    public static string LoadManifestHash()
    {
        using var document = Read();
        return document.RootElement.GetProperty("manifest").GetString()!;
    }

    private static JsonDocument Read()
    {
        var path = FindRepositoryRoot();
        var file = Path.Combine(path, "UniversalToolchain", "UniversalToolchain.LanguageSdk.Tests", FileName);
        Assert.That(File.Exists(file), Is.True, $"golden plan hash file is missing: {file}");
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
