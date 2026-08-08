using System.Text.Json;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistDslPlanParityTests
{
    [Test]
    public void EveryShippedDialect_HasSameSemanticProjectionAsApiPreset()
    {
        var package = new WistLanguageFeaturePackage();
        var registry = new LanguagePackageRegistry().AddPackage(package);
        var compiler = new LanguageCompiler(registry);
        var root = FindRepositoryRoot();
        var failures = new List<string>();

        foreach (var presetId in WistLanguageDefinitions.PresetIds)
        {
            var apiBase = WistLanguageDefinitions.Create(presetId);
            var sourcePath = Path.Combine(root, "UniversalToolchain", "Dialects", "examples", "wist", presetId, "dialect.wistdialect");
            var source = File.ReadAllText(sourcePath);

            foreach (var backend in apiBase.Backends)
            {
                var api = WistFacadeLanguageDefinitionFactory.FromPreset(
                    presetId,
                    backend.Value,
                    WistFacadeSsaPolicy.Disabled);
                var fromFile = WistFacadeLanguageDefinitionFactory.FromDialectText(
                    source,
                    sourcePath,
                    backend.Value,
                    WistFacadeSsaPolicy.Disabled);
                var apiResult = compiler.Compile(api);
                var fileResult = compiler.Compile(fromFile);
                if (!apiResult.IsSuccess || !fileResult.IsSuccess)
                {
                    failures.Add($"{presetId}/{backend.Value}: API={Format(apiResult)} FILE={Format(fileResult)}");
                    continue;
                }

                var apiProjection = SemanticProjection(apiResult.Plan!);
                var fileProjection = SemanticProjection(fileResult.Plan!);
                if (!StringComparer.Ordinal.Equals(apiProjection, fileProjection))
                    failures.Add($"{presetId}/{backend.Value}: semantic projection differs.{Environment.NewLine}API {apiProjection}{Environment.NewLine}FILE {fileProjection}");
            }
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void TranslatedDialectPlan_IsStableUnderRegistryInsertionOrderPerturbation()
    {
        var root = FindRepositoryRoot();
        var sourcePath = Path.Combine(root, "UniversalToolchain", "Dialects", "examples", "wist", WistLanguageDefinitions.MinimalArithmeticGroupedId, "dialect.wistdialect");
        var definition = WistFacadeLanguageDefinitionFactory.FromDialectText(
            File.ReadAllText(sourcePath),
            sourcePath,
            "interpreter",
            WistFacadeSsaPolicy.Disabled);
        var wist = new WistLanguageFeaturePackage();
        var noise = new NoisePackage();
        var first = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(noise).AddPackage(wist))
            .Compile(definition)
            .GetRequiredPlan();
        var second = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(wist).AddPackage(noise))
            .Compile(definition)
            .GetRequiredPlan();

        Assert.Multiple(() =>
        {
            Assert.That(SemanticProjection(second), Is.EqualTo(SemanticProjection(first)));
            Assert.That(second.PlanHash, Is.EqualTo(first.PlanHash));
            Assert.That(LanguageLockFile.Serialize(second), Is.EqualTo(LanguageLockFile.Serialize(first)));
        });
    }

    private static string SemanticProjection(LanguagePlan plan)
    {
        var projection = new
        {
            features = plan.Features.Select(static item => item.Feature.Id.Value).OrderBy(static value => value, StringComparer.Ordinal).ToArray(),
            contributions = plan.Contributions.Select(static item => item.Contribution.Id.Value).ToArray(),
            routes = plan.Routes.Values
                .OrderBy(static route => route.Backend.Value, StringComparer.Ordinal)
                .Select(static route => new
                {
                    backend = route.Backend.Value,
                    source = route.SourceContract.ToString(),
                    target = route.TargetContract.ToString(),
                    steps = route.Steps.Select(static step => step.ContributionId.Value).ToArray()
                })
                .ToArray(),
            requireDeterminism = plan.Definition.RuntimePolicy.RequireDeterminism,
            allowHostInterop = plan.Definition.RuntimePolicy.AllowHostInterop,
            capabilities = plan.Contributions
                .SelectMany(static item => item.Contribution.ProvidesCapabilities)
                .Select(static capability => capability.Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static value => value, StringComparer.Ordinal)
                .ToArray(),
            intrinsicPolicy = plan.Definition.IntrinsicPolicy
                .Select(static directive => $"{directive.Backend?.Value ?? "*"}:{directive.Intrinsic.Value}:{directive.Allowed}")
                .ToArray()
        };
        return JsonSerializer.Serialize(projection);
    }

    private static string Format(LanguageBuildResult result) =>
        result.IsSuccess
            ? "success"
            : string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "UniversalToolchain", "Dialects", "examples", "wist")))
                return directory.FullName;
            directory = directory.Parent;
        }

        Assert.Fail("Repository root was not found from the test directory.");
        return string.Empty;
    }

    private sealed class NoisePackage : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = new(
            new LanguagePackageId("Noise.Package"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(new LanguageFeatureId("noise.feature"))]);
    }
}
