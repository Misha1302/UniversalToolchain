namespace Tests;

[TestFixture]
public class DependencyInjectionRegistrationTests
{
    [Test]
    public void AddWistServices_RegistersExecutableGiversForDynamicMethodAndAbstractIr()
    {
        var services = new ServiceCollection();
        services.AddWistServices();

        using var provider = services.BuildServiceProvider();

        var dynamicMethodExecutableGivers = provider.GetServices<IExecutableGiver<DynamicMethod>>().ToList();
        var abstractIrExecutableGivers = provider.GetServices<IExecutableGiver<IAbstractIR>>().ToList();

        Assert.That(dynamicMethodExecutableGivers, Is.Not.Empty);
        Assert.That(abstractIrExecutableGivers, Is.Not.Empty);
    }
}