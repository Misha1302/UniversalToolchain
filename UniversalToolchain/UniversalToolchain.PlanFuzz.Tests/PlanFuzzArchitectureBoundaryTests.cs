namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class PlanFuzzArchitectureBoundaryTests
{
    [Test]
    public void GenericCoreHasNoLanguageAdapterOrWistDependency()
    {
        var root = FindRepositoryRoot();
        var coreDirectory = Path.Combine(root, "UniversalToolchain", "UniversalToolchain.PlanFuzz.Core");
        var project = File.ReadAllText(Path.Combine(coreDirectory, "UniversalToolchain.PlanFuzz.Core.csproj"));
        var source = string.Join(
            "\n",
            Directory.EnumerateFiles(coreDirectory, "*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(static path => path, StringComparer.Ordinal)
                .Select(File.ReadAllText));

        Assert.Multiple(() =>
        {
            Assert.That(project, Does.Not.Contain("PlanFuzz.Adapter"));
            Assert.That(project, Does.Not.Contain("UniversalToolchain.Wist"));
            Assert.That(source, Does.Not.Contain("WistPlanFuzz"));
            Assert.That(source, Does.Not.Contain("core.external.load.i32"));
            Assert.That(source, Does.Not.Contain("ssa.operation.descriptor.missing"));
        });
    }

    [Test]
    public void WistAdapterRemainsNonPackable()
    {
        var root = FindRepositoryRoot();
        var project = File.ReadAllText(Path.Combine(
            root,
            "UniversalToolchain",
            "UniversalToolchain.PlanFuzz.Adapter.Wist",
            "UniversalToolchain.PlanFuzz.Adapter.Wist.csproj"));

        Assert.That(project, Does.Contain("<IsPackable>false</IsPackable>"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "build.sh")) &&
                Directory.Exists(Path.Combine(directory.FullName, "UniversalToolchain")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
