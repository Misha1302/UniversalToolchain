namespace UniversalToolchain.Dialects.Tests.Architecture;

public sealed class WistPublicFacadeSyntaxOwnershipGuardrailTests
{
    [Test]
    public void WistEngine_DoesNotRediscoverLanguageSyntaxFromRawSourceText()
    {
        var source = File.ReadAllText(FindRepositoryFile(
            "UniversalToolchain",
            "UniversalToolchain.Wist",
            "WistEngine.cs"));

        var forbiddenPatterns = new[]
        {
            "ContainsTokenBoundary",
            "IsIdentifierCharacter",
            "Substring(index",
            "Feature 'let' is not enabled"
        };

        Assert.Multiple(() =>
        {
            foreach (var pattern in forbiddenPatterns)
                Assert.That(source, Does.Not.Contain(pattern), $"WistEngine must not parse Wist syntax from raw source text: {pattern}");
        });
    }

    private static string FindRepositoryFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate repository file.", Path.Combine(relativeParts));
    }
}
