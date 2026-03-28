using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure;

[TestFixture]
public class DeterministicCompositionAndDiscoveryTests
{
    [Test]
    public void AddWistServices_ShouldProduceStableRelevantRegistrations_AcrossRepeatedBuilds()
    {
        var baseline = DescribeServiceCollection(CreateServices());

        for (var i = 0; i < 25; i++)
            Assert.That(DescribeServiceCollection(CreateServices()), Is.EqualTo(baseline));
    }

    [Test]
    public void AddWistServices_ShouldProduceStableCoreRunnableProjection_AcrossRepeatedProviders()
    {
        var baseline = DescribeResolvedCoreRunnables();

        for (var i = 0; i < 20; i++)
            Assert.That(DescribeResolvedCoreRunnables(), Is.EqualTo(baseline));
    }

    [Test]
    public void AutoRegistration_ShouldProduceStableProjection_AcrossRepeatedBuilds()
    {
        var baseline = DescribeAutoRegisteredServices();

        for (var i = 0; i < 25; i++)
            Assert.That(DescribeAutoRegisteredServices(), Is.EqualTo(baseline));
    }

    [Test]
    public void ArithmeticModeFiltering_ShouldBeDeterministic_ForRelevantModuleRegistrations()
    {
        var universal = DescribeFrontendModules(options => options.ArithmeticMode = ArithmeticMode.Universal);
        var native = DescribeFrontendModules(options => options.ArithmeticMode = ArithmeticMode.Native);

        Assert.That(universal, Is.Not.EqualTo(native));
        Assert.That(DescribeFrontendModules(options => options.ArithmeticMode = ArithmeticMode.Universal), Is.EqualTo(universal));
        Assert.That(DescribeFrontendModules(options => options.ArithmeticMode = ArithmeticMode.Native), Is.EqualTo(native));
    }

    [Test]
    public void ArithmeticModeFiltering_ShouldUseTypeMetadata_NotNamespaceNames()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMetadataTestService, StrangeNamespaceNativeService>();
        services.AddSingleton<IMetadataTestService, StrangeNamespaceUniversalService>();

        ApplyArithmeticModePolicy(services, ArithmeticMode.Universal);
        Assert.That(DescribeMetadataServices(services), Is.EqualTo(new[] { typeof(StrangeNamespaceUniversalService).FullName! }));

        var nativeServices = new ServiceCollection();
        nativeServices.AddSingleton<IMetadataTestService, StrangeNamespaceNativeService>();
        nativeServices.AddSingleton<IMetadataTestService, StrangeNamespaceUniversalService>();

