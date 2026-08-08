using BasicCore.Builtins;
using BasicCore.Capabilities;
using BasicCore.Contracts;
using UniversalIntermediateRepresentation;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistIntrinsicPlanPolicyTests
{
    private static readonly BackendId Cil = new("cil");

    [Test]
    public void ForbiddenIntrinsic_IsHiddenFromOptimizerCapabilityContext()
    {
        var policy = WistIntrinsicPlanPolicy.Create(CreatePlan(), Cil);
        var capabilities = policy.ApplyTo(new AllowAllCapabilityContext());

        Assert.Multiple(() =>
        {
            Assert.That(capabilities.Supports(BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(int)), Is.False);
            Assert.That(capabilities.Supports(BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(long)), Is.True);
        });
    }

    [Test]
    public void ForbiddenIntrinsic_ReachingBackendAir_FailsClosed()
    {
        var policy = WistIntrinsicPlanPolicy.Create(CreatePlan(), Cil);
        var air = new AbstractIR();
        air.AppendInstructions([
            BuiltinIntrinsicInstruction.Create(BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(int))
        ]);

        var exception = Assert.Throws<InvalidOperationException>(() => policy.Validate(air));

        Assert.That(exception!.Message, Does.Contain("add_i32").And.Contain("forbidden"));
    }

    private static LanguagePlan CreatePlan()
    {
        var package = new WistLanguageFeaturePackage();
        var definition = LanguageDefinitionBuilder
            .Create("wist.intrinsic-policy.tests", WistLanguageFeaturePackage.PackageVersion.Value)
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .UseFeature(WistFeatureIds.Arithmetic)
            .UseFeature(WistSsaPolicyFeatureIds.Disabled)
            .EnableBackend(Cil)
            .ConfigureIntrinsic(new LanguageIntrinsicId("add_i32"), allowed: false, Cil)
            .Build();

        return new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
    }

    private sealed class AllowAllCapabilityContext : IOptimizerIntrinsicCapabilityContext
    {
        public bool Supports(IntrinsicSymbol symbol, params Type[] typeArguments) => true;

        public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<Type> typeArguments) => true;
    }
}
