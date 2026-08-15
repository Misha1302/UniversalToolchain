using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.LanguageSdk.Generic.Tests;

[TestFixture]
public sealed class GenericLanguageSdkOwnershipTests
{
    private static readonly BackendId Backend = new("generic-test");
    private static readonly LanguageArtifactKind<int> Parsed = new("generic.parsed");

    [Test]
    public void Compiler_ProducesDeterministicPlanWithoutLanguageSpecificPackages()
    {
        var package = CreatePackage();
        var compiler = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package));
        var definition = CreateDefinition();

        var first = compiler.Compile(definition).GetRequiredPlan();
        var second = compiler.Compile(definition).GetRequiredPlan();

        Assert.Multiple(() =>
        {
            Assert.That(first.PlanHash, Is.EqualTo(second.PlanHash));
            Assert.That(LanguageLockFile.Serialize(first), Is.EqualTo(LanguageLockFile.Serialize(second)));
            Assert.That(first.Routes[Backend].Steps.Select(static step => step.ContributionId.Value),
                Is.EqualTo(new[] { "generic.parse" }));
        });
    }

    [Test]
    public void Runtime_MaterializesAndExecutesOnlyThePlannedGenericRoute()
    {
        var package = CreatePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(CreateDefinition())
            .GetRequiredPlan();
        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });

        var result = runtime.Run(new LanguageExecutionRequest("21", Backend));

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(plan.Routes[Backend].Steps.Select(static step => step.ContributionId.Value),
                Is.EqualTo(new[] { "generic.parse" }));
        });
    }

    [Test]
    public void Runtime_FailsClosedWhenExactPlannedTransformerIsMissing()
    {
        var package = CreatePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(CreateDefinition())
            .GetRequiredPlan();
        var incomplete = LanguagePackageBuilder.Create("Generic.Incomplete", "1")
            .AddFeature("generic.core", feature => feature
                .AddBackend(
                    Backend,
                    new LanguageContributionId("generic.backend"),
                    Parsed,
                    static (value, _) => value * 2,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("generic.runtime", "1")
            .Build();

        var error = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { incomplete }));

        Assert.That(error!.Message, Does.Contain("generic.parse"));
    }

    private static AuthoredLanguagePackage CreatePackage() =>
        LanguagePackageBuilder.Create("Generic.Language", "1")
            .AddFeature("generic.core", feature => feature
                .AddTransformer(
                    "generic.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    Parsed,
                    static (source, _) => int.Parse(source),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1)
                .AddBackend(
                    Backend,
                    new LanguageContributionId("generic.backend"),
                    Parsed,
                    static (value, _) => value * 2,
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("generic.runtime", "1")
            .Build();

    private static LanguageDefinition CreateDefinition() =>
        LanguageDefinitionBuilder.Create("Generic.Language", "1")
            .UseFeature("generic.core")
            .EnableBackend(Backend)
            .Build();
}
