using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Architecture;

[TestFixture]
public class DeletedLegacySurfaceTests
{
    [Test]
    public void DependencyInjectionAssembly_ShouldNoLongerBeReferencedByCanonicalWistPath()
    {
        var referenced = typeof(WistDialectExecutionWorkflow).Assembly
            .GetReferencedAssemblies()
            .Select(static x => x.Name)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        Assert.That(referenced.Any(static x => string.Equals(x, "Wist.DependencyInjection", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void LegacyCompositionServiceType_ShouldNotExist()
    {
        Assert.That(FindType("Wist.DependencyInjection.WistCompositionService"), Is.Null);
    }

    [Test]
    public void LegacyRegistryTypes_ShouldNotExist()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FindType("Wist.DependencyInjection.AutoRegistration"), Is.Null);
            Assert.That(FindType("Wist.DependencyInjection.ServiceRegistrationRegistry"), Is.Null);
        });
    }

    [Test]
    public void OldBroadWistServiceCollectionExtensions_ShouldNotExist()
    {
        Assert.That(FindType("Wist.DependencyInjection.ServiceCollectionExtensions"), Is.Null);
    }

    [Test]
    public void OldWistOptionsType_ShouldNotExist()
    {
        Assert.That(FindType("Wist.DependencyInjection.WistOptions"), Is.Null);
    }

    [Test]
    public void CanonicalRuntimePath_ShouldNotRequireRemovedDependencyInjectionProject()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect D\nuse Arithmetic\nbackend interpreter", "inline");

        Assert.That(composition.IsSuccess, Is.True, composition.ToDeterministicText());
    }

    private static Type? FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
                   .Select(x => x.GetType(fullName, throwOnError: false, ignoreCase: false))
                   .FirstOrDefault(static x => x != null)
               ?? Type.GetType(fullName, throwOnError: false, ignoreCase: false);
    }
}
