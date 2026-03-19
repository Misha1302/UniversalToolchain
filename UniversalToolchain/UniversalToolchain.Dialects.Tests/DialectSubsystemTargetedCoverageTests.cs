using BasicCore.Contracts;
using CommonExceptions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests;

public class DialectSubsystemTargetedCoverageTests
{
    [Test]
    public void Compiler_ParsesCompleteDialectSurface()
    {
        var result = DialectDslTestComposition.CreateCompiler().Compile(
            """
            dialect Coverage
            use A,B
            exclude Z
            requires A,B
            backend interpreter
            allow i_add
            enable O1
            security trusted
            capability sandbox
            """);

        Assert.Multiple(() =>
        {
            Assert.That(result.UseModules, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(result.ExcludeModules, Is.EqualTo(new[] { "Z" }));
            Assert.That(result.BackendDirectives.Select(x => (x.Backend, x.Enabled)), Is.EqualTo(new[] { (TestBackendIds.Interpreter, true) }));
            Assert.That(result.IntrinsicDirectives.Select(x => (x.Name, x.Target)), Is.EqualTo(new[] { ("i_add", TestBackendIds.Any) }));
            Assert.That(result.OptimizerDirectives.Select(x => (x.Name, x.Target)), Is.EqualTo(new[] { ("O1", TestBackendIds.Any) }));
        });
    }

    [Test]
    public void Compiler_InvalidDirective_ReportsMeaningfulError()
    {
        var ex = Assert.Throws<ParserException>(() => DialectDslTestComposition.CreateCompiler().Compile("dialect A\nwat unknown\n"));

        Assert.That(ex!.Message, Does.Contain("Unknown dialect directive"));
    }

    [Test]
    public void RuntimeComposition_BackendScopedCollections_IgnoreUnavailableBackends()
    {
        var plan = new DialectBuildPlan(
            "dialect",
            "1",
            ["A"],
            [TestBackendIds.Interpreter],
            [],
            [new IntrinsicBuildDirective("intr-cil", true, TestBackendIds.Cil)],
            [new OptimizerBuildDirective("opt-cil", true, TestBackendIds.Cil)],
            SecurityProfile.Trusted,
            [],
            new DialectValidationResult());

        var registry = new DialectRuntimeDescriptorRegistryBuilder()
            .RegisterModule(new RuntimeModuleDescriptor("A", typeof(FakeFrontendModule)))
            .RegisterBackend(new RuntimeBackendDescriptor(TestBackendIds.Interpreter, "InterpreterBackend"))
            .Build();

        var composition = new DialectRuntimeCompositionResolver().Resolve(plan, registry);

        Assert.Multiple(() =>
        {
            Assert.That(composition.IsResolved, Is.True);
            Assert.That(composition.EnabledOptimizers, Is.Empty);
            Assert.That(composition.AllowedIntrinsics, Is.Empty);
        });
    }

    [Test]
    public void Regression_FrameworkBuildPlan_IsDeterministicAcrossRuns()
    {
        const string source = "dialect D\nuse B,A\nbefore A,B\nbackend interpreter\nallow i1\nenable O1\n";
        var builder = new DialectCompiledDialectBuildPlanBuilder();

        var first = builder.Build(DialectDslTestComposition.CreateCompiler().Compile(source));
        var second = builder.Build(DialectDslTestComposition.CreateCompiler().Compile(source));

        Assert.Multiple(() =>
        {
            Assert.That(first.OrderedModules, Is.EqualTo(second.OrderedModules));
            Assert.That(first.EnabledBackends, Is.EqualTo(second.EnabledBackends));
            Assert.That(first.IntrinsicDirectives.Select(x => (x.Name, x.Target)),
                Is.EqualTo(second.IntrinsicDirectives.Select(x => (x.Name, x.Target))));
            Assert.That(first.OptimizerDirectives.Select(x => (x.Name, x.Target)),
                Is.EqualTo(second.OptimizerDirectives.Select(x => (x.Name, x.Target))));
        });
    }

    private sealed class FakeFrontendModule : IFrontendCoreModule
    {
    }
}