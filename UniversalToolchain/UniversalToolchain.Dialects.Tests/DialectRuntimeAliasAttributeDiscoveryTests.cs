using BasicCore.Contracts;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class DialectRuntimeAliasAttributeDiscoveryTests
{
    [Test]
    public void AttributeDiscovery_ResolvesModuleOptimizerAndBackendAliases()
    {
        var registry = CreateAttributedRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.TryResolveModule("AttributedModule", out var module), Is.True);
            Assert.That(module!.ImplementationType, Is.EqualTo(typeof(AttributedModule)));
            Assert.That(registry.TryResolveOptimizer("AttributedOptimizer", out var optimizer), Is.True);
            Assert.That(optimizer!.ImplementationType, Is.EqualTo(typeof(AttributedOptimizer)));
            Assert.That(registry.TryResolveBackend(new DialectBackendId("attributed-backend"), out var backendById), Is.True);
            Assert.That(registry.TryResolveBackend(new DialectBackendId("attributed-runtime"), out var backendByAlias), Is.True);
            Assert.That(backendById!.CanonicalId, Is.EqualTo(backendByAlias!.CanonicalId));
            Assert.That(backendByAlias.MetadataOwnerType, Is.EqualTo(typeof(AttributedBackendDeclaration)));
        });
    }

    [Test]
    public void AttributeDiscovery_SupportsMultipleAliasesForTheSameComponent()
    {
        var registry = CreateAttributedRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.TryResolveModule("MultiAliasModule", out var canonicalAlias), Is.True);
            Assert.That(registry.TryResolveModule("MultiAliasModuleLegacy", out var legacyAlias), Is.True);
            Assert.That(canonicalAlias, Is.SameAs(legacyAlias));
            Assert.That(registry.TryResolveOptimizer("MultiAliasOptimizer", out var optimizerPrimary), Is.True);
            Assert.That(registry.TryResolveOptimizer("MultiAliasOptimizerLegacy", out var optimizerLegacy), Is.True);
            Assert.That(optimizerPrimary, Is.SameAs(optimizerLegacy));
        });
    }

    [Test]
    public void WistRuntimeProvider_ResolvesExistingAliasesFromAttributes()
    {
        var registry = BuildRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.TryResolveModule("Arithmetic", out var arithmetic), Is.True);
            Assert.That(arithmetic!.ImplementationType, Is.EqualTo(typeof(ArithmeticModule.Module.ArithmeticModuleImpl)));
            Assert.That(registry.TryResolveOptimizer("LocalVariablesOptimization", out var localVariables), Is.True);
            Assert.That(localVariables!.ImplementationType, Is.EqualTo(typeof(LocalVariablesOptimizerModule.LocalVariablesOptimizer)));
            Assert.That(registry.TryResolveBackend(new DialectBackendId("cil"), out var cil), Is.True);
            Assert.That(registry.TryResolveBackend(new DialectBackendId("compiler"), out var compiler), Is.True);
            Assert.That(cil, Is.SameAs(compiler));
            Assert.That(registry.TryResolveBackend(new DialectBackendId("interpreter"), out var interpreter), Is.True);
            Assert.That(interpreter!.MetadataOwnerType.Name, Is.EqualTo("WistInterpreterBackendDeclaration"));
        });
    }

    [Test]
    public void WistWorkflow_ComposesExistingDialectFileWithAttributeDrivenAliases()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeFile(Path.Combine(GetWistExamplesRoot(), "full-default", "dialect.wistdialect"));

        Assert.That(result.IsSuccess, Is.True, string.Join(Environment.NewLine, result.ResolutionDiagnostics.Select(static x => x.Message)));
        Assert.That(result.RuntimeComposition, Is.Not.Null);

        Assert.Multiple(() =>
        {
            var moduleNames = result.RuntimeComposition!.OrderedModules.Select(static x => x.ImplementationType.Name).ToArray();
            Assert.That(moduleNames, Does.Contain("ArithmeticModuleImpl"));
            Assert.That(moduleNames, Does.Contain("CSharpInteropModuleImpl"));
            Assert.That(moduleNames, Does.Contain("VariablesModuleImpl"));
            Assert.That(Array.IndexOf(moduleNames, "ArithmeticModuleImpl"), Is.LessThan(Array.IndexOf(moduleNames, "VariablesModuleImpl")));
            var backends = result.RuntimeComposition.EnabledBackends.Select(static x => x.CanonicalId).ToArray();
            Assert.That(backends, Has.Length.EqualTo(2));
            Assert.That(backends, Does.Contain("cil"));
            Assert.That(backends, Does.Contain("interpreter"));

            var optimizers = result.RuntimeComposition.EnabledOptimizers.Select(static x => x.ImplementationType.Name).ToArray();
            Assert.That(optimizers, Has.Length.EqualTo(1));
            Assert.That(optimizers[0], Is.EqualTo("LocalVariablesOptimizer"));
        });
    }

    [Test]
    public void AttributeDiscovery_FailsFastForDuplicateAliasesAcrossModulesOptimizersAndBackends()
    {
        var moduleException = Assert.Throws<ArgumentException>(() => new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterAttributedModules(typeof(DuplicateAttributedModule), typeof(DuplicateAttributedModule2)));
        var optimizerException = Assert.Throws<ArgumentException>(() => new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterAttributedOptimizers(typeof(DuplicateAttributedOptimizer), typeof(DuplicateAttributedOptimizer2)));
        var backendException = Assert.Throws<ArgumentException>(() => new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterAttributedBackends(typeof(DuplicateAttributedBackendDeclaration), typeof(DuplicateAttributedBackendDeclaration2)));

        Assert.Multiple(() =>
        {
            Assert.That(moduleException!.Message, Does.Contain("module alias 'DuplicateAttributedModuleAlias'"));
            Assert.That(moduleException.Message, Does.Contain(typeof(DuplicateAttributedModule).FullName!));
            Assert.That(moduleException.Message, Does.Contain(typeof(DuplicateAttributedModule2).FullName!));
            Assert.That(optimizerException!.Message, Does.Contain("optimizer alias 'DuplicateAttributedOptimizerAlias'"));
            Assert.That(optimizerException.Message, Does.Contain(typeof(DuplicateAttributedOptimizer).FullName!));
            Assert.That(optimizerException.Message, Does.Contain(typeof(DuplicateAttributedOptimizer2).FullName!));
            Assert.That(backendException!.Message, Does.Contain("backend alias 'duplicate-attributed-backend'"));
            Assert.That(backendException.Message, Does.Contain(typeof(DuplicateAttributedBackendDeclaration).FullName!));
            Assert.That(backendException.Message, Does.Contain(typeof(DuplicateAttributedBackendDeclaration2).FullName!));
        });
    }

    [Test]
    public void WistWorkflow_ReportsMissingAliasesThroughExistingResolutionDiagnostics()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeText(
            """
            dialect MissingAlias
            use UnknownModule
            backend interpreter
            enable UnknownOptimizer
            """,
            "missing-alias.wistdialect");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ResolutionDiagnostics.Select(static x => x.Code), Does.Contain("R001"));
            Assert.That(result.ResolutionDiagnostics.Select(static x => x.Code), Does.Contain("R003"));
            Assert.That(result.ResolutionDiagnostics.Select(static x => x.Message), Does.Contain("Runtime module descriptor 'UnknownModule' was not registered."));
            Assert.That(result.ResolutionDiagnostics.Select(static x => x.Message), Does.Contain("Runtime optimizer descriptor 'UnknownOptimizer' was not registered."));
        });
    }

    [Test]
    public void AttributeDiscovery_IsDeterministicAcrossAssemblyOrderings()
    {
        var first = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterAttributedModules(typeof(MultiAliasAttributedModule), typeof(AttributedModule))
            .RegisterAttributedOptimizers(typeof(MultiAliasAttributedOptimizer), typeof(AttributedOptimizer))
            .RegisterAttributedBackends(typeof(AttributedBackendDeclaration))
            .Build();

        var second = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterAttributedModules(typeof(AttributedModule), typeof(MultiAliasAttributedModule))
            .RegisterAttributedOptimizers(typeof(AttributedOptimizer), typeof(MultiAliasAttributedOptimizer))
            .RegisterAttributedBackends(typeof(AttributedBackendDeclaration))
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(first.Modules.Keys.OrderBy(static x => x), Is.EqualTo(second.Modules.Keys.OrderBy(static x => x)));
            Assert.That(first.Optimizers.Keys.OrderBy(static x => x), Is.EqualTo(second.Optimizers.Keys.OrderBy(static x => x)));
            Assert.That(first.Backends.Keys.OrderBy(static x => x), Is.EqualTo(second.Backends.Keys.OrderBy(static x => x)));
            Assert.That(first.TryResolveModule("MultiAliasModule", out var firstModule), Is.True);
            Assert.That(second.TryResolveModule("MultiAliasModule", out var secondModule), Is.True);
            Assert.That(firstModule!.ImplementationType, Is.EqualTo(secondModule!.ImplementationType));

            Assert.That(first.TryResolveOptimizer("MultiAliasOptimizer", out var firstOptimizer), Is.True);
            Assert.That(second.TryResolveOptimizer("MultiAliasOptimizer", out var secondOptimizer), Is.True);
            Assert.That(firstOptimizer!.ImplementationType, Is.EqualTo(secondOptimizer!.ImplementationType));

            Assert.That(first.TryResolveBackend(new DialectBackendId("attributed-runtime"), out var firstBackend), Is.True);
            Assert.That(second.TryResolveBackend(new DialectBackendId("attributed-runtime"), out var secondBackend), Is.True);
            Assert.That(firstBackend!.CanonicalId, Is.EqualTo(secondBackend!.CanonicalId));
        });
    }

    private static DialectRuntimeDescriptorRegistry CreateAttributedRegistry()
    {
        return new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterAttributedModules(typeof(AttributedModule), typeof(MultiAliasAttributedModule))
            .RegisterAttributedOptimizers(typeof(AttributedOptimizer), typeof(MultiAliasAttributedOptimizer))
            .RegisterAttributedBackends(typeof(AttributedBackendDeclaration))
            .Build();
    }

    private static DialectRuntimeDescriptorRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();
    }

    private static string GetWistExamplesRoot()
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist"));
    }

    [DialectModuleAlias("AttributedModule")]
    private sealed class AttributedModule : IFrontendCoreModule
    {
    }

    [DialectModuleAlias("MultiAliasModule", "MultiAliasModuleLegacy")]
    private sealed class MultiAliasAttributedModule : IFrontendCoreModule
    {
    }

    [DialectModuleAlias("DuplicateAttributedModuleAlias")]
    private sealed class DuplicateAttributedModule : IFrontendCoreModule
    {
    }

    [DialectModuleAlias("DuplicateAttributedModuleAlias")]
    private sealed class DuplicateAttributedModule2 : IFrontendCoreModule
    {
    }

    [DialectOptimizerAlias("AttributedOptimizer")]
    private sealed class AttributedOptimizer : IIRProcessingModule
    {
    }

    [DialectOptimizerAlias("MultiAliasOptimizer", "MultiAliasOptimizerLegacy")]
    private sealed class MultiAliasAttributedOptimizer : IIRProcessingModule
    {
    }

    [DialectOptimizerAlias("DuplicateAttributedOptimizerAlias")]
    private sealed class DuplicateAttributedOptimizer : IIRProcessingModule
    {
    }

    [DialectOptimizerAlias("DuplicateAttributedOptimizerAlias")]
    private sealed class DuplicateAttributedOptimizer2 : IIRProcessingModule
    {
    }

    [DialectBackendAlias("attributed-runtime")]
    private sealed class AttributedBackendDeclaration : DialectBackendDeclaration
    {
        public override DialectBackendId BackendId => new("attributed-backend");
    }

    [DialectBackendAlias("duplicate-attributed-backend")]
    private sealed class DuplicateAttributedBackendDeclaration : DialectBackendDeclaration
    {
        public override DialectBackendId BackendId => new("duplicate-attributed-backend-a");
    }

    [DialectBackendAlias("duplicate-attributed-backend")]
    private sealed class DuplicateAttributedBackendDeclaration2 : DialectBackendDeclaration
    {
        public override DialectBackendId BackendId => new("duplicate-attributed-backend-b");
    }
}
