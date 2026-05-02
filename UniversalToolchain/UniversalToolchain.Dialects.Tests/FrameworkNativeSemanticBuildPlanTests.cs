using CommonExceptions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class FrameworkNativeSemanticBuildPlanTests
{
    [Test]
    public void BuildPlan_UseExcludeConflict_FailsDuringDslValidation()
    {
        var ex = Assert.Throws<ParserException>(() => BuildPlan(
            """
            dialect D
            use A
            exclude A
            """));

        Assert.That(ex!.Message, Does.Contain("both use and exclude"));
    }

    [Test]
    public void BuildPlan_ListBasedOrdering_UsesDeclarationOrderAsDefault()
    {
        const string source =
            """
            dialect D
            use C,B,A
            backend interpreter,cil
            allow i1
            enable O1
            security trusted
            capability cap2,cap1
            """;

        var first = BuildPlan(source);
        var second = BuildPlan(source);

        Assert.Multiple(() =>
        {
            Assert.That(first.CanBuild, Is.True);
            Assert.That(first.OrderedModules, Is.EqualTo(new[] { "C", "B", "A" }));
            Assert.That(first.EnabledBackends, Is.EqualTo(new[] { TestBackendIds.Cil, TestBackendIds.Interpreter }));
            Assert.That(first.Capabilities.Select(x => (x.Key, x.Value)), Is.EqualTo(new[] { ("cap1", true), ("cap2", true) }));
            Assert.That(first.OrderedModules, Is.EqualTo(second.OrderedModules));
            Assert.That(first.EnabledBackends, Is.EqualTo(second.EnabledBackends));
            Assert.That(first.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)),
                Is.EqualTo(second.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target))));
            Assert.That(first.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)),
                Is.EqualTo(second.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target))));
        });
    }

    [Test]
    public void BuildPlan_NoOrderRules_PreservesDeclarationOrder()
    {
        var plan = BuildPlan(
            """
            dialect D
            use C,B,A
            """);

        Assert.That(plan.OrderedModules, Is.EqualTo(new[] { "C", "B", "A" }));
    }

    [Test]
    public void BuildPlan_OrderRules_OverrideDeclarationOrder()
    {
        var plan = BuildPlan(
            """
            dialect D
            use C,B,A
            before A,B
            """);

        Assert.That(plan.OrderedModules, Is.EqualTo(new[] { "C", "A", "B" }));
    }

    [Test]
    public void BuildPlan_ReadySetTieBreaker_UsesDeclarationOrder()
    {
        var plan = BuildPlan(
            """
            dialect D
            use D,C,B,A
            before C,A
            """);

        Assert.That(plan.OrderedModules, Is.EqualTo(new[] { "D", "C", "B", "A" }));
    }

    [Test]
    public void ParserRegistry_UsesFeatureOwnedCreators_AndStagedDirectiveSlots()
    {
        var registry = DialectDslTestComposition.CreateRegistry();
        var creatorTypes = DialectDslParserNodeRegistry.CreateRegistrations(registry).Select(x => x.Creator.GetType().Name).ToArray();
        var descriptors = DialectDirectiveDescriptors.CreateOrdered(registry);

        Assert.Multiple(() =>
        {
            Assert.That(creatorTypes, Does.Contain(nameof(DialectDeclarationNodeCreator)));
            Assert.That(creatorTypes, Does.Contain(nameof(FeatureDialectDirectiveNodeCreator)));
            Assert.That(creatorTypes, Does.Contain(nameof(DialectDocumentNodeCreator)));
            Assert.That(descriptors.Select(x => x.Id), Is.EqualTo(new[]
            {
                "builtin.modules.use",
                "builtin.modules.exclude",
                "builtin.order.requires",
                "builtin.order.before",
                "builtin.order.after",
                "builtin.backends.enable",
                "builtin.intrinsics.allow",
                "builtin.intrinsics.forbid",
                "builtin.optimizers.enable",
                "builtin.optimizers.disable",
                "builtin.security.profile",
                "builtin.capabilities.enable"
            }));
            Assert.That(descriptors.Select(x => x.ParserOrder.Slot), Is.EqualTo(new[]
            {
                DialectDirectiveSlot.ModuleSelection,
                DialectDirectiveSlot.ModuleSelection,
                DialectDirectiveSlot.ModuleOrdering,
                DialectDirectiveSlot.ModuleOrdering,
                DialectDirectiveSlot.ModuleOrdering,
                DialectDirectiveSlot.BackendSelection,
                DialectDirectiveSlot.IntrinsicPolicy,
                DialectDirectiveSlot.IntrinsicPolicy,
                DialectDirectiveSlot.OptimizerPolicy,
                DialectDirectiveSlot.OptimizerPolicy,
                DialectDirectiveSlot.Security,
                DialectDirectiveSlot.Capabilities
            }));
        });
    }

    private static DialectBuildPlan BuildPlan(string source) => new DialectCompiledDialectBuildPlanBuilder().Build(DialectDslTestComposition.CreateCompiler().Compile(source));
}