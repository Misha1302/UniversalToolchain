using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class RuntimeInfrastructureBootstrapContractTests
{
    [Test]
    public void AddFileSystemRuntimeCatalogServices_ShouldRegisterCatalogArtifactsOnly()
    {
        var services = new ServiceCollection();
        services.AddFileSystemRuntimeCatalogServices();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(static x => x.ServiceType == typeof(RuntimeArtifactLocatorOptions)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeManifestFileLocator)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeManifestSerializer)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentCatalog)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentTypeLoader)), Is.False);
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectExecutionWorkflow)), Is.False);
        });
    }

    [Test]
    public void AddReflectionRuntimeResolutionServices_ShouldRegisterResolutionArtifactsOnly()
    {
        var services = new ServiceCollection();
        services.AddReflectionRuntimeResolutionServices();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeAssemblyLocator)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeAssemblyLoadStrategy)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeAssemblyTypeLoader)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentResolver)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeComponentTypeLoader)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeBackendRegistrarResolver)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(IRuntimeKnownBackendsProvider)), Is.True);
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectExecutionWorkflow)), Is.False);
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectServiceProviderFactory)), Is.False);
        });
    }

    [Test]
    public void GenericRuntimeBootstrap_ComposedBlocks_ShouldNotRegisterWistSpecificRuntimeTypes()
    {
        var services = new ServiceCollection();
        services
            .AddFileSystemRuntimeCatalogServices()
            .AddReflectionRuntimeResolutionServices();

        Assert.Multiple(() =>
        {
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectExecutionWorkflow)), Is.False);
            Assert.That(services.Any(static x => x.ServiceType == typeof(WistDialectServiceProviderFactory)), Is.False);
        });
    }
}