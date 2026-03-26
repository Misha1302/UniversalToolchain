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
    public void AutoRegistration_ShouldDiscoverServicesDeterministically_WhenAssemblyOrderChanges()
    {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;

        var servicesA = new ServiceCollection();
        servicesA.AddAutoRegisteredServices([assembly]);

        var servicesB = new ServiceCollection();
        servicesB.AddAutoRegisteredServices((new[] { assembly }).Reverse());

        Assert.That(DescribeServiceCollection(servicesB), Is.EqualTo(DescribeServiceCollection(servicesA)));
    }

    [Test]
    public void ArithmeticModeFiltering_ShouldBeDeterministic_ForRelevantModuleRegistrations()
    {
        var universal = DescribeFrontendModules(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Universal);
        var native = DescribeFrontendModules(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native);

        Assert.That(universal, Is.Not.EqualTo(native));
        Assert.That(DescribeFrontendModules(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Universal), Is.EqualTo(universal));
        Assert.That(DescribeFrontendModules(options => options.ArithmeticMode = WistOptions.ArithmeticModeEnum.Native), Is.EqualTo(native));
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
}
