using Tests.Infrastructure;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Stress;

[TestFixture]
public class RuntimeStressContractsTests
{
    private const int RepeatCount = 100;
    private const int ParallelCount = 50;

    [Test]
    public void ComposeAndCreateHost_ShouldSurvive100RepeatedCycles()
    {
        using var provider = TestContractsInfrastructure.CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var signatures = new List<string>(RepeatCount);
        for (var i = 0; i < RepeatCount; i++)
        {
            var composition = workflow.ComposeText("dialect Repeat\nuse Arithmetic,Numbers,Variables\n\nbackend cil,interpreter", $"repeat-{i}");
            Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));
            using var host = workflow.CreateHost(composition);
            signatures.Add(TestContractsInfrastructure.BuildSelectionSignature(composition) + "##" + TestContractsInfrastructure.BuildHostSignature(host));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1), FormatSignatureGroups(signatures));
    }

    [Test]
    public async Task ComposeAndCreateHost_ShouldSurvive50ParallelCycles()
    {
        using var provider = TestContractsInfrastructure.CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var signatures = await Task.WhenAll(Enumerable.Range(0, ParallelCount).Select(i => Task.Run(() =>
        {
            var composition = workflow.ComposeText("dialect Parallel\nuse Arithmetic,Numbers\nbackend cil,interpreter", $"parallel-{i}");
            if (!composition.IsSuccess)
                return "compose-failed:" + FormatComposition(composition);

            using var host = workflow.CreateHost(composition);
            return TestContractsInfrastructure.BuildSelectionSignature(composition) + "##" + TestContractsInfrastructure.BuildHostSignature(host);
        })));

        Assert.That(signatures.All(static x => !x.StartsWith("compose-failed:", StringComparison.Ordinal)), Is.True, string.Join(Environment.NewLine, signatures));
        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1), FormatSignatureGroups(signatures));
    }

    [Test]
    public void ManifestCatalogLoading_ShouldRemainStable_After100Repeats()
    {
        using var temp = new TempDirectory();
        var first = TestContractsInfrastructure.WriteManifest(temp.Path, "a.dialect.runtime.json", "A.Assembly", [new FileDialectRuntimeComponentEntry("FrontendModule", "Arithmetic", ["arith"], "frontend.arithmetic", new FileRuntimeComponentActivationEntry(new RuntimeTypeReference("A.Assembly", "ArithmeticModule.Module.ArithmeticModuleImpl")))]);
        var second = TestContractsInfrastructure.WriteManifest(temp.Path, "b.dialect.runtime.json", "B.Assembly", [new FileDialectRuntimeComponentEntry("Backend", "interpreter", ["vm"], "backend.interpreter", new FileRuntimeComponentActivationEntry(new RuntimeTypeReference("B.Assembly", "BasicInterpreter.Implementations.BasicInterpreter")))]);
        var serializer = new RuntimeManifestJsonSerializer();

        var signatures = new List<string>(RepeatCount);
        for (var i = 0; i < RepeatCount; i++)
        {
            var catalog = new FileBasedRuntimeComponentCatalog(new StaticManifestLocator([second, first]), serializer);
            var modules = catalog.GetModulesInDeterministicOrder().Select(static x => x.CanonicalAlias).ToArray();
            var backends = catalog.GetBackendsInDeterministicOrder().Select(static x => x.CanonicalAlias).ToArray();
            signatures.Add(string.Join("|", modules) + "::" + string.Join("|", backends));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1), FormatSignatureGroups(signatures));
    }

    [Test]
    public void RuntimeTypeLoading_ShouldRemainStable_After100Repeats()
    {
        using var provider = TestContractsInfrastructure.CreateWorkflowProvider();
        var loader = provider.GetRequiredService<IRuntimeComponentTypeLoader>();
        var catalog = provider.GetRequiredService<IRuntimeComponentCatalog>();

        var entries = catalog.GetModulesInDeterministicOrder().Take(3)
            .Concat(catalog.GetBackendsInDeterministicOrder())
            .ToArray();

        var signatures = new List<string>(RepeatCount);
        for (var i = 0; i < RepeatCount; i++)
            signatures.Add(string.Join("|", entries.Select(loader.LoadType).Select(static x => x.FullName)));

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1), FormatSignatureGroups(signatures));
    }

    [Test]
    public void KnownBackendResolution_ShouldRemainStable_After100Repeats()
    {
        using var provider = TestContractsInfrastructure.CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect Backends\nuse Arithmetic\nbackend cil,interpreter", "backends");
        Assert.That(composition.IsSuccess, Is.True, FormatComposition(composition));

        var signatures = new List<string>(RepeatCount);
        for (var i = 0; i < RepeatCount; i++)
        {
            using var host = workflow.CreateHost(composition);
            signatures.Add(string.Join("|", host.Configuration.EnabledBackends.Select(static x => x.CanonicalId)));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1), FormatSignatureGroups(signatures));
    }

    [Test]
    public async Task CanonicalWistRuntimeFlow_ShouldRemainStable_UnderMixedLoad()
    {
        using var provider = TestContractsInfrastructure.CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var tasks = Enumerable.Range(0, ParallelCount).Select(i => Task.Run(() =>
        {
            var dialectText = i % 2 == 0
                ? "dialect M1\nuse Arithmetic,Numbers\nbackend cil,interpreter"
                : "dialect M2\nuse Arithmetic,Identifier,Numbers,Scopes,Variables\n\nbackend cil,interpreter";

            var composition = workflow.ComposeText(dialectText, $"mixed-{i}");
            if (!composition.IsSuccess)
                return "compose-failed:" + FormatComposition(composition);

            using var host = workflow.CreateHost(composition);
            var runResult = host.Run("1+2", i % 2 == 0 ? "interpreter" : "cil");
            return TestContractsInfrastructure.BuildSelectionSignature(composition) + "##" + TestContractsInfrastructure.BuildHostSignature(host) + "##" + (runResult?.ToString() ?? "<null>");
        }));

        var signatures = await Task.WhenAll(tasks);
        Assert.That(signatures.All(static x => !x.StartsWith("compose-failed:", StringComparison.Ordinal)), Is.True, string.Join(Environment.NewLine, signatures));
        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(2), FormatSignatureGroups(signatures));
    }

    private static string FormatComposition(DialectFrameworkCompositionResult composition) => DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition));

    private static string FormatSignatureGroups(IEnumerable<string> signatures)
    {
        return string.Join(
            Environment.NewLine,
            signatures
                .GroupBy(static x => x, StringComparer.Ordinal)
                .OrderByDescending(static x => x.Count())
                .ThenBy(static x => x.Key, StringComparer.Ordinal)
                .Select(static x => $"{x.Count()}x {x.Key}"));
    }
}
