using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistPhaseOwnershipActivationTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public void MinimalArithmetic_LoweringValidation_UsesOnlyNativePlannedOwners()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.MinimalArithmeticId))
            .GetRequiredPlan();

        Assert.DoesNotThrow(() => WistPlannedSemanticBindingActivation.ValidatePlannedLowering(package, plan));

        var loweringContributions = plan.Contributions
            .Where(static contribution => contribution.Contribution.Slot == WistModulePhaseSlots.Lowering)
            .ToArray();

        Assert.That(loweringContributions, Is.Not.Empty);
        foreach (var contribution in loweringContributions)
        {
            if (contribution.Contribution.Id == WistContributionIds.CanonicalAddLowering)
                continue;

            Assert.That(
                WistModulePhaseOwnership.TryGetLoweringComponent(contribution.Contribution.Id, out var component),
                Is.True,
                $"Planned lowering contribution '{contribution.Contribution.Id.Value}' has no derived native owner.");
            Assert.That(component, Is.Not.Null);
            Assert.That(
                WistSemanticBytecodeLowerer.SupportsModuleContribution(component!.ContributionId),
                Is.True,
                $"Derived owner '{component.ContributionId.Value}' is not supported by the native semantic lowerer.");
            Assert.That(component.Alias.Contains("Whitespace", StringComparison.Ordinal), Is.False,
                "Syntax-only modules must not be activated as lowering owners.");
        }
    }

    [Test]
    public void MinimalArithmetic_SemanticActivation_ReturnsBindingRulesWithoutFrontendModules()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.MinimalArithmeticId))
            .GetRequiredPlan();
        using var services = new ServiceCollection().BuildServiceProvider();

        var rules = WistPlannedSemanticBindingActivation.CreateOrderedRules(package, plan, services);

        Assert.That(rules, Is.Not.Null);
        Assert.That(
            plan.Contributions
                .Where(static contribution => contribution.Contribution.Slot == WistModulePhaseSlots.Semantics)
                .All(contribution =>
                    contribution.Contribution.Id == WistContributionIds.CanonicalAddSemantics
                    || WistModulePhaseOwnership.TryGetSemanticComponent(contribution.Contribution.Id, out _)),
            Is.True,
            "Every planned semantic contribution must be backed by an exact phase-specific semantic owner.");
    }

    [Test]
    public void LoweringValidation_UnknownPlannedPhaseContribution_FailsClosed()
    {
        var wist = new WistLanguageFeaturePackage();
        var externalFeature = new LanguageFeatureId("acme.wist.phase-lowering");
        var externalContribution = new LanguageContributionId("acme.wist.lowering.unregistered");
        var external = new ExternalPhasePackage(externalFeature, externalContribution);
        var registry = new LanguagePackageRegistry().AddPackage(wist).AddPackage(external);
        var result = new LanguageCompiler(registry).Compile(
            LanguageDefinitionBuilder.Create("Wist.PhaseOwnership.FailClosed", "1")
                .EnableBackend(Interpreter)
                .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
                .UseFeature(WistFeatureIds.Arithmetic)
                .UseFeature(externalFeature)
                .Build());
        Assert.That(result.IsSuccess, Is.True,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WistPlannedSemanticBindingActivation.ValidatePlannedLowering(wist, result.GetRequiredPlan()));

        Assert.That(exception!.Message, Does.Contain(externalContribution.Value));
        Assert.That(exception.Message, Does.Contain("exact Wist package"));
    }

    private sealed class ExternalPhasePackage : ILanguageFeaturePackage
    {
        public ExternalPhasePackage(LanguageFeatureId featureId, LanguageContributionId contributionId)
        {
            Descriptor = new LanguagePackageDescriptor(
                new LanguagePackageId("Acme.Wist.PhaseLowering"),
                new LanguageVersion("1"),
                ToolchainApi.Current,
                [new LanguageFeatureDescriptor(featureId, supportedBackends: [Interpreter], contributions: [contributionId])],
                contributions:
                [
                    new LanguageContributionDescriptor(
                        contributionId,
                        WistModulePhaseSlots.Lowering,
                        supportedBackends: [Interpreter],
                        order: 999)
                ]);
        }

        public LanguagePackageDescriptor Descriptor { get; }
    }
}
