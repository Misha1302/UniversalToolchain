using BasicCore.Contracts;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests;

public class RuntimeCompositionResolverTests
{
    [Test]
    public void Resolve_DeterministicResolution_ReturnsStableComposition()
    {
        var plan = CreateValidPlan();
        var registry = CreateRegistryBuilder().Build();
        var resolver = new DialectRuntimeCompositionResolver();

        var first = resolver.Resolve(plan, registry);
        var second = resolver.Resolve(plan, registry);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsResolved, Is.True);
            Assert.That(second.IsResolved, Is.True);
            Assert.That(first.OrderedModules.Select(x => x.Name), Is.EqualTo(second.OrderedModules.Select(x => x.Name)));
            Assert.That(first.EnabledBackends.Select(x => x.RuntimeName), Is.EqualTo(second.EnabledBackends.Select(x => x.RuntimeName)));
            Assert.That(first.EnabledOptimizers.Select(x => x.Name), Is.EqualTo(second.EnabledOptimizers.Select(x => x.Name)));
            Assert.That(first.AllowedIntrinsics.Select(x => x.Name), Is.EqualTo(second.AllowedIntrinsics.Select(x => x.Name)));
        });
    }

    [Test]
    public void Resolve_MissingModule_AddsDiagnostic()
    {
        var plan = CreateValidPlan();
        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "InterpreterBackend"))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("intrinsic-a", DialectBackendTarget.Any))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("opt-a", typeof(FakeOptimizerModule)))
            .Build();

        var resolver = new DialectRuntimeCompositionResolver();
        var composition = resolver.Resolve(plan, registry);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsResolved, Is.False);
            Assert.That(composition.Diagnostics.Diagnostics.Any(x => x.Code == "R001"), Is.True);
        });
    }

    [Test]
    public void Resolve_MissingBackend_AddsDiagnostic()
    {
        var plan = CreateValidPlan();
        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("B", typeof(FakeIrModule)))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("intrinsic-a", DialectBackendTarget.Any))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("opt-a", typeof(FakeOptimizerModule)))
            .Build();

        var resolver = new DialectRuntimeCompositionResolver();
        var composition = resolver.Resolve(plan, registry);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsResolved, Is.False);
            Assert.That(composition.Diagnostics.Diagnostics.Any(x => x.Code == "R002"), Is.True);
        });
    }

    [Test]
    public void Resolve_MissingIntrinsic_AddsDiagnostic()
    {
        var plan = CreateValidPlan();
        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("B", typeof(FakeIrModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "InterpreterBackend"))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("opt-a", typeof(FakeOptimizerModule)))
            .Build();

        var resolver = new DialectRuntimeCompositionResolver();
        var composition = resolver.Resolve(plan, registry);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsResolved, Is.False);
            Assert.That(composition.Diagnostics.Diagnostics.Any(x => x.Code == "R004"), Is.True);
        });
    }


    [Test]
    public void Resolve_UnsupportedBackendToken_AddsDiagnostic()
    {
        var plan = new DialectBuildPlan(
            "dialect",
            "1",
            ["A"],
            ["wasm"],
            [],
            [],
            [],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .Build();

        var resolver = new DialectRuntimeCompositionResolver();
        var composition = resolver.Resolve(plan, registry);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsResolved, Is.False);
            Assert.That(composition.Diagnostics.Diagnostics.Any(x => x.Code == "R005"), Is.True);
        });
    }

    [Test]
    public void Resolve_DualBackendModelCompatibility_ResolvesInterpreterAndCil()
    {
        var plan = new DialectBuildPlan(
            "dialect",
            "1",
            ["A"],
            ["cil", "interpreter"],
            [],
            [new IntrinsicBuildDirective("intrinsic-a", true, DialectBackendTarget.Any)],
            [new OptimizerBuildDirective("opt-a", true, DialectBackendTarget.Any)],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "InterpreterBackend"))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Cil, "CilBackend"))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("intrinsic-a", DialectBackendTarget.Any))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("opt-a", typeof(FakeOptimizerModule)))
            .Build();

        var resolver = new DialectRuntimeCompositionResolver();
        var composition = resolver.Resolve(plan, registry);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsResolved, Is.True);
            Assert.That(composition.EnabledBackends.Select(x => x.RuntimeName), Is.EqualTo(new[] { "CilBackend", "InterpreterBackend" }));
        });
    }

    private static DialectBuildPlan CreateValidPlan()
    {
        return new DialectBuildPlan(
            "dialect",
            "1",
            ["A", "B"],
            ["interpreter"],
            [],
            [new IntrinsicBuildDirective("intrinsic-a", true, DialectBackendTarget.Any)],
            [new OptimizerBuildDirective("opt-a", true, DialectBackendTarget.Any)],
            SecurityProfile.Trusted,
            [new KeyValuePair<string, bool>("c", true)],
            new DialectValidationResult());
    }

    private static DialectRuntimeDescriptorRegistryBuilder CreateRegistryBuilder()
    {
        return new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("B", typeof(FakeIrModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "InterpreterBackend"))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("intrinsic-a", DialectBackendTarget.Any))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("opt-a", typeof(FakeOptimizerModule)));
    }

    private sealed class FakeFrontendModule : IFrontendCoreModule
    {
    }

    private sealed class FakeIrModule : IIRProcessingModule
    {
    }

    private sealed class FakeOptimizerModule : IIRProcessingModule
    {
    }
}
