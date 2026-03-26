using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure;

[TestFixture]
public class DeterministicCompositionAndDiscoveryTests
{
    [Test]
    public void AddWistServices_ShouldProduceStableRelevantRegistrations_AcrossRepeatedBuilds()
    {
        var baseline = DescribeServices();
        for (var i = 0; i < 25; i++)
        {
            Assert.That(DescribeServices(), Is.EqualTo(baseline));
        }
    }

    [Test]
    public void AutoRegistration_ShouldDiscoverServicesDeterministically_AcrossRepeatedBuilds()
    {
        var first = DescribeAutoRegistered();
        var second = DescribeAutoRegistered();

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void RuntimeDescriptorProjection_ShouldBeStable_AcrossRepeatedComposition()
    {
        var first = ResolveFrontendModuleTypeNames();

        for (var i = 0; i < 20; i++)
        {
            Assert.That(ResolveFrontendModuleTypeNames(), Is.EqualTo(first));
        }
    }

    [Test]
    public void DuplicateOrOverlappingRegistrations_ShouldBehavePredictably()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDuplicateContract, DuplicateA>();
        services.AddSingleton<IDuplicateContract, DuplicateB>();
        services.AddSingleton<IDuplicateContract, DuplicateA>();

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetServices<IDuplicateContract>().Select(x => x.GetType().Name).ToArray();

        Assert.That(resolved, Is.EqualTo(new[] { nameof(DuplicateA), nameof(DuplicateB), nameof(DuplicateA) }));
    }

    private static string[] DescribeServices()
    {
        var services = new ServiceCollection();
        services.AddWistServices();
        return services
            .Where(x => x.ImplementationType != null)
            .Select(x => $"{x.Lifetime}|{x.ServiceType.FullName}|{x.ImplementationType!.FullName}")
            .ToArray();
    }

    private static string[] DescribeAutoRegistered()
    {
        var services = new ServiceCollection();
        services.AddAutoRegisteredServices(typeof(ServiceCollectionExtensions).Assembly);
        return services
            .Where(x => x.ImplementationType != null)
            .Select(x => $"{x.Lifetime}|{x.ServiceType.FullName}|{x.ImplementationType!.FullName}")
            .ToArray();
    }

    private static string[] ResolveFrontendModuleTypeNames()
    {
        var services = new ServiceCollection();
        services.AddWistServices();
        using var provider = services.BuildServiceProvider();
        return provider.GetServices<IFrontendCoreModule>().Select(x => x.GetType().FullName!).ToArray();
    }

    private interface IDuplicateContract;
    private sealed class DuplicateA : IDuplicateContract;
    private sealed class DuplicateB : IDuplicateContract;
}
