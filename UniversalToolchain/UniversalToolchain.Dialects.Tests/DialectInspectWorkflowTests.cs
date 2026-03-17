using ATarget = UniversalToolchain.Dialects.Abstractions.DialectBackendTarget;
using BasicCore.Contracts;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Parsing;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Tests;

public class DialectInspectWorkflowTests
{
    [Test]
    public void InspectFile_ValidFile_EndToEndSucceeds()
    {
        var workflow = CreateWorkflow();
        var file = ResolveExampleFile("minimal.dialect");
        var registry = CreateRegistry();

        var result = workflow.InspectFile(file, registry);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.BuildPlan, Is.Not.Null);
            Assert.That(result.RuntimeComposition, Is.Not.Null);
            Assert.That(result.ParseDiagnostics, Is.Empty);
            Assert.That(result.SemanticDiagnostics, Is.Empty);
            Assert.That(result.ResolutionDiagnostics, Is.Empty);
        });
    }

    [Test]
    public void InspectFile_InvalidSyntaxFile_ReturnsParseDiagnostics()
    {
        var workflow = CreateWorkflow();
        var file = CreateTempDialectFile("dialect A\nrequires A - B\n");

        var result = workflow.InspectFile(file, CreateRegistry());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ParseDiagnostics, Is.Not.Empty);
            Assert.That(result.SemanticDiagnostics, Is.Empty);
            Assert.That(result.ResolutionDiagnostics, Is.Empty);
        });
    }

    [Test]
    public void InspectFile_UnresolvedModuleName_ReturnsResolutionDiagnostics()
    {
        var workflow = CreateWorkflow();
        var file = CreateTempDialectFile("""
                                         dialect X
                                         use MissingModule
                                         backend interpreter enable
                                         security trusted
                                         """);

        var result = workflow.InspectFile(file, CreateRegistry());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ParseDiagnostics, Is.Empty);
            Assert.That(result.SemanticDiagnostics, Is.Empty);
            Assert.That(result.ResolutionDiagnostics.Any(x => x.Code == "R001"), Is.True);
        });
    }

    [Test]
    public void InspectFile_ConflictingRules_ReturnsSemanticDiagnostics()
    {
        var workflow = CreateWorkflow();
        var file = CreateTempDialectFile("""
                                         dialect X
                                         use Arithmetic
                                         use Variables
                                         before Arithmetic -> Variables
                                         before Variables -> Arithmetic
                                         backend interpreter enable
                                         security trusted
                                         """);

        var result = workflow.InspectFile(file, CreateRegistry());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ParseDiagnostics, Is.Empty);
            Assert.That(result.SemanticDiagnostics.Any(x => x.Code == "S007"), Is.True);
            Assert.That(result.ResolutionDiagnostics, Is.Empty);
        });
    }


    [Test]
    public void InspectResult_DeterministicText_PreservesModuleOrderFromBuildPlan()
    {
        var plan = new DialectBuildPlan(
            "dialect",
            "1",
            ["Z", "A"],
            ["interpreter"],
            [],
            [],
            [],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var result = new DialectInspectResult("inline", plan, null, [], [], []);
        var text = result.ToDeterministicText();

        Assert.That(text, Does.Contain("Ordered modules: Z, A"));
    }

    [Test]
    public void InspectFile_DeterministicPrintedOutput_IsStable()
    {
        var workflow = CreateWorkflow();
        var file = ResolveExampleFile("full.dialect");
        var registry = CreateRegistry();

        var first = workflow.InspectFile(file, registry).ToDeterministicText();
        var second = workflow.InspectFile(file, registry).ToDeterministicText();

        Assert.That(first, Is.EqualTo(second));
    }

    private static DialectInspectWorkflow CreateWorkflow()
    {
        return new DialectInspectWorkflow(
            new DialectDefinitionParser(),
            new DialectBuildPlanBuilder(),
            new DialectRuntimeCompositionResolver());
    }

    private static DialectRuntimeDescriptorRegistry CreateRegistry()
    {
        return new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("Arithmetic", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("Variables", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("Scopes", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("Conditions", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("Labels", typeof(FakeFrontendModule)))
            .RegisterModule(new RuntimeModuleDescriptor("Loops", typeof(FakeFrontendModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(ATarget.Interpreter, "InterpreterBackend"))
            .RegisterBackend(new RuntimeBackendDescriptor(ATarget.Cil, "CilBackend"))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("LocalVariablesOptimization", typeof(FakeOptimizerModule)))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("add_i32", ATarget.Any))
            .Build();
    }

    private static string ResolveExampleFile(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", fileName));
        if (!File.Exists(path))
            Thrower.FileNotFound(path);

        return path;
    }

    private static string CreateTempDialectFile(string text)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dialect-{Guid.NewGuid():N}.dialect");
        File.WriteAllText(path, text);
        return path;
    }

    private sealed class FakeFrontendModule : IFrontendCoreModule
    {
    }

    private sealed class FakeOptimizerModule : IIRProcessingModule
    {
    }
}
