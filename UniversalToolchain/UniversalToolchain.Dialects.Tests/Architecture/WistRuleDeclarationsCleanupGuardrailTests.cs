using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Architecture;

public sealed class WistRuleDeclarationsCleanupGuardrailTests
{
    [Test]
    public void TemporaryRuleDeclarationsModule_MustNotBeRuntimeVisible()
    {
        using var provider = CreateProvider();
        var runtimeComponentCatalog = provider.GetRequiredService<IRuntimeComponentCatalog>();
        var aliases = runtimeComponentCatalog
            .GetModulesInDeterministicOrder()
            .SelectMany(static entry => entry.AllAliases)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();

        Assert.That(aliases, Does.Not.Contain("RuleDeclarations"));
    }

    [Test]
    public void ExecutableDialectExamples_MustNotUseRuleDeclarationsModule()
    {
        var root = ResolveRepositoryRoot();
        var examplesRoot = Path.Combine(root, "UniversalToolchain", "Dialects", "examples", "wist");
        var dialectFiles = Directory.GetFiles(examplesRoot, "*.wistdialect", SearchOption.AllDirectories)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();

        var violations = dialectFiles
            .Where(file => File.ReadAllText(file).Contains("RuleDeclarations", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file))
            .ToList();

        Assert.That(violations, Is.Empty, "Executable dialect examples must not use RuleDeclarations module during cleanup state.");
    }

    [Test]
    public void TemporaryRuleDeclarationsModuleFiles_MustNotExist()
    {
        var root = ResolveRepositoryRoot();
        var moduleRoot = Path.Combine(root, "UniversalToolchain", "RuleDeclarationsModule");

        Assert.That(Directory.Exists(moduleRoot), Is.False, "Temporary RuleDeclarationsModule project must be removed from repository runtime surface.");
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root with AGENTS.md was not found.");
    }
}
