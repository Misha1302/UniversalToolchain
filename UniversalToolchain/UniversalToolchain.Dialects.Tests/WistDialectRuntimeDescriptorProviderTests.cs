using System.Reflection;
using System.Reflection.Emit;
using ArithmeticModule.Module;
using LocalVariablesOptimizerModule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectRuntimeDescriptorProviderTests
{
    private static readonly Assembly TestRuntimeExtensionAssembly = CreateTestRuntimeExtensionAssembly();

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

    [Test]
    public void AddWistDialectServices_AllowsExtendingRuntimeAssemblyDiscoveryViaContributor()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWistDialectRuntimeAssemblyContributor, TestOnlyRuntimeAssemblyContributor>());

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();

        Assert.Multiple(() =>
        {
            Assert.That(registry.TryResolveModule("TestOnlyFrontend", out var module), Is.True);
            Assert.That(module!.CanonicalId, Does.Contain("TestOnlyAttributedFrontendModule"));
            Assert.That(registry.TryResolveOptimizer("TestOnlyOptimizer", out var optimizer), Is.True);
            Assert.That(optimizer!.CanonicalId, Does.Contain("TestOnlyAttributedOptimizer"));
        });
    }

    [Test]
    public void WistDialectRuntimeDescriptorProvider_DeduplicatesDuplicateAssemblies()
    {
        var registry = BuildRegistry(
            new DuplicateTestAssemblyContributor(),
            new TestOnlyRuntimeAssemblyContributor());
        var assemblies = WistDialectRuntimeAssemblyCatalog.Build(
            [new DuplicateTestAssemblyContributor(), new TestOnlyRuntimeAssemblyContributor()]);

        Assert.Multiple(() =>
        {
            Assert.That(assemblies.Select(static x => x.FullName), Is.EqualTo(new[] { TestRuntimeExtensionAssembly.FullName }));
            Assert.That(registry.TryResolveModule("TestOnlyFrontend", out _), Is.True);
            Assert.That(registry.TryResolveOptimizer("TestOnlyOptimizer", out _), Is.True);
            Assert.That(registry.Modules.Keys.Count(static x => x.Contains("TestOnlyAttributedFrontendModule", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(registry.Optimizers.Keys.Count(static x => x.Contains("TestOnlyAttributedOptimizer", StringComparison.Ordinal)), Is.EqualTo(1));
        });
    }

    [Test]
    public void WistDialectRuntimeAssemblyCatalog_OrderDoesNotDependOnContributorRegistrationOrder()
    {
        var first = WistDialectRuntimeAssemblyCatalog.Build([new ZuluAssemblyContributor(), new AlphaAssemblyContributor()]);
        var second = WistDialectRuntimeAssemblyCatalog.Build([new AlphaAssemblyContributor(), new ZuluAssemblyContributor()]);

        Assert.That(
            first.Select(static x => x.FullName),
            Is.EqualTo(second.Select(static x => x.FullName)));
    }

    [Test]
    public void WistDialectRuntimeDescriptorProvider_RejectsInvalidContributorInputClearly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new WistDialectRuntimeDescriptorProvider(Array.Empty<IWistDialectBackendServiceProvider>(), null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("runtimeAssemblyContributors"));
            Assert.That(
                () => new WistDialectRuntimeDescriptorProvider(Array.Empty<IWistDialectBackendServiceProvider>(), new IWistDialectRuntimeAssemblyContributor[] { null! }),
                Throws.ArgumentException.With.Message.Contains("Contributor collection must not contain null entries."));
            Assert.That(
                () => new WistDialectRuntimeDescriptorProvider(Array.Empty<IWistDialectBackendServiceProvider>(), [new NullAssembliesContributor()]),
                Throws.InvalidOperationException.With.Message.Contains("returned null assemblies"));
            Assert.That(
                () => new WistDialectRuntimeDescriptorProvider(Array.Empty<IWistDialectBackendServiceProvider>(), [new NullAssemblyEntryContributor()]),
                Throws.InvalidOperationException.With.Message.Contains("returned a null assembly"));
        });
    }

    private static DialectRuntimeDescriptorRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();
    }

    private static DialectRuntimeDescriptorRegistry BuildRegistry(params IWistDialectRuntimeAssemblyContributor[] contributors)
    {
        var provider = new WistDialectRuntimeDescriptorProvider(Array.Empty<IWistDialectBackendServiceProvider>(), contributors);
        var builder = new DialectRuntimeDescriptorRegistryBuilder();
        provider.Register(builder);
        return builder.Build();
    }

    private static Assembly CreateTestRuntimeExtensionAssembly()
    {
        var assemblyName = new AssemblyName("UniversalToolchain.Dialects.Tests.RuntimeExtension");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("RuntimeExtensionModule");

        CreateAttributedDerivedType(moduleBuilder, "TestOnlyAttributedFrontendModule", typeof(ArithmeticModuleImpl), typeof(DialectModuleAliasAttribute), "TestOnlyFrontend");
        CreateAttributedDerivedType(moduleBuilder, "TestOnlyAttributedOptimizer", typeof(LocalVariablesOptimizer), typeof(DialectOptimizerAliasAttribute), "TestOnlyOptimizer");
        return assemblyBuilder;
    }

    private static void CreateAttributedDerivedType(
        ModuleBuilder moduleBuilder,
        string typeName,
        Type baseType,
        Type attributeType,
        string alias)
    {
        var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, baseType);
        var attributeConstructor = attributeType.GetConstructor([typeof(string[])])!;
        var attribute = new CustomAttributeBuilder(attributeConstructor, new object[] { new[] { alias } });
        typeBuilder.SetCustomAttribute(attribute);
        _ = typeBuilder.CreateType();
    }

    private sealed class TestOnlyRuntimeAssemblyContributor : IWistDialectRuntimeAssemblyContributor
    {
        public int Order => 700;

        public IReadOnlyList<Assembly> GetAssemblies()
        {
            return
            [
                TestRuntimeExtensionAssembly
            ];
        }
    }

    private sealed class DuplicateTestAssemblyContributor : IWistDialectRuntimeAssemblyContributor
    {
        public int Order => 700;

        public IReadOnlyList<Assembly> GetAssemblies()
        {
            return
            [
                TestRuntimeExtensionAssembly,
                TestRuntimeExtensionAssembly
            ];
        }
    }

    private sealed class AlphaAssemblyContributor : IWistDialectRuntimeAssemblyContributor
    {
        public int Order => 900;

        public IReadOnlyList<Assembly> GetAssemblies()
        {
            return
            [
                typeof(WistDialectRuntimeDescriptorProvider).Assembly
            ];
        }
    }

    private sealed class ZuluAssemblyContributor : IWistDialectRuntimeAssemblyContributor
    {
        public int Order => 900;

        public IReadOnlyList<Assembly> GetAssemblies()
        {
            return
            [
                TestRuntimeExtensionAssembly
            ];
        }
    }

    private sealed class NullAssembliesContributor : IWistDialectRuntimeAssemblyContributor
    {
        public int Order => 0;

        public IReadOnlyList<Assembly> GetAssemblies()
        {
            return null!;
        }
    }

    private sealed class NullAssemblyEntryContributor : IWistDialectRuntimeAssemblyContributor
    {
        public int Order => 0;

        public IReadOnlyList<Assembly> GetAssemblies()
        {
            return
            [
                null!
            ];
        }
    }
}
