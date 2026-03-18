using BasicCore.Contracts;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests;

public class FrameworkNativeDslDemoWorkflowTests
{
    [Test]
    public void Demo_EndToEndValidSource_Succeeds()
    {
        var workflow = CreateWorkflow();
        var source =
            """
            dialect Demo
            use Arithmetic,Variables
            before Arithmetic,Variables
            backend interpreter
            allow add_i32
            enable LocalVariablesOptimization
            security trusted
            capability sandbox
            """;

        var report = workflow.RunSource(source, CreateRegistry(), "demo.dialect");

        Assert.Multiple(() =>
        {
            Assert.That(report.IsSuccess, Is.True);
            Assert.That(report.CompositionResult, Is.Not.Null);
            Assert.That(report.CompositionResult!.BuildPlan, Is.Not.Null);
            Assert.That(report.CompositionResult.RuntimeComposition, Is.Not.Null);
        });
    }

    [Test]
    public void Demo_InvalidSyntaxSource_ReturnsCompilationError()
    {
        var workflow = CreateWorkflow();
        var report = workflow.RunSource("dialect Demo\nuse A,\n", CreateRegistry(), "broken.dialect");

        Assert.Multiple(() =>
        {
            Assert.That(report.IsSuccess, Is.False);
            Assert.That(report.CompilationError, Does.Contain("trailing comma"));
            Assert.That(report.CompositionResult, Is.Null);
        });
    }

    [Test]
    public void Demo_DeterministicOutput_IsStable()
    {
        var workflow = CreateWorkflow();
        var registry = CreateRegistry();
        const string source = "dialect Demo\nuse Arithmetic,Variables\nbefore Arithmetic,Variables\nbackend interpreter\nallow add_i32\nenable LocalVariablesOptimization\nsecurity trusted\ncapability sandbox\n";

        var first = workflow.RunSource(source, registry, "demo.dialect").ToDeterministicText();
        var second = workflow.RunSource(source, registry, "demo.dialect").ToDeterministicText();

        Assert.That(first, Is.EqualTo(second));
    }

    private static DialectFrameworkDemoWorkflow CreateWorkflow()
    {
        return new DialectFrameworkDemoWorkflow(
            new DialectFrameworkCompositionWorkflow(
                new DialectDslCompiler(),
                new DialectCompiledDialectBuildPlanBuilder(),
                new DialectRuntimeCompositionResolver()));
    }

    private static DialectRuntimeDescriptorRegistry CreateRegistry()
    {
        return new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("Arithmetic", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("Variables", typeof(FakeFrontendModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "InterpreterBackend"))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("LocalVariablesOptimization", typeof(FakeOptimizerModule)))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("add_i32", DialectBackendTarget.Any))
            .Build();
    }

    private sealed class FakeFrontendModule : IFrontendCoreModule
    {
    }

    private sealed class FakeOptimizerModule : IIRProcessingModule
    {
    }
}
