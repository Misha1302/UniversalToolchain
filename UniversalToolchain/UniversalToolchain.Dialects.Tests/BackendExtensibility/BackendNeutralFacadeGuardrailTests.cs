namespace UniversalToolchain.Dialects.Tests.BackendExtensibility;

public sealed class BackendNeutralFacadeGuardrailTests
{
    [Test]
    public void WistRuntimeFacade_ShouldNotReferenceBuiltInBackendArtifactInternals()
    {
        var source = File.ReadAllText(FindRepositoryFile(
            "UniversalToolchain",
            "UniversalToolchain.Dialects.Wist",
            "Facade",
            "WistRuntimeFacade.cs"));

        var forbiddenTokens = new[]
        {
            "DynamicMethod",
            "IAbstractIR",
            "GetArtifactCompiler<",
            "WistDialectBackendIds.Interpreter",
            "WistDialectBackendIds.Cil",
            "System.Reflection.Emit",
            "IntermediateRepresentationAbstractions"
        };

        foreach (var token in forbiddenTokens)
            Assert.That(source, Does.Not.Contain(token));
    }

    [Test]
    public void WistCli_ShouldNotContainRawDialectTextMutationBuilder()
    {
        Assert.That(
            File.Exists(FindRepositoryFileIfPresent("UniversalToolchain", "Wistc", "WistCliCustomizedDialectBuilder.cs")),
            Is.False);
    }

    private static string FindRepositoryFile(params string[] relativeParts)
    {
        var path = FindRepositoryFileIfPresent(relativeParts);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException("Could not locate repository file.", Path.Combine(relativeParts));

        return path;
    }

    private static string FindRepositoryFileIfPresent(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return Path.Combine(relativeParts);
    }
}