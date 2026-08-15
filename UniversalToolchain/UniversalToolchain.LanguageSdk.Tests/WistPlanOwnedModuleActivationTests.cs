using BasicCore.Contracts;
using BasicLexer.Core;
using BasicParser.Core;
using BasicCodeTranslator;
using AbstractIrConverters;
using BasicCore.Registration;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistPlanOwnedModuleActivationTests
{
    private static readonly BackendId Interpreter = new("interpreter");
    private static readonly BackendId Cil = new("cil");

    [Test]
    public void Activation_PreservesLanguagePlanOrder_WhenRegistrationOrderIsReversed()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = Compile(package, Interpreter);
        var builtIn = WistFrontendModuleActivation.CreateBuiltInSource(package);
        var reversed = new WistFrontendModuleSource(package, builtIn.FrontendModules.Reverse());
        using var services = new ServiceCollection().BuildServiceProvider();

        var factories = WistFrontendModuleActivation.CreateOrderedFactories(plan, [reversed], services);
        var actual = factories.Select(factory => factory().GetType().Name).ToArray();
        var planned = PlannedModuleIds(plan).ToArray();
        var expected = planned.Select(id =>
        {
            var registration = builtIn.FrontendModules.Single(item => item.ContributionId == id);
            return registration.Create(services).GetType().Name;
        }).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(planned, Is.EqualTo(new[]
            {
                WistContributionIds.ArithmeticModule,
                WistContributionIds.NumbersModule,
                WistContributionIds.ScopesModule,
                WistContributionIds.WhitespacesModule
            }));
        });
    }

    [Test]
    public void ExternalPackage_WithNewContributionId_ActivatesWithoutGenericSdkChanges()
    {
        var wist = new WistLanguageFeaturePackage();
        var externalId = new LanguageContributionId("acme.wist.module.audit");
        var externalFeature = new LanguageFeatureId("acme.wist.audit");
        var external = ExternalPackage.Create(externalFeature, externalId, order: 155);
        var registry = new LanguagePackageRegistry().AddPackage(wist).AddPackage(external);
        var definition = DefinitionBuilder(Interpreter)
            .UseFeature(WistFeatureIds.Arithmetic)
            .UseFeature(externalFeature)
            .Build();
        var plan = new LanguageCompiler(registry).Compile(definition).GetRequiredPlan();
        var externalSource = new WistFrontendModuleSource(
            external,
            [new WistFrontendModuleRegistration(externalId, static _ => new ExternalMarkerModule())]);
        using var services = new ServiceCollection().BuildServiceProvider();

        var factories = WistFrontendModuleActivation.CreateOrderedFactories(
            plan,
            [WistFrontendModuleActivation.CreateBuiltInSource(wist), externalSource],
            services);
        var modules = factories.Select(factory => factory()).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(PlannedModuleIds(plan), Does.Contain(externalId));
            Assert.That(modules.OfType<ExternalMarkerModule>().Count(), Is.EqualTo(1));
            Assert.That(modules.Select(static module => module.GetType().Name),
                Does.Contain(nameof(ExternalMarkerModule)));
        });
    }

    [Test]
    public void Activation_MissingSelectedRegistration_FailsClosed()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = Compile(package, Interpreter);
        var builtIn = WistFrontendModuleActivation.CreateBuiltInSource(package);
        var selected = PlannedModuleIds(plan).First();
        var incomplete = new WistFrontendModuleSource(
            package,
            builtIn.FrontendModules.Where(registration => registration.ContributionId != selected));
        using var services = new ServiceCollection().BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WistFrontendModuleActivation.CreateOrderedFactories(plan, [incomplete], services));

        Assert.That(exception!.Message, Does.Contain("exactly one Wist frontend module"));
    }

    [Test]
    public void Activation_DuplicateSelectedRegistration_FailsClosed()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = Compile(package, Interpreter);
        var builtIn = WistFrontendModuleActivation.CreateBuiltInSource(package);
        var selected = PlannedModuleIds(plan).First();
        var duplicate = builtIn.FrontendModules.Single(registration => registration.ContributionId == selected);
        var source = new WistFrontendModuleSource(package, builtIn.FrontendModules.Concat([duplicate]));
        using var services = new ServiceCollection().BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            WistFrontendModuleActivation.CreateOrderedFactories(plan, [source], services));

        Assert.That(exception!.Message, Does.Contain("exactly one Wist frontend module"));
    }

    [Test]
    public void ExternalPackage_ReusingCanonicalContributionId_IsRejectedAtRegistration()
    {
        var wist = new WistLanguageFeaturePackage();
        var external = ExternalPackage.Create(
            new LanguageFeatureId("acme.wist.spoof"),
            WistContributionIds.ArithmeticModule,
            order: 999);
        var registry = new LanguagePackageRegistry().AddPackage(wist);

        var exception = Assert.Throws<InvalidOperationException>(() => registry.AddPackage(external));

        Assert.That(exception!.Message, Does.Contain(WistContributionIds.ArithmeticModule.Value));
    }

    [Test]
    public void ModuleSelection_IsBackendIndependentAndPlanOwned()
    {
        var package = new WistLanguageFeaturePackage();
        var interpreterPlan = Compile(package, Interpreter);
        var cilPlan = Compile(package, Cil);
        var source = WistFrontendModuleActivation.CreateBuiltInSource(package);
        using var services = new ServiceCollection().BuildServiceProvider();

        var interpreterFactories = WistFrontendModuleActivation.CreateOrderedFactories(interpreterPlan, [source], services);
        var cilFactories = WistFrontendModuleActivation.CreateOrderedFactories(cilPlan, [source], services);

        Assert.Multiple(() =>
        {
            Assert.That(PlannedModuleIds(interpreterPlan), Is.EqualTo(PlannedModuleIds(cilPlan)));
            Assert.That(
                interpreterFactories.Select(factory => factory().GetType()),
                Is.EqualTo(cilFactories.Select(factory => factory().GetType())));
        });
    }

    [Test]
    public void ActivatedFactories_CreateIndependentModulesUnderConcurrency()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = Compile(package, Interpreter);
        using var services = new ServiceCollection().BuildServiceProvider();
        var factories = WistFrontendModuleActivation.CreateOrderedFactories(
            plan,
            [WistFrontendModuleActivation.CreateBuiltInSource(package)],
            services);

        var batches = Enumerable.Range(0, 24)
            .Select(_ => Task.Run(() => factories.Select(factory => factory()).ToArray()))
            .ToArray();
        Task.WaitAll(batches);
        var moduleBatches = batches.Select(static task => task.Result).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(moduleBatches.All(batch => batch.Length == factories.Count), Is.True);
            for (var index = 0; index < factories.Count; index++)
                Assert.That(moduleBatches.Select(batch => batch[index]).Distinct().Count(), Is.EqualTo(24));
        });
    }

    [Test]
    public void ExternalModule_DependencyMismatch_IsRejectedByLanguageCompiler()
    {
        var wist = new WistLanguageFeaturePackage();
        var id = new LanguageContributionId("acme.wist.module.requires-missing");
        var feature = new LanguageFeatureId("acme.wist.requires-missing");
        var missing = new LanguageContributionId("acme.wist.module.missing");
        var external = ExternalPackage.Create(feature, id, order: 155, requires: [missing]);
        var registry = new LanguagePackageRegistry().AddPackage(wist).AddPackage(external);

        var result = new LanguageCompiler(registry).Compile(
            DefinitionBuilder(Interpreter)
                .UseFeature(WistFeatureIds.Arithmetic)
                .UseFeature(feature)
                .Build());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains(missing.Value, StringComparison.Ordinal)), Is.True);
        });
    }

    private static LanguagePlan Compile(WistLanguageFeaturePackage package, BackendId backend)
    {
        var result = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(DefinitionBuilder(backend).UseFeature(WistFeatureIds.Arithmetic).Build());
        Assert.That(result.IsSuccess, Is.True,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        return result.GetRequiredPlan();
    }

    private static LanguageDefinitionBuilder DefinitionBuilder(BackendId backend) =>
        LanguageDefinitionBuilder.Create("Wist.ModuleActivation", "1")
            .EnableBackend(backend)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion);

    private static IEnumerable<LanguageContributionId> PlannedModuleIds(LanguagePlan plan) =>
        plan.Contributions
            .Where(static contribution => contribution.Contribution.Slot == LanguageSlots.FrontendSyntax)
            .Select(static contribution => contribution.Contribution.Id);

    private sealed class ExternalMarkerModule : IFrontendCoreModule;

    private sealed class ExternalPackage(LanguagePackageDescriptor descriptor) : ILanguageFeaturePackage
    {
        public LanguagePackageDescriptor Descriptor { get; } = descriptor;

        public static ExternalPackage Create(
            LanguageFeatureId featureId,
            LanguageContributionId contributionId,
            int order,
            IReadOnlyList<LanguageContributionId>? requires = null)
        {
            var descriptor = new LanguagePackageDescriptor(
                new LanguagePackageId($"Acme.Wist.{featureId.Value}"),
                new LanguageVersion("1"),
                ToolchainApi.Current,
                [new LanguageFeatureDescriptor(featureId, supportedBackends: [Interpreter, Cil], contributions: [contributionId])],
                contributions:
                [
                    new LanguageContributionDescriptor(
                        contributionId,
                        LanguageSlots.FrontendSyntax,
                        requiresContributions: requires,
                        requiresCapabilities:
                        [
                            new LanguageCapabilityId("frontend:wist"),
                            new LanguageCapabilityId("lowering:air")
                        ],
                        supportedBackends: [Interpreter, Cil],
                        order: order)
                ]);
            return new ExternalPackage(descriptor);
        }
    }
}
