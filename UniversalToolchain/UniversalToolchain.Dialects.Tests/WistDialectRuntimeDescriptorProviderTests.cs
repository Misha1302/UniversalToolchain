using Microsoft.Extensions.DependencyInjection;
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
            Assert.That(first.TryResolveModule("Arithmetic", out var arithmeticModule), Is.True);
            Assert.That(arithmeticModule!.CanonicalId, Does.Contain("ArithmeticModuleImpl"));
            Assert.That(first.TryResolveModule("Variables", out var variablesModule), Is.True);
            Assert.That(variablesModule!.CanonicalId, Does.Contain("VariablesModuleImpl"));
            Assert.That(first.TryResolveOptimizer("LocalVariablesOptimization", out var localVariablesOptimizer), Is.True);
            Assert.That(localVariablesOptimizer!.CanonicalId, Does.Contain("LocalVariablesOptimizer"));
            Assert.That(first.Backends.Keys, Is.EqualTo(new[] { TestBackendIds.Cil, TestBackendIds.Interpreter }));
            Assert.That(first.Intrinsics.Keys, Does.Contain(("add_i32", TestBackendIds.CilSelector)));
        });
    }

    [Test]
    public void AddWistDialectServices_RegistersReusableWorkflowServices()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();

        Assert.Multiple(() =>
        {
            Assert.That(registry.TryResolveModule("Whitespaces", out _), Is.True);
            Assert.That(provider.GetRequiredService<WistDialectExecutionWorkflow>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<WistDialectServiceProviderFactory>(), Is.Not.Null);
        });
    }

    private static DialectRuntimeDescriptorRegistry BuildRegistry() => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders([new WistDialectRuntimeDescriptorProvider()]);
}
