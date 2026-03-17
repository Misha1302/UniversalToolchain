using BasicCore.Contracts;
using ExceptionsManager;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;
using ATarget = UniversalToolchain.Dialects.Abstractions.DialectBackendTarget;

namespace UniversalToolchain.Dialects.Tests;

public class FrameworkNativeDslDemoWorkflowTests
{
    [Test]
    public void Demo_EndToEndValidDemo_Succeeds()
    {
        var demoWorkflow = CreateWorkflow();
        var source = File.ReadAllText(ResolveExampleFile("framework-native-demo.dialect"));

        var report = demoWorkflow.RunSource(source, CreateRegistry(), "framework-native-demo.dialect");

        Assert.Multiple(() =>
        {
            Assert.That(report.IsSuccess, Is.True);
            Assert.That(report.CompositionResult, Is.Not.Null);
            Assert.That(report.CompositionResult!.BuildPlan, Is.Not.Null);
            Assert.That(report.CompositionResult.RuntimeComposition, Is.Not.Null);
        });
    }

    [Test]
    public void Demo_InvalidSyntaxDemo_ReturnsCompilationError()
    {
        var demoWorkflow = CreateWorkflow();

        var report = demoWorkflow.RunScenario(DialectFrameworkDemoScenario.InvalidSyntax, CreateRegistry());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsSuccess, Is.False);
            Assert.That(report.CompilationError, Is.Not.Empty);
            Assert.That(report.CompositionResult, Is.Null);
        });
    }

    [Test]
    public void Demo_SemanticConflictDemo_ReturnsSemanticDiagnostics()
    {
        var demoWorkflow = CreateWorkflow();

        var report = demoWorkflow.RunScenario(DialectFrameworkDemoScenario.SemanticConflict, CreateRegistry());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsSuccess, Is.False);
            Assert.That(report.CompositionResult, Is.Not.Null);
            Assert.That(report.CompositionResult!.SemanticDiagnostics.Any(x => x.Code == "S101"), Is.True);
        });
    }

    [Test]
    public void Demo_UnresolvedModuleDemo_ReturnsResolutionDiagnostics()
    {
        var demoWorkflow = CreateWorkflow();

        var report = demoWorkflow.RunScenario(DialectFrameworkDemoScenario.UnresolvedModule, CreateRegistry());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsSuccess, Is.False);
            Assert.That(report.CompositionResult, Is.Not.Null);
            Assert.That(report.CompositionResult!.ResolutionDiagnostics.Any(x => x.Code == "R001"), Is.True);
        });
    }

    [Test]
    public void Demo_DeterministicOutputDemo_IsStable()
    {
        var demoWorkflow = CreateWorkflow();
        var registry = CreateRegistry();

        var first = demoWorkflow.RunScenario(DialectFrameworkDemoScenario.Valid, registry).ToDeterministicText();
        var second = demoWorkflow.RunScenario(DialectFrameworkDemoScenario.Valid, registry).ToDeterministicText();

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
            .RegisterBackend(new RuntimeBackendDescriptor(ATarget.Interpreter, "InterpreterBackend"))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("LocalVariablesOptimization", typeof(FakeOptimizerModule)))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("add_i32", ATarget.Any))
            .Build();
    }

    private static string ResolveExampleFile(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", fileName));
        if (!File.Exists(path))
        {
            Thrower.FileNotFound(path);
        }

        return path;
    }

    private sealed class FakeFrontendModule : IFrontendCoreModule
    {
    }

    private sealed class FakeOptimizerModule : IIRProcessingModule
    {
    }
}
