using ArithmeticModule;
using ArithmeticModule.Module;
using ConditionsModule;
using LocalVariablesOptimizerModule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectRuntimeDescriptorProviderTests
{
    [Test]
    public void RegistryFactory_BuildsDeterministicRealWistCatalog()
    {
        var first = BuildRegistryFromServices();
        var second = BuildRegistryFromServices();

        Assert.Multiple(() =>
        {
            Assert.That(first.Modules.Keys, Is.EqualTo(second.Modules.Keys));
            Assert.That(first.Optimizers.Keys, Is.EqualTo(second.Optimizers.Keys));
            Assert.That(first.Backends.Keys, Is.EqualTo(second.Backends.Keys));
            Assert.That(first.Intrinsics.Keys, Is.EqualTo(second.Intrinsics.Keys));
            Assert.That(first.TryResolveModule("Arithmetic", out var arithmeticModule), Is.True);
            Assert.That(arithmeticModule!.CanonicalId, Is.EqualTo("Arithmetic"));
            Assert.That(first.TryResolveModule("Variables", out var variablesModule), Is.True);
            Assert.That(variablesModule!.CanonicalId, Is.EqualTo("Variables"));
            Assert.That(first.TryResolveOptimizer("LocalVariablesOptimization", out var localVariablesOptimizer), Is.True);
            Assert.That(localVariablesOptimizer!.CanonicalId, Is.EqualTo("LocalVariablesOptimization"));
            Assert.That(first.Backends.Keys, Is.EqualTo(new[] { TestBackendIds.Cil, TestBackendIds.Interpreter }));
            Assert.That(first.Intrinsics.Keys, Does.Contain(("add_i32", TestBackendIds.CilSelector)));
        });
    }

    [Test]
    public void AddWistDialectServices_RegistersReusableWorkflowServices()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServicesLegacy();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();

        Assert.Multiple(() =>
        {
            Assert.That(registry.TryResolveModule("Whitespaces", out _), Is.True);
            Assert.That(provider.GetRequiredService<WistDialectExecutionWorkflow>(), Is.Not.Null);
            Assert.That(provider.GetRequiredService<WistDialectServiceProviderFactory>(), Is.Not.Null);
        });
    }

    [Test]
    public void AddWistDialectServices_AllowsExtendingRuntimeDescriptorDiscoveryViaProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServicesLegacy();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDialectRuntimeDescriptorProvider, TestOnlyRuntimeDescriptorProvider>());

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();

        Assert.Multiple(() =>
        {
            Assert.That(registry.TryResolveModule("TestOnlyFrontend", out var module), Is.True);
            Assert.That(module!.CanonicalId, Does.Contain(nameof(TestOnlyAttributedFrontendModule)));
            Assert.That(registry.TryResolveOptimizer("TestOnlyOptimizer", out var optimizer), Is.True);
            Assert.That(optimizer!.CanonicalId, Does.Contain(nameof(TestOnlyAttributedOptimizer)));
        });
    }

    [Test]
    public void RegistryFactory_CompositionIsDeterministicRegardlessOfProviderRegistrationOrder()
    {
        var first = DialectRuntimeDescriptorRegistryFactory.BuildFromProviders([
            new TestOnlyRuntimeDescriptorProvider(),
            new LocalVariablesOptimizerDialectRuntimeDescriptorProvider(),
            new WistDialectRuntimeDescriptorProvider(Array.Empty<IDialectBackendRuntimeRegistrar>()),
            new ArithmeticDialectRuntimeDescriptorProvider(),
            new ConditionsDialectRuntimeDescriptorProvider()
        ]);
        var second = DialectRuntimeDescriptorRegistryFactory.BuildFromProviders([
            new ConditionsDialectRuntimeDescriptorProvider(),
            new ArithmeticDialectRuntimeDescriptorProvider(),
            new WistDialectRuntimeDescriptorProvider(Array.Empty<IDialectBackendRuntimeRegistrar>()),
            new LocalVariablesOptimizerDialectRuntimeDescriptorProvider(),
            new TestOnlyRuntimeDescriptorProvider()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(first.Modules.Keys, Is.EqualTo(second.Modules.Keys));
            Assert.That(first.Optimizers.Keys, Is.EqualTo(second.Optimizers.Keys));
            Assert.That(first.Backends.Keys, Is.EqualTo(second.Backends.Keys));
            Assert.That(first.Intrinsics.Keys, Is.EqualTo(second.Intrinsics.Keys));
        });
    }

    [Test]
    public void RegistryFactory_RejectsInvalidProviderInputClearly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("providers"));
            Assert.That(
                () => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders([null!]),
                Throws.ArgumentException.With.Message.Contains("Provider collection must not contain null entries."));
            Assert.That(
                () => new WistDialectRuntimeDescriptorProvider(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("backendProviders"));
            Assert.That(
                () => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders([new NullTypeEntryRuntimeDescriptorProvider()]),
                Throws.ArgumentException.With.Message.Contains("Type list must not contain null entries."));
        });
    }

    [Test]
    public void RegistryFactory_PreservesDuplicateCollisionValidationAcrossDistributedProviders()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders([
                    new ArithmeticDialectRuntimeDescriptorProvider(),
                    new ConflictingModuleRuntimeDescriptorProvider()
                ]),
                Throws.ArgumentException.With.Message.Contains("module alias 'Arithmetic'"));
            Assert.That(
                () => DialectRuntimeDescriptorRegistryFactory.BuildFromProviders([
                    new LocalVariablesOptimizerDialectRuntimeDescriptorProvider(),
                    new ConflictingOptimizerRuntimeDescriptorProvider()
                ]),
                Throws.ArgumentException.With.Message.Contains("optimizer alias 'LocalVariablesOptimization'"));
        });
    }

    private static DialectRuntimeDescriptorRegistry BuildRegistryFromServices()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServicesLegacy();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();
    }

    private sealed class TestOnlyRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
    {
        public decimal Order => 700m;

        public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
        {
            builder
                .RegisterAttributedModules(typeof(TestOnlyAttributedFrontendModule))
                .RegisterAttributedOptimizers(typeof(TestOnlyAttributedOptimizer));
        }
    }

    private sealed class NullTypeEntryRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
    {
        public decimal Order => 700m;

        public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
        {
            builder.RegisterAttributedModules([null!]);
        }
    }

    private sealed class ConflictingModuleRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
    {
        public decimal Order => 700m;

        public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
        {
            builder.RegisterAttributedModules(typeof(ConflictingAttributedArithmeticModule));
        }
    }

    private sealed class ConflictingOptimizerRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
    {
        public decimal Order => 700m;

        public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
        {
            builder.RegisterAttributedOptimizers(typeof(ConflictingAttributedOptimizer));
        }
    }

    [DialectModuleAlias("TestOnlyFrontend")]
    private sealed class TestOnlyAttributedFrontendModule : ArithmeticModuleImpl
    {
    }

    [DialectOptimizerAlias("TestOnlyOptimizer")]
    private sealed class TestOnlyAttributedOptimizer : LocalVariablesOptimizer
    {
    }

    [DialectModuleAlias("Arithmetic")]
    private sealed class ConflictingAttributedArithmeticModule : ArithmeticModuleImpl
    {
    }

    [DialectOptimizerAlias("LocalVariablesOptimization")]
    private sealed class ConflictingAttributedOptimizer : LocalVariablesOptimizer
    {
    }
}
