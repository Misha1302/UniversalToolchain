using NumbersModule.Core;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistDirectBackendRuntimeTests
{
    private static readonly BackendId Interpreter = new("interpreter");
    private static readonly BackendId Cil = new("cil");

    [Test]
    public void DirectRuntime_ExecutesInterpreterThroughGenericRoute()
    {
        var (package, plan) = CompileArithmeticPlan([Interpreter]);
        using var runtime = CreateRuntime(package, plan);

        var result = runtime.Run(new LanguageExecutionRequest("2 + 3", Interpreter));

        Assert.Multiple(() =>
        {
            Assert.That(result.Backend, Is.EqualTo(Interpreter));
            Assert.That(result.Value, Is.EqualTo(5d));
            Assert.That(result.Value, Is.TypeOf<double>());
        });
    }

    [Test]
    public void DirectRuntime_InterpreterAndCilHaveSamePublicValueBoundary()
    {
        var (package, plan) = CompileArithmeticPlan([Interpreter, Cil]);
        using var runtime = CreateRuntime(package, plan);

        var interpreter = runtime.Run(new LanguageExecutionRequest("2 + 3 * 4", Interpreter));
        var cil = runtime.Run(new LanguageExecutionRequest("2 + 3 * 4", Cil));

        Assert.Multiple(() =>
        {
            Assert.That(interpreter.Value, Is.EqualTo(14d));
            Assert.That(cil.Value, Is.EqualTo(interpreter.Value));
            Assert.That(cil.Value?.GetType(), Is.EqualTo(interpreter.Value?.GetType()));
        });
    }

    [Test]
    public void NumbersFeature_PlansExplicitRealNumberValueAdapter()
    {
        var (_, plan) = CompileArithmeticPlan([Interpreter]);

        var adapters = plan.Contributions
            .Where(static contribution => contribution.Contribution.Slot == WistLanguageSlots.RuntimeValueAdapters)
            .Select(static contribution => contribution.Contribution.Id)
            .ToArray();

        Assert.That(adapters, Is.EqualTo(new[] { WistContributionIds.RealNumberValueAdapter }));
    }

    [Test]
    public void ExplicitValueAdapter_NormalizesRealNumberWithoutReflectionFallback()
    {
        var (_, plan) = CompileArithmeticPlan([Interpreter]);

        var value = WistRuntimeValueAdapterActivation.Normalize(plan, RealNumberImpl.Create(17.5));

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(17.5d));
            Assert.That(value, Is.TypeOf<double>());
        });
    }

    [Test]
    public void MissingSelectedValueAdapterRegistration_FailsClosed()
    {
        var (_, plan) = CompileArithmeticPlan([Interpreter]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            WistRuntimeValueAdapterActivation.Normalize(plan, [], RealNumberImpl.Create(1)));

        Assert.That(error!.Message, Does.Contain(WistContributionIds.RealNumberValueAdapter.Value));
    }

    [Test]
    public void DuplicateSelectedValueAdapterRegistration_FailsClosed()
    {
        var (_, plan) = CompileArithmeticPlan([Interpreter]);
        var first = new WistRuntimeValueAdapterRegistration(
            WistContributionIds.RealNumberValueAdapter,
            typeof(RealNumberImpl),
            static value => ((RealNumberImpl)value).GetValue());
        var second = new WistRuntimeValueAdapterRegistration(
            WistContributionIds.RealNumberValueAdapter,
            typeof(RealNumberImpl),
            static value => ((RealNumberImpl)value).GetValue());

        var error = Assert.Throws<InvalidOperationException>(() =>
            WistRuntimeValueAdapterActivation.Normalize(plan, [first, second], RealNumberImpl.Create(1)));

        Assert.That(error!.Message, Does.Contain("exactly one exact registration"));
    }

    [Test]
    public void RealNumberWithoutPlannedAdapter_FailsClosed()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(LanguageDefinitionBuilder
                .Create("wist.direct.no-number-adapter", WistLanguageFeaturePackage.PackageVersion.Value)
                .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
                .UseFeature(WistFeatureIds.Whitespaces)
                .UseFeature(WistFeatureIds.Scopes)
                .UseFeature(WistSsaPolicyFeatureIds.Disabled)
                .EnableBackend(Interpreter)
                .Build())
            .GetRequiredPlan();

        var error = Assert.Throws<InvalidOperationException>(() =>
            WistRuntimeValueAdapterActivation.Normalize(plan, RealNumberImpl.Create(1)));

        Assert.That(error!.Message, Does.Contain("without the planned"));
    }

    [Test]
    public void DirectProvider_RejectsHostInteropWhenPolicyForbidsIt()
    {
        var package = new WistLanguageFeaturePackage();
        var definition = LanguageDefinitionBuilder
            .Create("wist.direct.host-policy", WistLanguageFeaturePackage.PackageVersion.Value)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .WithRuntimePolicy(new LanguageRuntimePolicy(AllowHostInterop: false))
            .UseFeature(WistFeatureIds.CSharpInterop)
            .UseFeature(WistSsaPolicyFeatureIds.Disabled)
            .EnableBackend(Interpreter)
            .Build();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
        var provider = new WistDirectLanguageRuntimeProvider(plan, package);

        var error = Assert.Throws<InvalidOperationException>(() =>
            LanguageRuntime.Create(plan, new LanguageRuntimeProviderRegistry().AddProvider(provider)));

        Assert.That(error!.Message, Does.Contain("forbids host interop"));
    }

    [Test]
    public void DirectProvider_IsBoundToExactPlannedPackageInstance()
    {
        var (plannedPackage, plan) = CompileArithmeticPlan([Interpreter]);
        var equivalentPackage = new WistLanguageFeaturePackage();

        Assert.Multiple(() =>
        {
            Assert.That(
                LanguageFeatureManifestSerializer.ComputeSha256(equivalentPackage.Descriptor),
                Is.EqualTo(LanguageFeatureManifestSerializer.ComputeSha256(plannedPackage.Descriptor)));
            Assert.Throws<InvalidOperationException>(() => new WistDirectLanguageRuntimeProvider(plan, equivalentPackage));
        });
    }

    [Test]
    public void DirectSsaRoute_ExecutesBothBackendsWithoutDialectHost()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.SsaId))
            .GetRequiredPlan();
        using var runtime = CreateRuntime(package, plan);

        var interpreter = runtime.Run(new LanguageExecutionRequest("2 + 3", Interpreter));
        var cil = runtime.Run(new LanguageExecutionRequest("2 + 3", Cil));

        Assert.Multiple(() =>
        {
            Assert.That(interpreter.Value?.ToString(), Is.EqualTo("5"));
            Assert.That(cil.Value, Is.EqualTo(interpreter.Value));
            Assert.That(cil.Value?.GetType(), Is.EqualTo(interpreter.Value?.GetType()));
        });
    }

    private static LanguageRuntime CreateRuntime(WistLanguageFeaturePackage package, LanguagePlan plan) =>
        LanguageRuntime.Create(
            plan,
            new LanguageRuntimeProviderRegistry()
                .AddProvider(new WistDirectLanguageRuntimeProvider(plan, package)));

    private static (WistLanguageFeaturePackage Package, LanguagePlan Plan) CompileArithmeticPlan(
        IReadOnlyList<BackendId> backends)
    {
        var package = new WistLanguageFeaturePackage();
        var builder = LanguageDefinitionBuilder
            .Create("wist.direct.arithmetic", WistLanguageFeaturePackage.PackageVersion.Value)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .UseFeature(WistFeatureIds.Arithmetic)
            .UseFeature(WistSsaPolicyFeatureIds.Disabled);
        foreach (var backend in backends)
            builder.EnableBackend(backend);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(builder.Build())
            .GetRequiredPlan();
        return (package, plan);
    }
}
