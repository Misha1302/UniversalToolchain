using BasicCore.Builtins;
using BasicCore.Capabilities;
using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist.LanguagePack;

namespace Tests.Backends;

[TestFixture]
public sealed class CilIntrinsicRegistryCompositionTests
{
    [Test]
    public void AddCompilerBackendDefaults_ShouldResolveCompilerWithProviderScopedIntrinsicRegistry()
    {
        var services = new ServiceCollection();
        services.AddCompilerBackendDefaults();

        using var firstProvider = services.BuildServiceProvider();
        using var secondProvider = services.BuildServiceProvider();

        var firstRegistry = firstProvider.GetRequiredService<CilIntrinsicRegistry>();
        var secondRegistry = secondProvider.GetRequiredService<CilIntrinsicRegistry>();
        var compiler = firstProvider.GetRequiredService<AbstractMethodsCompilerImpl>();

        Assert.Multiple(() =>
        {
            Assert.That(firstProvider.GetRequiredService<CilIntrinsicRegistry>(), Is.SameAs(firstRegistry));
            Assert.That(secondRegistry, Is.Not.SameAs(firstRegistry));
            Assert.That(compiler.SupportedIntrinsics, Is.EqualTo(firstRegistry.SupportedIntrinsics));
        });
    }

    [Test]
    public void CanonicalWistCilRoute_UsesCompilerIntrinsicCapabilitySurface()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.FullDefaultNativeId))
            .GetRequiredPlan();
        var compiler = new AbstractMethodsCompilerImpl();
        var registry = new CilIntrinsicRegistry();
        var capabilities = WistIntrinsicPlanPolicy.Create(plan, new BackendId("cil")).ApplyTo(
            new OptimizerIntrinsicCapabilityContext(
                new CompilerIntrinsicCapabilitySetFactory().Create(compiler)));

        Assert.Multiple(() =>
        {
            Assert.That(compiler.SupportedIntrinsics, Is.EqualTo(registry.SupportedIntrinsics));
            Assert.That(registry.SupportedIntrinsics, Does.Contain("add_f64"));
            Assert.That(capabilities.Supports(BuiltinIntrinsicSymbols.Arithmetic.Add, typeof(double)), Is.True);
        });
    }
}
