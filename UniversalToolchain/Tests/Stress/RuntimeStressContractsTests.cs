using Tests.Infrastructure;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace Tests.Stress;

[TestFixture]
public class RuntimeStressContractsTests
{
    private const int RepeatCount = 100;
    private const int ParallelCount = 50;

    [Test]
    public void PlanAndRuntime_ShouldSurvive100RepeatedCycles()
    {
        const string dialect = "dialect Repeat\nuse Arithmetic,Numbers,Variables\nbackend cil,interpreter\nsecurity restricted";
        var signatures = new List<string>(RepeatCount);

        for (var i = 0; i < RepeatCount; i++)
        {
            var (package, plan) = Compile(dialect, $"repeat-{i}");
            using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
            signatures.Add(BuildPlanSignature(plan));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1), FormatSignatureGroups(signatures));
    }

    [Test]
    public async Task PlanAndRuntime_ShouldSurvive50ParallelCycles()
    {
        const string dialect = "dialect Parallel\nuse Arithmetic,Numbers\nbackend cil,interpreter\nsecurity restricted";

        var signatures = await Task.WhenAll(Enumerable.Range(0, ParallelCount).Select(i => Task.Run(() =>
        {
            var (package, plan) = Compile(dialect, $"parallel-{i}");
            using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
            return BuildPlanSignature(plan);
        })));

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
    public void KnownBackendResolution_ShouldRemainStable_After100Repeats()
    {
        const string dialect = "dialect Backends\nuse Arithmetic,Numbers\nbackend cil,interpreter\nsecurity restricted";
        var signatures = new List<string>(RepeatCount);

        for (var i = 0; i < RepeatCount; i++)
        {
            var (_, plan) = Compile(dialect, $"backends-{i}");
            signatures.Add(string.Join(
                "|",
                plan.Definition.Backends.Select(static backend => backend.Value).OrderBy(static value => value, StringComparer.Ordinal)));
        }

        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(1), FormatSignatureGroups(signatures));
        Assert.That(signatures[0], Is.EqualTo("cil|interpreter"));
    }

    [Test]
    public async Task CanonicalWistRuntimeFlow_ShouldRemainStable_UnderMixedLoad()
    {
        var tasks = Enumerable.Range(0, ParallelCount).Select(i => Task.Run(() =>
        {
            var dialectText = i % 2 == 0
                ? "dialect M1\nuse Arithmetic,Numbers\nbackend cil,interpreter\nsecurity restricted"
                : "dialect M2\nuse Arithmetic,Identifier,Numbers,Scopes,Variables\nbackend cil,interpreter\nsecurity restricted";
            var (package, plan) = Compile(dialectText, $"mixed-{i}");
            using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });
            var backend = new BackendId(i % 2 == 0 ? "interpreter" : "cil");
            var value = runtime.Run(new LanguageExecutionRequest("1+2", backend)).Value;
            var normalized = WistRuntimeValueAdapterActivation.Normalize(plan, value);
            return BuildPlanSignature(plan) + "##" + (normalized?.ToString() ?? "<null>");
        }));

        var signatures = await Task.WhenAll(tasks);
        Assert.That(signatures.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(2), FormatSignatureGroups(signatures));
        Assert.That(signatures.All(static signature => signature.EndsWith("##3", StringComparison.Ordinal) || signature.EndsWith("##3.0", StringComparison.Ordinal)), Is.True);
    }

    private static (WistLanguageFeaturePackage Package, LanguagePlan Plan) Compile(string source, string sourceName)
    {
        var package = new WistLanguageFeaturePackage();
        var definition = WistFacadeLanguageDefinitionFactory.FromDialectText(
            source,
            sourceName,
            "interpreter",
            WistFacadeSsaPolicy.Disabled);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
        return (package, plan);
    }

    private static string BuildPlanSignature(LanguagePlan plan)
    {
        var contributions = string.Join(
            "|",
            plan.Contributions.Select(static contribution => contribution.Contribution.Id.Value));
        var backends = string.Join(
            "|",
            plan.Definition.Backends.Select(static backend => backend.Value).OrderBy(static value => value, StringComparer.Ordinal));
        var routes = string.Join(
            "|",
            plan.Routes.OrderBy(static route => route.Key.Value, StringComparer.Ordinal)
                .Select(static route => $"{route.Key.Value}:{route.Value.TargetContract}"));
        return $"{plan.PlanHash}::{contributions}::{backends}::{routes}";
    }

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
