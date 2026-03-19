using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectRuntimeDescriptorProviderTests
{
    [Test]
    public void RegistryFactory_BuildsDeterministicRealWistCatalog()
    {
        var first = BuildRegistry();
        var second = BuildRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(first.Modules.Keys, Is.EqualTo(second.Modules.Keys));
            Assert.That(first.Optimizers.Keys, Is.EqualTo(second.Optimizers.Keys));
            Assert.That(first.Backends.Keys, Is.EqualTo(second.Backends.Keys));
            Assert.That(first.Intrinsics.Keys, Is.EqualTo(second.Intrinsics.Keys));
            Assert.That(first.Modules.Keys, Does.Contain(WistDialectCatalogNames.Modules.Arithmetic));
            Assert.That(first.Modules.Keys, Does.Contain(WistDialectCatalogNames.Modules.Variables));
            Assert.That(first.Optimizers.Keys, Does.Contain(WistDialectCatalogNames.Optimizers.LocalVariables));
            Assert.That(first.Backends.Keys, Is.EqualTo(new[] { DialectBackendTarget.Cil, DialectBackendTarget.Interpreter }));
            Assert.That(first.Intrinsics.Keys, Does.Contain(("add_i32", DialectBackendTarget.Any)));
        });
    }

    [Test]
    public void AddWistDialectServices_RegistersReusableWorkflowServices()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<DialectRuntimeDescriptorRegistry>().Modules.Keys, Does.Contain(WistDialectCatalogNames.Modules.Whitespaces));
            Assert.That(provider.GetRequiredService<WistDialectExecutionWorkflow>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<WistDialectServiceProviderFactory>(), Is.Not.Null);
        });
    }

    private static DialectRuntimeDescriptorRegistry BuildRegistry()
    {
        return DialectRuntimeDescriptorRegistryFactory.BuildFromProviders([new WistDialectRuntimeDescriptorProvider()]);
    }
}
