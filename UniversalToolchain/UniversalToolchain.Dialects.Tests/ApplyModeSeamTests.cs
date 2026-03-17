using BasicCore.Contracts;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Integration;
using ATarget = UniversalToolchain.Dialects.Abstractions.DialectBackendTarget;

namespace UniversalToolchain.Dialects.Tests;

public class ApplyModeSeamTests
{
    [Test]
    public void Build_ResolvedPlan_ProducesApplyDescription()
    {
        var plan = new DialectBuildPlan(
            "FrameworkNative",
            "v1",
            ["Arithmetic", "Variables"],
            [DialectBackendTarget.Interpreter],
            [],
            [new IntrinsicBuildDirective("add_i32", true, ATarget.Any)],
            [new OptimizerBuildDirective("LocalVariablesOptimization", true, ATarget.Any)],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var resolver = new DialectRuntimeCompositionResolver();
        var composition = resolver.Resolve(plan, CreateRegistry());
        var builder = new DialectApplyDescriptionBuilder();

        var applyDescription = builder.Build(composition);

        Assert.Multiple(() =>
        {
            Assert.That(applyDescription.DialectName, Is.EqualTo("FrameworkNative"));
            Assert.That(applyDescription.FrontendModules.Select(x => x.Name), Is.EqualTo(new[] { nameof(FakeFrontendModule), nameof(FakeFrontendModule2) }));
            Assert.That(applyDescription.IrProcessingModules, Is.Empty);
            Assert.That(applyDescription.Optimizers.Select(x => x.Name), Is.EqualTo(new[] { nameof(FakeOptimizerModule) }));
            Assert.That(applyDescription.RuntimeBackends, Is.EqualTo(new[] { "InterpreterBackend" }));
            Assert.That(applyDescription.Intrinsics.Select(x => $"{x.Name}@{DialectBackendTargetText.ToText(x.Target)}"), Is.EqualTo(new[] { "add_i32@any" }));
        });
    }

    [Test]
    public void Build_UnresolvedComposition_ThrowsArgumentException()
    {
        var plan = new DialectBuildPlan(
            "FrameworkNative",
            "v1",
            ["MissingModule"],
            [DialectBackendTarget.Interpreter],
            [],
            [],
            [],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var resolver = new DialectRuntimeCompositionResolver();
        var composition = resolver.Resolve(plan, CreateRegistry());
        var builder = new DialectApplyDescriptionBuilder();

        var ex = Assert.Throws<ArgumentException>(() => builder.Build(composition));

        Assert.That(ex!.Message, Does.Contain("unresolved runtime composition"));
    }

    [Test]
    public void Build_DeterministicDescriptionShape_IsStable()
    {
        var plan = new DialectBuildPlan(
            "FrameworkNative",
            "v1",
            ["Arithmetic", "Variables"],
            [DialectBackendTarget.Interpreter, DialectBackendTarget.Cil],
            [],
            [
                new IntrinsicBuildDirective("add_i32", true, ATarget.Any),
                new IntrinsicBuildDirective("sub_i32", true, ATarget.Any)
            ],
            [
                new OptimizerBuildDirective("AlgebraicSimplification", true, ATarget.Any),
                new OptimizerBuildDirective("LocalVariablesOptimization", true, ATarget.Any)
            ],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var resolver = new DialectRuntimeCompositionResolver();
        var composition = resolver.Resolve(plan, CreateRegistry());
        var builder = new DialectApplyDescriptionBuilder();

        var first = builder.Build(composition).ToDeterministicText();
        var second = builder.Build(composition).ToDeterministicText();

        Assert.That(first, Is.EqualTo(second));
    }

    private static DialectRuntimeDescriptorRegistry CreateRegistry()
    {
        return new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("Arithmetic", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("Variables", typeof(FakeFrontendModule2)))
            .RegisterBackend(new RuntimeBackendDescriptor(ATarget.Interpreter, "InterpreterBackend"))
            .RegisterBackend(new RuntimeBackendDescriptor(ATarget.Cil, "CilBackend"))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("LocalVariablesOptimization", typeof(FakeOptimizerModule)))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("AlgebraicSimplification", typeof(FakeOptimizerModule2)))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("add_i32", ATarget.Any))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("sub_i32", ATarget.Any))
            .Build();
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

    private sealed class FakeOptimizerModule2 : IIRProcessingModule
    {
    }
}
