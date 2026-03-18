using CommonExceptions;
using BasicCore.Contracts;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests;

public class FrameworkNativeRuntimeCompositionWorkflowTests
{
    [Test]
    public void ComposeText_ValidSource_ProducesDeterministicRuntimeComposition()
    {
        var workflow = CreateWorkflow();
        var registry = CreateRegistry();
        var source =
            """
            dialect D
            use A,B
            before A,B
            backend interpreter
            allow intrinsic_a
            enable opt_a
            """;

        var first = workflow.ComposeText(source, registry, "first");
        var second = workflow.ComposeText(source, registry, "second");

        Assert.Multiple(() =>
        {
            Assert.That(first.IsSuccess, Is.True);
            Assert.That(first.RuntimeComposition, Is.Not.Null);
            Assert.That(first.RuntimeComposition!.OrderedModules.Select(x => x.Name), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(first.RuntimeComposition.EnabledBackends.Select(x => x.RuntimeName), Is.EqualTo(new[] { "InterpreterBackend" }));
            Assert.That(first.RuntimeComposition.EnabledOptimizers.Select(x => x.Name), Is.EqualTo(new[] { "opt_a" }));
            Assert.That(first.RuntimeComposition.AllowedIntrinsics.Select(x => x.Name), Is.EqualTo(new[] { "intrinsic_a" }));
            Assert.That(first.RuntimeComposition.OrderedModules.Select(x => x.Name), Is.EqualTo(second.RuntimeComposition!.OrderedModules.Select(x => x.Name)));
        });
    }

    [Test]
    public void ComposeText_MissingRuntimeDescriptors_ReturnsResolutionDiagnostics()
    {
        var workflow = CreateWorkflow();
        var registry = new DialectRuntimeDescriptorRegistryBuilder().Build();
        var source =
            """
            dialect D
            use A
            backend interpreter
            allow intrinsic_a
            enable opt_a
            """;

        var result = workflow.ComposeText(source, registry);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.SemanticDiagnostics, Is.Empty);
            Assert.That(result.ResolutionDiagnostics.Any(x => x.Code == "R001"), Is.True);
            Assert.That(result.ResolutionDiagnostics.Any(x => x.Code == "R002"), Is.True);
            Assert.That(result.ResolutionDiagnostics.Any(x => x.Code == "R003"), Is.True);
            Assert.That(result.ResolutionDiagnostics.Any(x => x.Code == "R004"), Is.True);
        });
    }

    [Test]
    public void ComposeText_SemanticConflict_FailsDuringDslValidation()
    {
        var workflow = CreateWorkflow();
        var source =
            """
            dialect D
            use A
            exclude A
            """;

        var ex = Assert.Throws<ParserException>(() => workflow.ComposeText(source, CreateRegistry()));

        Assert.That(ex!.Message, Does.Contain("both use and exclude"));
    }

    private static DialectFrameworkCompositionWorkflow CreateWorkflow()
    {
        return new DialectFrameworkCompositionWorkflow(
            new DialectDslCompiler(),
            new DialectCompiledDialectBuildPlanBuilder(),
            new DialectRuntimeCompositionResolver());
    }

    private static DialectRuntimeDescriptorRegistry CreateRegistry()
    {
        return new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("B", typeof(FakeFrontendModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "InterpreterBackend"))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("opt_a", typeof(FakeOptimizerModule)))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("intrinsic_a", DialectBackendTarget.Any))
            .Build();
    }

    private sealed class FakeFrontendModule : IFrontendCoreModule
    {
    }

    private sealed class FakeOptimizerModule : IIRProcessingModule
    {
    }
}
