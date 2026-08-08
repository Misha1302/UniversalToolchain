using UniversalToolchain.Testing.Infrastructure;

namespace UniversalToolchain.Dialects.Tests;

public sealed class TestingInfrastructureGuardrailTests
{
    [Test]
    public void TestingInfrastructure_ShouldNotRewriteDialectSourceByBackendDirectivePrefix()
    {
        var files = TestingInfrastructureFiles();
        var forbiddenTokens = new[]
        {
            "EnsureSingleBackend",
            "StartsWith(\"backend ",
            "Split(['\\r', '\\n']",
            "lines.Add($\"backend"
        };

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);

            foreach (var token in forbiddenTokens)
                Assert.That(source, Does.Not.Contain(token), $"File '{file}' must not use raw dialect backend rewriting token '{token}'.");
        }
    }

    [Test]
    public void TestingInfrastructure_ShouldNotRegisterBuiltInBackendsManually()
    {
        var files = TestingInfrastructureFiles();
        var forbiddenTokens = new[]
        {
            "AddWistCilBackend",
            "AddWistInterpreterBackend"
        };

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);

            foreach (var token in forbiddenTokens)
                Assert.That(source, Does.Not.Contain(token), $"File '{file}' must use the canonical Wist facade instead of '{token}'.");
        }
    }

    [Test]
    public void DialectTestHostInfrastructure_RunInBothBackends_UsesCanonicalFacadeWithoutSourceRewrite()
    {
        const string dialectText = """
                                   dialect StructuredOverride

                                   use Arithmetic,Numbers

                                   backend interpreter,cil
                                   security restricted
                                   """;

        var result = DialectTestHostInfrastructure.RunInBothBackends(dialectText, "2 + 3");

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(5d));
    }

    [Test]
    public void BackendParityInfrastructure_RunBoth_UsesCanonicalFacadeBackendSelection()
    {
        const string dialectText = """
                                   dialect Parity
                                   use Arithmetic,Numbers,CSharpInterop,Identifier,Scopes,Whitespaces
                                   backend interpreter,cil
                                   security trusted
                                   capability unsafe-interop
                                   """;

        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(
            dialectText,
            "2 + 3");

        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        Assert.Multiple(() =>
        {
            Assert.That(compilerResult.IsSuccess, Is.True);
            Assert.That(interpreterResult.IsSuccess, Is.True);
            Assert.That(BackendParityInfrastructure.AsNumber(compilerResult.Value), Is.EqualTo(5d));
        });
    }

    private static IReadOnlyList<string> TestingInfrastructureFiles() =>
    [
        FindRepositoryFile(
            "UniversalToolchain",
            "UniversalToolchain.Testing.Infrastructure",
            "BackendParityInfrastructure.cs"),
        FindRepositoryFile(
            "UniversalToolchain",
            "UniversalToolchain.Testing.Infrastructure",
            "DialectTestHostInfrastructure.cs")
    ];

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
