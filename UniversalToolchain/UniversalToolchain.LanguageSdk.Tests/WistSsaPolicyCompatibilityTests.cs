using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistSsaPolicyCompatibilityTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public void MissingTypedPolicy_WithoutSsaPass_HasCanonicalOffMeaning()
    {
        var definition = LanguageDefinitionBuilder
            .Create("wist.ssa.compatibility.off", WistLanguageFeaturePackage.PackageVersion.Value)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .UseFeature(WistFeatureIds.Arithmetic)
            .EnableBackend(Interpreter)
            .Build();
        var plan = CreateCompiler().Compile(definition).GetRequiredPlan();

        Assert.Multiple(() =>
        {
            Assert.That(WistSsaPlanPolicy.CreateRuntimeOptions(plan).Policy.ToString(), Is.EqualTo("Off"));
            Assert.That(plan.Contributions.Any(static contribution =>
                contribution.Contribution.Id == WistContributionIds.SsaOptimizer), Is.False);
        });
    }

    [Test]
    public void SsaPass_WithoutTypedPolicy_FailsClosedInsteadOfChoosingRuntimeFallback()
    {
        var definition = LanguageDefinitionBuilder
            .Create("wist.ssa.compatibility.missing-policy", WistLanguageFeaturePackage.PackageVersion.Value)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .UseFeature(WistFeatureIds.Identifiers)
            .UseFeature(WistFeatureIds.NativeTypes)
            .UseFeature(WistFeatureIds.Scopes)
            .UseFeature(WistFeatureIds.Variables)
            .UseFeature(WistFeatureIds.Whitespaces)
            .UseFeature(WistFeatureIds.SsaOptimization)
            .EnableBackend(Interpreter)
            .Build();
        var plan = CreateCompiler().Compile(definition).GetRequiredPlan();

        var error = Assert.Throws<InvalidOperationException>(() => WistSsaPlanPolicy.CreateRuntimeOptions(plan));

        Assert.That(error!.Message, Does.Contain("explicit typed SSA policy"));
    }

    private static LanguageCompiler CreateCompiler() =>
        new(new UniversalToolchain.FeatureSdk.LanguagePackageRegistry()
            .AddPackage(new WistLanguageFeaturePackage()));
}