        ApplyArithmeticModePolicy(nativeServices, ArithmeticMode.Native);
        Assert.That(DescribeMetadataServices(nativeServices), Is.EqualTo(new[] { typeof(StrangeNamespaceNativeService).FullName! }));
    }

    [Test]
    public void ArithmeticModeFiltering_ShouldNotRequireCentralModuleCatalog()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMetadataTestService, CatalogIndependentUniversalService>();

        ApplyArithmeticModePolicy(services, ArithmeticMode.Universal);
        Assert.That(DescribeMetadataServices(services), Is.EqualTo(new[] { typeof(CatalogIndependentUniversalService).FullName! }));

        var nativeServices = new ServiceCollection();
        nativeServices.AddSingleton<IMetadataTestService, CatalogIndependentUniversalService>();

        ApplyArithmeticModePolicy(nativeServices, ArithmeticMode.Native);
        Assert.That(DescribeMetadataServices(nativeServices), Is.Empty);
    }

    [Test]
    public void ArithmeticModeFiltering_ShouldPreserveDeterminism()
    {
        var universalBaseline = DescribeMetadataPolicyProjection(ArithmeticMode.Universal);
        var nativeBaseline = DescribeMetadataPolicyProjection(ArithmeticMode.Native);

        Assert.That(universalBaseline, Is.Not.EqualTo(nativeBaseline));

        for (var i = 0; i < 15; i++)
        {
            Assert.That(DescribeMetadataPolicyProjection(ArithmeticMode.Universal), Is.EqualTo(universalBaseline));
            Assert.That(DescribeMetadataPolicyProjection(ArithmeticMode.Native), Is.EqualTo(nativeBaseline));
        }
    }

    [Test]
    public void ArithmeticModeFiltering_ShouldNotDependOnNamespaceRenaming()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMetadataTestService, NamespaceOneUniversalService>();
        services.AddSingleton<IMetadataTestService, NamespaceTwoUniversalService>();

        ApplyArithmeticModePolicy(services, ArithmeticMode.Universal);
        Assert.That(
            DescribeMetadataServices(services),
            Is.EqualTo(new[]
            {
                typeof(NamespaceOneUniversalService).FullName!,
                typeof(NamespaceTwoUniversalService).FullName!
            }));

        var nativeServices = new ServiceCollection();
        nativeServices.AddSingleton<IMetadataTestService, NamespaceOneUniversalService>();
        nativeServices.AddSingleton<IMetadataTestService, NamespaceTwoUniversalService>();

        ApplyArithmeticModePolicy(nativeServices, ArithmeticMode.Native);
        Assert.That(DescribeMetadataServices(nativeServices), Is.Empty);
    }

    private static ServiceCollection CreateServices(Action<WistOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddWistServices(configure);
        return services;
    }

    private static string[] DescribeServiceCollection(ServiceCollection services)
    {
        return services.Select(static d =>
            string.Join("|", [
                d.Lifetime.ToString(),
                d.ServiceType.FullName ?? "<null>",
                d.ImplementationType?.FullName ?? "<none>",
                d.ImplementationFactory != null ? "factory" : "no-factory",
                d.ImplementationInstance != null ? "instance" : "no-instance"
            ])).ToArray();
    }

    private static string[] DescribeAutoRegisteredServices()
    {
        var services = new ServiceCollection();
        services.AddAutoRegisteredServices(typeof(ServiceCollectionExtensions).Assembly);
        return DescribeServiceCollection(services);
    }

    private static string[] DescribeResolvedCoreRunnables()
    {
        using var provider = CreateServices().BuildServiceProvider();
        return provider.GetServices<ICoreRunnable>()
            .Select(x => x.GetType().FullName ?? x.GetType().Name)
            .ToArray();
    }

    private static string[] DescribeFrontendModules(Action<WistOptions> configure)
    {
        using var provider = CreateServices(configure).BuildServiceProvider();
        return provider.GetServices<IFrontendCoreModule>()
            .Select(x => x.GetType().FullName ?? x.GetType().Name)
            .ToArray();
    }

    private static void ApplyArithmeticModePolicy(ServiceCollection services, ArithmeticMode mode)
    {
        var method = typeof(ServiceCollectionExtensions).GetMethod(
            "ApplyArithmeticModePolicy",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        method!.Invoke(null, new object?[] { services, mode });
    }

    private static string[] DescribeMetadataPolicyProjection(ArithmeticMode mode)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMetadataTestService, StrangeNamespaceNativeService>();
        services.AddSingleton<IMetadataTestService, StrangeNamespaceUniversalService>();
        services.AddSingleton<IMetadataTestService, NamespaceOneUniversalService>();
        services.AddSingleton<IMetadataTestService, NamespaceTwoUniversalService>();
        ApplyArithmeticModePolicy(services, mode);
        return DescribeMetadataServices(services);
    }

    private static string[] DescribeMetadataServices(ServiceCollection services)
    {
        return services
            .Where(static d => d.ServiceType == typeof(IMetadataTestService))
            .Select(static d => d.ImplementationType!.FullName!)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();
    }

    private interface IMetadataTestService;

    [ArithmeticModeCompatibility(ArithmeticMode.Native)]
    private sealed class StrangeNamespaceNativeService : IMetadataTestService;

    [ArithmeticModeCompatibility(ArithmeticMode.Universal)]
    private sealed class StrangeNamespaceUniversalService : IMetadataTestService;

    [ArithmeticModeCompatibility(ArithmeticMode.Universal)]
    private sealed class CatalogIndependentUniversalService : IMetadataTestService;

    [ArithmeticModeCompatibility(ArithmeticMode.Universal)]
    private sealed class NamespaceOneUniversalService : IMetadataTestService;

    [ArithmeticModeCompatibility(ArithmeticMode.Universal)]
    private sealed class NamespaceTwoUniversalService : IMetadataTestService;
}
