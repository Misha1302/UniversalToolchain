using BasicCore.Contracts;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Tests;

public class DialectSubsystemTargetedCoverageTests
{
    [Test]
    public void Parsing_ValidDirectives_ParsesCompleteDialectSurface()
    {
        var parser = new DialectDefinitionParser();
        var result = parser.Parse(
            """
            dialect Coverage
            use A
            exclude Z
            requires A -> B
            backend interpreter enable
            allow intrinsic "i_add" for any
            enable optimizer O1 for interpreter
            security trusted
            capability sandbox = true
            """);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(result.Document!.UseModules, Is.EqualTo(new[] { "A" }));
            Assert.That(result.Document.BackendDirectives.Select(x => (x.Backend, x.Enabled)),
                Is.EqualTo(new[] { (DialectBackendTarget.Interpreter, true) }));
            Assert.That(result.Document.IntrinsicDirectives.Select(x => (x.Name, x.Target)),
                Is.EqualTo(new[] { ("i_add", DialectBackendTarget.Any) }));
            Assert.That(result.Document.OptimizerDirectives.Select(x => (x.Name, x.Target)),
                Is.EqualTo(new[] { ("O1", DialectBackendTarget.Interpreter) }));
        });
    }

    [Test]
    public void Parsing_InvalidDirective_ReportsMeaningfulDiagnostic()
    {
        var parser = new DialectDefinitionParser();
        var result = parser.Parse("dialect A\nwat unknown\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(x => x.Code == "P107"), Is.True);
            Assert.That(result.Diagnostics.Any(x => x.Message.Contains("Unknown directive", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void RuntimeComposition_BackendScopedDirectives_IgnoreDisabledBackends_Regression()
    {
        var plan = new DialectBuildPlan(
            "dialect",
            "1",
            ["A"],
            [DialectBackendTarget.Interpreter],
            [],
            [new IntrinsicBuildDirective("intr-cil", true, DialectBackendTarget.Cil)],
            [new OptimizerBuildDirective("opt-cil", true, DialectBackendTarget.Cil)],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "InterpreterBackend"))
            .Build();

        var composition = new DialectRuntimeCompositionResolver().Resolve(plan, registry);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsResolved, Is.True);
            Assert.That(composition.EnabledOptimizers, Is.Empty);
            Assert.That(composition.AllowedIntrinsics, Is.Empty);
            Assert.That(composition.Diagnostics.Diagnostics.Any(x => x.Code == "R003"), Is.False);
            Assert.That(composition.Diagnostics.Diagnostics.Any(x => x.Code == "R004"), Is.False);
        });
    }

    [Test]
    public void RuntimeComposition_IntrinsicSpecificTarget_TakesPrecedenceOverAny()
    {
        var plan = new DialectBuildPlan(
            "dialect",
            "1",
            ["A"],
            [DialectBackendTarget.Cil],
            [],
            [new IntrinsicBuildDirective("intrinsic-a", true, DialectBackendTarget.Cil)],
            [],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Cil, "CilBackend"))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("intrinsic-a", DialectBackendTarget.Any))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("intrinsic-a", DialectBackendTarget.Cil))
            .Build();

        var composition = new DialectRuntimeCompositionResolver().Resolve(plan, registry);

        Assert.That(composition.AllowedIntrinsics.Single().Target, Is.EqualTo(DialectBackendTarget.Cil));
    }

    [Test]
    public void RuntimeComposition_IntrinsicAnyFallback_IsUsedWhenSpecificMissing()
    {
        var plan = new DialectBuildPlan(
            "dialect",
            "1",
            ["A"],
            [DialectBackendTarget.Cil],
            [],
            [new IntrinsicBuildDirective("intrinsic-a", true, DialectBackendTarget.Cil)],
            [],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Cil, "CilBackend"))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("intrinsic-a", DialectBackendTarget.Any))
            .Build();

        var composition = new DialectRuntimeCompositionResolver().Resolve(plan, registry);

        Assert.That(composition.AllowedIntrinsics.Single().Target, Is.EqualTo(DialectBackendTarget.Any));
    }

    [Test]
    public void BuildPlan_Normalization_IsDeterministicAcrossRuns()
    {
        const string source =
            """
            dialect D
            use B
            use A
            backend interpreter enable
            backend cil disable
            allow intrinsic "i1" for any
            enable optimizer O1 for interpreter
            """;

        var parser = new DialectDefinitionParser();
        var builder = new DialectBuildPlanBuilder();

        var first = builder.Build(parser.Parse(source).Document!);
        var second = builder.Build(parser.Parse(source).Document!);

        Assert.Multiple(() =>
        {
            Assert.That(first.OrderedModules, Is.EqualTo(second.OrderedModules));
            Assert.That(first.EnabledBackends, Is.EqualTo(second.EnabledBackends));
            Assert.That(first.DisabledBackends, Is.EqualTo(second.DisabledBackends));
            Assert.That(first.IntrinsicDirectives.Select(x => (x.Name, x.Target)),
                Is.EqualTo(second.IntrinsicDirectives.Select(x => (x.Name, x.Target))));
        });
    }

    [Test]
    public void RuntimeComposition_ResolvesRegisteredModulesBackendsAndIntrinsics()
    {
        var source =
            """
            dialect D
            use A
            backend interpreter enable
            allow intrinsic "intrinsic-a" for any
            enable optimizer opt-a for any
            """;

        var workflow = new DialectFrameworkCompositionWorkflow(
            new DialectDslCompiler(),
            new DialectCompiledDialectBuildPlanBuilder(),
            new DialectRuntimeCompositionResolver());

        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "InterpreterBackend"))
            .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("intrinsic-a", DialectBackendTarget.Any))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor("opt-a", typeof(FakeOptimizerModule)))
            .Build();

        var result = workflow.ComposeText(source, registry);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.RuntimeComposition, Is.Not.Null);
            Assert.That(result.RuntimeComposition!.EnabledBackends.Select(x => x.RuntimeName), Is.EqualTo(new[] { "InterpreterBackend" }));
            Assert.That(result.RuntimeComposition.EnabledOptimizers.Select(x => x.Name), Is.EqualTo(new[] { "opt-a" }));
            Assert.That(result.RuntimeComposition.AllowedIntrinsics.Select(x => x.Name), Is.EqualTo(new[] { "intrinsic-a" }));
        });
    }

    [Test]
    public void PolicySecurityCapability_RestrictedUnsafeInterop_ProducesS006()
    {
        var document = new DialectSyntaxDocument(
            "dialect",
            null,
            ["A"],
            [],
            [],
            [],
            [],
            [],
            SecurityProfile.Restricted,
            [new KeyValuePair<string, bool>("unsafe-interop", true)]);

        var plan = new DialectBuildPlanBuilder().Build(document);

        Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S006"), Is.True);
    }

    [Test]
    public void Regression_FrameworkAndParserBuildPlans_RemainSemanticallyEquivalent()
    {
        const string source =
            """
            dialect D
            use A
            use B
            before A -> B
            backend interpreter enable
            backend cil disable
            allow intrinsic "i1" for any
            enable optimizer O1 for interpreter
            security trusted
            capability cap = true
            """;

        var frameworkPlan = new DialectCompiledDialectBuildPlanBuilder().Build(new DialectDslCompiler().Compile(source));
        var parserPlan = new DialectBuildPlanBuilder().Build(new DialectDefinitionParser().Parse(source).Document!);

        Assert.Multiple(() =>
        {
            Assert.That(frameworkPlan.OrderedModules, Is.EqualTo(parserPlan.OrderedModules));
            Assert.That(frameworkPlan.EnabledBackends, Is.EqualTo(parserPlan.EnabledBackends));
            Assert.That(frameworkPlan.DisabledBackends, Is.EqualTo(parserPlan.DisabledBackends));
            Assert.That(frameworkPlan.IntrinsicDirectives.Select(x => (x.Name, x.Target)), Is.EqualTo(parserPlan.IntrinsicDirectives.Select(x => (x.Name, x.Target))));
            Assert.That(frameworkPlan.OptimizerDirectives.Select(x => (x.Name, x.Target)), Is.EqualTo(parserPlan.OptimizerDirectives.Select(x => (x.Name, x.Target))));
        });
    }

    private sealed class FakeFrontendModule : IFrontendCoreModule
    {
    }

    private sealed class FakeOptimizerModule : IIRProcessingModule
    {
    }
}
