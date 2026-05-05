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
                Assert.That(source, Does.Not.Contain(token), $"File '{file}' must use canonical manifest-backed services instead of '{token}'.");
        }
    }

    [Test]
    public void DialectTestHostInfrastructure_CreateBackendSpecificHosts_UsesStructuredBackendOverride()
    {
        const string dialectText = """
                                   dialect StructuredOverride

                                   use Arithmetic,Numbers

                                   backend interpreter,compiler
                                   """;

        using var interpreterHost = DialectTestHostInfrastructure.CreateInterpreterHost(dialectText);
        using var compilerHost = DialectTestHostInfrastructure.CreateCompilerHost(dialectText);

        Assert.Multiple(() =>
        {
            Assert.That(
                interpreterHost.Configuration.BackendConfigurations.Select(static x => x.BackendDescriptor.CanonicalId),
                Is.EqualTo(new[] { "interpreter" }));
            Assert.That(
                compilerHost.Configuration.BackendConfigurations.Select(static x => x.BackendDescriptor.CanonicalId),
                Is.EqualTo(new[] { "cil" }));
            Assert.That(interpreterHost.Configuration.FrontendModules.Select(static x => x.Name), Is.EquivalentTo(compilerHost.Configuration.FrontendModules.Select(static x => x.Name)));
            Assert.That(interpreterHost.Configuration.IrModules.Select(static x => x.Name), Is.EquivalentTo(compilerHost.Configuration.IrModules.Select(static x => x.Name)));
        });
    }

    [Test]
    public void BackendParityInfrastructure_RunBoth_UsesCanonicalCompositionBeforeStructuredOverride()
    {
        const string dialectText = """
                                   dialect Parity
                                   use Arithmetic,Numbers,CSharpInterop,Identifier,Scopes,Whitespaces
                                   backend interpreter,compiler
                                   """;

        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(
            dialectText,
            "Main.Round((10 * 2) * 3.141592653589793)");

        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        Assert.Multiple(() =>
        {
            Assert.That(compilerResult.IsSuccess, Is.True);
            Assert.That(interpreterResult.IsSuccess, Is.True);
            Assert.That(BackendParityInfrastructure.AsNumber(compilerResult.Value), Is.EqualTo(63d));
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