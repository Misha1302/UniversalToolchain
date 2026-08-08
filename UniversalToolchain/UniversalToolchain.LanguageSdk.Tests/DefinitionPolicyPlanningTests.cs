using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class DefinitionPolicyPlanningTests
{
    private static readonly LanguageFeatureId Feature = new("policy.feature");
    private static readonly LanguageContributionId A = new("policy.a");
    private static readonly LanguageContributionId B = new("policy.b");
    private static readonly LanguageContributionId C = new("policy.c");

    [Test]
    public void DefinitionOrder_IsResolvedByLanguageCompilerAndPreservedByPlan()
    {
        var compiler = new LanguageCompiler(Registry(A, B));
        var baseline = compiler.Compile(Definition()).GetRequiredPlan();
        var reordered = compiler.Compile(LanguageDefinitionBuilder.Create("Policy.Language", "1")
            .UseFeature(Feature)
            .OrderContributionBefore(B, A)
            .Build()).GetRequiredPlan();

        Assert.Multiple(() =>
        {
            Assert.That(baseline.Contributions.Select(static item => item.Contribution.Id), Is.EqualTo(new[] { A, B }));
            Assert.That(reordered.Contributions.Select(static item => item.Contribution.Id), Is.EqualTo(new[] { B, A }));
            Assert.That(reordered.PlanHash, Is.Not.EqualTo(baseline.PlanHash));
            Assert.That(LanguageLockFile.Serialize(reordered), Does.Contain("contributionOrderConstraints"));
        });
    }

    [Test]
    public void DefinitionOrder_RejectsMissingContributionAndCycle()
    {
        var compiler = new LanguageCompiler(Registry(A, B));
        var missing = compiler.Compile(LanguageDefinitionBuilder.Create("Policy.Language", "1")
            .UseFeature(Feature)
            .OrderContributionBefore(C, A)
            .Build());
        var cycle = compiler.Compile(LanguageDefinitionBuilder.Create("Policy.Language", "1")
            .UseFeature(Feature)
            .OrderContributionBefore(A, B)
            .OrderContributionBefore(B, A)
            .Build());

        Assert.Multiple(() =>
        {
            Assert.That(missing.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("UTL2110"));
            Assert.That(cycle.Diagnostics.Select(static diagnostic => diagnostic.Code), Does.Contain("UTL2112"));
        });
    }

    [Test]
    public void DefinitionOrder_IsIndependentOfConstraintInsertionOrder()
    {
        var compiler = new LanguageCompiler(Registry(A, B, C));
        var first = compiler.Compile(LanguageDefinitionBuilder.Create("Policy.Language", "1")
            .UseFeature(Feature)
            .OrderContributionBefore(C, B)
            .OrderContributionBefore(B, A)
            .Build()).GetRequiredPlan();
        var second = compiler.Compile(LanguageDefinitionBuilder.Create("Policy.Language", "1")
            .UseFeature(Feature)
            .OrderContributionBefore(B, A)
            .OrderContributionBefore(C, B)
            .Build()).GetRequiredPlan();

        Assert.Multiple(() =>
        {
            Assert.That(first.Contributions.Select(static item => item.Contribution.Id), Is.EqualTo(new[] { C, B, A }));
            Assert.That(second.Contributions.Select(static item => item.Contribution.Id), Is.EqualTo(new[] { C, B, A }));
            Assert.That(second.PlanHash, Is.EqualTo(first.PlanHash));
            Assert.That(LanguageLockFile.Serialize(second), Is.EqualTo(LanguageLockFile.Serialize(first)));
        });
    }

    [Test]
    public void IntrinsicPolicy_IsPartOfPlanIdentityAndLock()
    {
        var compiler = new LanguageCompiler(Registry(A));
        var allowed = compiler.Compile(LanguageDefinitionBuilder.Create("Policy.Language", "1")
            .UseFeature(Feature)
            .ConfigureIntrinsic(new LanguageIntrinsicId("math.sqrt"), true)
            .Build()).GetRequiredPlan();
        var forbidden = compiler.Compile(LanguageDefinitionBuilder.Create("Policy.Language", "1")
            .UseFeature(Feature)
            .ConfigureIntrinsic(new LanguageIntrinsicId("math.sqrt"), false)
            .Build()).GetRequiredPlan();
        var lockFile = LanguageLockFile.Serialize(forbidden);

        Assert.Multiple(() =>
        {
            Assert.That(forbidden.PlanHash, Is.Not.EqualTo(allowed.PlanHash));
            Assert.That(lockFile, Does.Contain("intrinsicPolicy"));
            Assert.That(lockFile, Does.Contain("math.sqrt"));
            Assert.That(lockFile, Does.Contain("\"allowed\": false"));
        });
    }

    private static LanguageDefinition Definition() => LanguageDefinitionBuilder.Create("Policy.Language", "1")
        .UseFeature(Feature)
        .Build();

    private static LanguagePackageRegistry Registry(params LanguageContributionId[] contributions)
    {
        var descriptor = new LanguagePackageDescriptor(
            new LanguagePackageId("Policy.Package"),
            new LanguageVersion("1"),
            ToolchainApi.Current,
            [new LanguageFeatureDescriptor(Feature, contributions: contributions)],
            contributions: contributions.Select(static id => new LanguageContributionDescriptor(id, LanguageSlots.Tooling)));
        return new LanguagePackageRegistry().AddPackage(new Package(descriptor));
    }

    private sealed class Package(LanguagePackageDescriptor descriptor) : ILanguageExtensionPackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;
    }
}
