using ArithmeticModule.Module;
using BasicCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Module;
using ScopesModule.Module;
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
    public void MinimalArithmetic_LoweringActivation_UsesOnlyPlannedLoweringOwners()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.MinimalArithmeticId))
            .GetRequiredPlan();
        using var services = new ServiceCollection().BuildServiceProvider();

        var factories = WistPlannedModulePhaseActivation.CreateOrderedFactories(
            package,
            plan,
            services,
            WistPlannedModulePhase.Lowering);
        var modules = factories.Select(factory => factory()).ToArray();
        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(modules.Select(static module => module.GetType()), Is.EqualTo(new[]
                {
                    typeof(ArithmeticModuleImpl),
                    typeof(NumbersModuleImpl),
                    typeof(ScopesModuleImpl)
                }));
                Assert.That(modules.Any(static module => module.GetType().Name.Contains("Whitespace", StringComparison.Ordinal)), Is.False,
                    "Syntax-only modules must not be materialized by bytecode lowering.");
            });
        }
        finally
        {
            foreach (var disposable in modules.OfType<IDisposable>().Reverse())
                disposable.Dispose();
        }
    }

    [Test]
    public void LoweringActivation_UnknownPlannedPhaseContribution_FailsClosed()
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
        using var services = new ServiceCollection().BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WistPlannedModulePhaseActivation.CreateOrderedFactories(
                wist,
                result.GetRequiredPlan(),
                services,
                WistPlannedModulePhase.Lowering));

        Assert.That(exception!.Message, Does.Contain(externalContribution.Value));
        Assert.That(exception.Message, Does.Contain("no exact phase-owned module implementation"));
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
