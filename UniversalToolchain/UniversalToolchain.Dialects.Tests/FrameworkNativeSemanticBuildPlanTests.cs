using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Parsing;

using UniversalToolchain.Dialects.Abstractions;
namespace UniversalToolchain.Dialects.Tests;

public class FrameworkNativeSemanticBuildPlanTests
{
    [Test]
    public void BuildPlan_UseExcludeConflict_IsReported()
    {
        var compiled = Compile(
            """
            dialect D
            use A
            exclude A
            """);

        var plan = new DialectCompiledDialectBuildPlanBuilder().Build(compiled);

        Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S101"), Is.True);
    }

    [Test]
    public void BuildPlan_ContradictoryBackendIntrinsicOptimizer_IsReported()
    {
        var compiled = Compile(
            """
            dialect D
            use A
            backend interpreter enable
            backend interpreter disable
            allow intrinsic "x" for any
            forbid intrinsic "x" for any
            enable optimizer Opt for cil
            disable optimizer Opt for cil
            """);

        var plan = new DialectCompiledDialectBuildPlanBuilder().Build(compiled);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S102"), Is.True);
            Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S103"), Is.True);
            Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S104"), Is.True);
        });
    }

    [Test]
    public void BuildPlan_BeforeAfterCycle_IsReported()
    {
        var compiled = Compile(
            """
            dialect D
            use A
            use B
            before A -> B
            before B -> A
            """);

        var plan = new DialectCompiledDialectBuildPlanBuilder().Build(compiled);

        Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S105"), Is.True);
        Assert.That(plan.OrderedModules, Is.Empty);
    }

    [Test]
    public void BuildPlan_ValidInput_ProducesNormalizedDeterministicPlan()
    {
        var source =
            """
            dialect D
            use B
            use A
            exclude Z
            before A -> B
            backend cil disable
            backend interpreter enable
            allow intrinsic "i1" for any
            enable optimizer O1 for interpreter
            security trusted
            capability cap2 = false
            capability cap1 = true
            """;

        var builder = new DialectCompiledDialectBuildPlanBuilder();
        var first = builder.Build(Compile(source));
        var second = builder.Build(Compile(source));

        Assert.Multiple(() =>
        {
            Assert.That(first.CanBuild, Is.True);
            Assert.That(first.OrderedModules, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(first.EnabledBackends, Is.EqualTo(new[] { DialectBackendTarget.Interpreter }));
            Assert.That(first.DisabledBackends, Is.EqualTo(new[] { DialectBackendTarget.Cil }));
            Assert.That(first.Capabilities.Select(x => x.Key), Is.EqualTo(new[] { "cap1", "cap2" }));

            Assert.That(first.OrderedModules, Is.EqualTo(second.OrderedModules));
            Assert.That(first.EnabledBackends, Is.EqualTo(second.EnabledBackends));
            Assert.That(first.DisabledBackends, Is.EqualTo(second.DisabledBackends));
            Assert.That(first.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)),
                Is.EqualTo(second.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target))));
            Assert.That(first.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)),
                Is.EqualTo(second.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target))));
            Assert.That(first.Capabilities.Select(x => (x.Key, x.Value)),
                Is.EqualTo(second.Capabilities.Select(x => (x.Key, x.Value))));
        });
    }



    [Test]
    public void BuildPlan_FrameworkPipelineAndDslParser_ProduceEquivalentSemantics()
    {
        var source =
            """
            dialect D
            use B
            use A
            before A -> B
            backend interpreter enable
            backend cil disable
            allow intrinsic "i1" for any
            enable optimizer O1 for interpreter
            security trusted
            capability cap2 = false
            capability cap1 = true
            """;

        var frameworkPlan = new DialectCompiledDialectBuildPlanBuilder().Build(Compile(source));

        var parserResult = new DialectDefinitionParser().Parse(source);
        Assert.That(parserResult.IsSuccess, Is.True);

        var parserPlan = new DialectBuildPlanBuilder().Build(parserResult.Document!);

        Assert.Multiple(() =>
        {
            Assert.That(frameworkPlan.OrderedModules, Is.EqualTo(parserPlan.OrderedModules));
            Assert.That(frameworkPlan.EnabledBackends, Is.EqualTo(parserPlan.EnabledBackends));
            Assert.That(frameworkPlan.DisabledBackends, Is.EqualTo(parserPlan.DisabledBackends));
            Assert.That(frameworkPlan.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)),
                Is.EqualTo(parserPlan.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target))));
            Assert.That(frameworkPlan.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)),
                Is.EqualTo(parserPlan.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target))));
            Assert.That(frameworkPlan.SecurityProfile, Is.EqualTo(parserPlan.SecurityProfile));
            Assert.That(frameworkPlan.Capabilities.Select(x => (x.Key, x.Value)),
                Is.EqualTo(parserPlan.Capabilities.Select(x => (x.Key, x.Value))));
        });
    }

    [Test]
    public void BuildPlan_BackendContradictionDiagnostic_HasClearMessage()
    {
        var compiled = Compile(
            """
            dialect D
            use A
            backend interpreter enable
            backend interpreter disable
            """);

        var plan = new DialectCompiledDialectBuildPlanBuilder().Build(compiled);
        var diagnostic = plan.ValidationResult.Diagnostics.Single(x => x.Code == "S102");

        Assert.That(diagnostic.Message, Is.EqualTo("Contradictory backend directives for 'interpreter'."));
    }

    [Test]
    public void BuildPlan_NoOrderRules_UsesLexicographicTieBreaker()
    {
        var compiled = Compile(
            """
            dialect D
            use C
            use A
            use B
            """);

        var plan = new DialectCompiledDialectBuildPlanBuilder().Build(compiled);

        Assert.That(plan.OrderedModules, Is.EqualTo(new[] { "A", "B", "C" }));
    }
    private static DialectDefinitionSlice Compile(string source)
    {
        return new DialectDslCompiler().Compile(source);
    }
}
