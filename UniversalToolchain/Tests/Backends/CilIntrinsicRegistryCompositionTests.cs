using UniversalToolchain.Dialects.Core.ServiceCollection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

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
    public void WistCilBackendRegistrar_ShouldExposeRegistryIntrinsicSurface()
    {
        var services = new ServiceCollection();
        services.AddWistCilBackend();

        using var provider = services.BuildServiceProvider();

        var registry = new CilIntrinsicRegistry();
        var registrar = provider.GetServices<IDialectBackendRuntimeRegistrar>().Single();

        Assert.That(registrar.SupportedIntrinsics, Is.EqualTo(registry.SupportedIntrinsics));
    }
}
