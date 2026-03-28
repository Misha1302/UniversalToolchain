using BasicCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class DialectRuntimeArchitectureTests
{
    [Test]
    public void BackendIds_AreOpenEndedAndWildcardMatchesArbitraryBackends()
    {
        var customBackend = new DialectBackendId("custom-backend");

        Assert.Multiple(() =>
        {
            Assert.That(customBackend.Value, Is.EqualTo("custom-backend"));
            Assert.That(DialectBackendSelector.Any.Matches(customBackend), Is.True);
            Assert.That(DialectBackendSelector.For(customBackend).Matches(customBackend), Is.True);
            Assert.That(DialectBackendSelector.For(customBackend).Matches(TestBackendIds.Cil), Is.False);
        });
    }

    [Test]
    public void Registry_ResolvesCanonicalIdsAndAliases_DeterministicallyAndRejectsDuplicates()
    {
        var builder = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor(typeof(FakeFrontendModule), ["FrontendAlias"]))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(typeof(FakeOptimizerModule), ["OptimizerAlias"]))
            .RegisterBackend(new RuntimeBackendDescriptor(new DialectBackendId("custom"), ["custom-alias"]));

        var registry = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(registry.TryResolveModule("FrontendAlias", out var moduleByAlias), Is.True);
            Assert.That(registry.TryResolveModule(typeof(FakeFrontendModule).FullName!, out var moduleByCanonical), Is.True);
            Assert.That(moduleByAlias!.CanonicalId, Is.EqualTo(moduleByCanonical!.CanonicalId));
            Assert.That(registry.TryResolveOptimizer("OptimizerAlias", out var optimizer), Is.True);
            Assert.That(optimizer!.CanonicalId, Is.EqualTo(typeof(FakeOptimizerModule).FullName));
            Assert.That(registry.TryResolveBackend(new DialectBackendId("custom-alias"), out var backend), Is.True);
            Assert.That(backend!.CanonicalId, Is.EqualTo("custom"));
        });

        var duplicateModuleAlias = Assert.Throws<ArgumentException>(() => new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor(typeof(FakeFrontendModule), ["dup"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(FakeFrontendModule2), ["dup"])));
        var duplicateBackendAlias = Assert.Throws<ArgumentException>(() => new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterBackend(new RuntimeBackendDescriptor(new DialectBackendId("backend-a"), ["dup"]))
            .RegisterBackend(new RuntimeBackendDescriptor(new DialectBackendId("backend-b"), ["dup"])));

        Assert.Multiple(() =>
        {
            Assert.That(duplicateModuleAlias!.Message, Does.Contain("dup"));
            Assert.That(duplicateBackendAlias!.Message, Does.Contain("dup"));
        });
    }

    [Test]
    public void WistIntrinsicRegistry_ComesFromRealBackendCapabilities()
    {
        var registry = BuildRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.GetIntrinsicDescriptors("call C#").Select(x => x.Target), Is.EqualTo(new[] { DialectBackendSelector.Any }));
            Assert.That(registry.GetIntrinsicDescriptors("add_i32").Select(x => x.Target), Is.EqualTo(new[] { TestBackendIds.CilSelector }));
            Assert.That(registry.GetIntrinsicDescriptors("ldloc"), Is.Empty);
        });
    }

    [Test]
    public void WistHost_ResolvesCompilerAliasThroughBackendDescriptors()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("full-default");

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(host.GetCore("compiler"), Is.Not.Null);
            Assert.That(host.GetCore("interpreter"), Is.Not.Null);
        });
    }

    [Test]
    public void WistExamples_DoNotExposeDecorativeSecurityOrCapabilityDirectives()
    {
        var exampleRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist"));
        var exampleFiles = Directory.GetFiles(exampleRoot, "dialect.wistdialect", SearchOption.AllDirectories)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.That(exampleFiles, Is.Not.Empty);
        foreach (var file in exampleFiles)
        {
            var text = File.ReadAllText(file);
            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Not.Contain("security "));
                Assert.That(text, Does.Not.Contain("capability "));
            });
        }
    }

    private static DialectRuntimeDescriptorRegistry BuildRegistry()
    {
        using var provider = CreateProvider();
        return provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static string ResolveExampleDirectory(string name)
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", name));
    }

    private sealed class FakeFrontendModule : IFrontendCoreModule
    {
    }

    private sealed class FakeFrontendModule2 : IFrontendCoreModule
    {
    }

    private sealed class FakeOptimizerModule : IIRProcessingModule
    {
    }
}
