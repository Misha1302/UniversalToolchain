using Microsoft.Extensions.DependencyInjection;
using Tests.TestInfrastructure;
using UniversalToolchain.Dialects.Integration;
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
        using var provider = TestContractsInfrastructure.CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect D\nuse Arithmetic\nbackend interpreter", "inline");

        Assert.That(composition.IsSuccess, Is.True, composition.ToDeterministicText());
    }

    [Test]
    public void CanonicalRuntimePath_ShouldNotExposeRemovedBroadWistBootstrap()
    {
        var extensionMethods = typeof(WistDialectServiceCollectionExtensions)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(static x => x.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(extensionMethods, Does.Contain(nameof(WistDialectServiceCollectionExtensions.AddWistDialectServices)));
            Assert.That(extensionMethods.Any(static x => x.Contains("Broad", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(extensionMethods.Any(static x => x.Contains("Legacy", StringComparison.OrdinalIgnoreCase)), Is.False);
        });
    }

    [Test]
    public void CanonicalRuntimePath_ShouldNotExposeLegacyCompositionWorkflow()
    {
        var workflowType = typeof(WistDialectExecutionWorkflow);
        var publicMethods = workflowType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(static x => x.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(publicMethods, Does.Contain(nameof(WistDialectExecutionWorkflow.ComposeText)));
            Assert.That(publicMethods, Does.Contain(nameof(WistDialectExecutionWorkflow.ComposeFile)));
            Assert.That(publicMethods.Any(static x => x.Contains("Legacy", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(publicMethods.Any(static x => x.Contains("AutoRegistration", StringComparison.OrdinalIgnoreCase)), Is.False);
        });
    }

    [Test]
    public void RemovedDependencyInjectionAssembly_ShouldNotBeLoadRequiredForCanonicalRuntime()
    {
        using var provider = TestContractsInfrastructure.CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect Canonical\nuse Arithmetic,Numbers\nbackend interpreter", "canonical");
        Assert.That(composition.IsSuccess, Is.True, composition.ToDeterministicText());

        using var host = workflow.CreateHost(composition);
        Assert.DoesNotThrow(() => host.Run("2+2", "interpreter"));

        Assert.That(AppDomain.CurrentDomain.GetAssemblies().Any(static x => string.Equals(x.GetName().Name, "Wist.DependencyInjection", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void CanonicalBootstrap_ShouldWorkWithoutAnyLegacyTypeResolution()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FindType("Wist.DependencyInjection.WistCompositionService"), Is.Null);
            Assert.That(FindType("Wist.DependencyInjection.ServiceCollectionExtensions"), Is.Null);
        });

        using var provider = TestContractsInfrastructure.CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect Pure\nuse Arithmetic,Numbers\nbackend interpreter", "pure");

        Assert.That(composition.IsSuccess, Is.True, composition.ToDeterministicText());
        using var host = workflow.CreateHost(composition);
        Assert.DoesNotThrow(() => host.Run("40+2", "interpreter"));
    }

    private static Type? FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
                   .Select(x => x.GetType(fullName, false, false))
                   .FirstOrDefault(static x => x != null)
               ?? Type.GetType(fullName, false, false);
    }
}
