using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Core.Groups;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Tests;

public class DialectBuildPlanBuilderConsistencyTests
{
    [Test]
    public void SyntaxBuildPlanBuilder_UsesSharedBuildPlanCore()
    {
        var document = CreateSyntaxDocument();
        var expectedDiagnostics = new List<DialectDiagnostic>();

        var actual = new DialectBuildPlanBuilder().Build(document);
        var expected = DialectDefinitionSemanticBinder.BuildPlanCore(
            new SyntaxDialectBindingSource(document),
            expectedDiagnostics,
            "S007",
            "Order rules contain a cycle involving modules",
            "S002",
            "Order rule references module(s) not present in active module set");

        AssertBuildPlansEqual(expected, actual);
    }

    [Test]
    public void CompiledBuildPlanBuilder_UsesSharedBuildPlanCore()
    {
        var slice = CreateCompiledSlice();
        var expectedDiagnostics = new List<DialectDiagnostic>();

        var actual = new DialectCompiledDialectBuildPlanBuilder().Build(slice);
        var expected = DialectDefinitionSemanticBinder.BuildPlanCore(
            new CompiledDialectBindingSource(slice),
            expectedDiagnostics,
            "S105",
            "Order directives contain a cycle involving modules");

        AssertBuildPlansEqual(expected, actual);
    }

    [Test]
    public void SyntaxBuildPlanBuilder_KnownGroup_ExpandsModulesAndCapabilities()
    {
        var document = CreateSyntaxDocument(useModules: ["TestCore", "Runtime"]);
        var plan = CreateSyntaxGroupedBuilder().Build(document);

        Assert.Multiple(() =>
        {
            Assert.That(plan.OrderedModules, Is.EqualTo(new[] { "Core", "Expressions", "Runtime" }));
            Assert.That(plan.Capabilities["test.feature"], Is.True);
            Assert.That(plan.ValidationResult.IsValid, Is.True);
        });
    }

    [Test]
    public void SyntaxBuildPlanBuilder_UnknownGroupAlias_KeepsAliasAsModule()
    {
        var document = CreateSyntaxDocument(useModules: ["UnknownAlias"], orderRules: []);
        var plan = CreateSyntaxGroupedBuilder().Build(document);

        Assert.Multiple(() =>
        {
            Assert.That(plan.OrderedModules, Is.EqualTo(new[] { "UnknownAlias" }));
            Assert.That(plan.ValidationResult.IsValid, Is.True);
        });
    }

    [Test]
    public void SyntaxBuildPlanBuilder_GroupCapabilityConflict_ReportsDiagnostic()
    {
        var document = CreateSyntaxDocument(
            useModules: ["TestCore", "Runtime"],
            capabilities: [new KeyValuePair<string, bool>("test.feature", false)]);
        var plan = CreateSyntaxGroupedBuilder().Build(document);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ValidationResult.IsValid, Is.False);
            Assert.That(plan.ValidationResult.Diagnostics.Select(x => x.Code), Does.Contain("G001"));
        });
    }

    [Test]
    public void CompiledBuildPlanBuilder_KnownGroup_ExpandsModulesAndCapabilities()
    {
        var slice = CreateCompiledSlice(useModules: ["TestCore", "Runtime"]);
        var plan = CreateCompiledGroupedBuilder().Build(slice);

        Assert.Multiple(() =>
        {
            Assert.That(plan.OrderedModules, Is.EqualTo(new[] { "Core", "Expressions", "Runtime" }));
            Assert.That(plan.Capabilities["test.feature"], Is.True);
            Assert.That(plan.ValidationResult.IsValid, Is.True);
        });
    }

    [Test]
    public void SyntaxBuildPlanBuilder_GroupExpansion_RepeatedBuildsProduceSamePlan()
    {
        var document = CreateSyntaxDocument(useModules: ["TestCore", "Runtime"]);
        var signatures = Enumerable.Range(0, 30)
            .Select(_ => BuildPlanSignature(CreateSyntaxGroupedBuilder().Build(document)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.That(signatures, Has.Length.EqualTo(1));
    }

    [Test]
    public void BindCore_PropagatesVersionIntoDialectDefinition()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var document = CreateSyntaxDocument("2.1");

        var definition = DialectDefinitionSemanticBinder.BindCore(new SyntaxDialectBindingSource(document), diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Version, Is.EqualTo("2.1"));
            Assert.That(diagnostics, Is.Empty);
        });
    }

    [Test]
    public void BindCore_PropagatesBaseDialectNameIntoDialectDefinition()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var document = CreateSyntaxDocument(baseDialectName: "base-dialect");

        var definition = DialectDefinitionSemanticBinder.BindCore(new SyntaxDialectBindingSource(document), diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(definition.BaseDialectName, Is.EqualTo("base-dialect"));
            Assert.That(diagnostics, Is.Empty);
        });
    }

    [Test]
    public void SyntaxAndCompiledEquivalentInputs_ProduceEquivalentDialectDefinitions_WhenMetadataMatches()
    {
        var syntaxDiagnostics = new List<DialectDiagnostic>();
        var compiledDiagnostics = new List<DialectDiagnostic>();
        var document = CreateSyntaxDocument("3.0", "base-dialect");
        var slice = CreateCompiledSlice("3.0", "base-dialect");

        var syntaxDefinition = DialectDefinitionSemanticBinder.BindCore(new SyntaxDialectBindingSource(document), syntaxDiagnostics);
        var compiledDefinition = DialectDefinitionSemanticBinder.BindCore(new CompiledDialectBindingSource(slice), compiledDiagnostics);

        AssertDefinitionsEqual(syntaxDefinition, compiledDefinition);
        Assert.Multiple(() =>
        {
            Assert.That(syntaxDiagnostics, Is.Empty);
            Assert.That(compiledDiagnostics, Is.Empty);
        });
    }

    private static DialectSyntaxDocument CreateSyntaxDocument(
        string? version = "1.0",
        string? baseDialectName = "base",
        IReadOnlyList<string>? useModules = null,
        IReadOnlyList<KeyValuePair<string, bool>>? capabilities = null,
        IReadOnlyList<OrderRule>? orderRules = null) =>
        new(
            "dialect",
            version,
            useModules ?? ["Core", "Expressions", "Runtime"],
            ["Legacy"],
            orderRules ??
            [
                new OrderRule(OrderRuleKind.Requires, "Expressions", "Core"),
                new OrderRule(OrderRuleKind.After, "Runtime", "Expressions")
            ],
            [
                new BackendDirectiveSyntax(TestBackendIds.Interpreter, true),
                new BackendDirectiveSyntax(TestBackendIds.Cil, false)
            ],
            [
                new IntrinsicDirectiveSyntax("add_i32", true, TestBackendIds.Any),
                new IntrinsicDirectiveSyntax("unsafe_reflect", false, TestBackendIds.CilSelector)
            ],
            [
                new OptimizerDirectiveSyntax("const_fold", true, TestBackendIds.Any),
                new OptimizerDirectiveSyntax("inline", false, TestBackendIds.InterpreterSelector)
            ],
            SecurityProfile.Restricted,
            capabilities ??
            [
                new KeyValuePair<string, bool>("sandbox", true),
                new KeyValuePair<string, bool>("unsafe-interop", false)
            ],
            baseDialectName);

    private static DialectDefinitionSlice CreateCompiledSlice(
        string? version = "1.0",
        string? baseDialectName = "base",
        IReadOnlyList<string>? useModules = null) =>
        new(
            "dialect",
            useModules ?? ["Core", "Expressions", "Runtime"],
            ["Legacy"],
            [
                new DialectOrderDirective(DialectOrderDirectiveKind.Requires, "Expressions", "Core"),
                new DialectOrderDirective(DialectOrderDirectiveKind.After, "Runtime", "Expressions")
            ],
            [
                new DialectBackendDirective(TestBackendIds.Interpreter, true),
                new DialectBackendDirective(TestBackendIds.Cil, false)
            ],
            [
                new DialectIntrinsicDirective("add_i32", true, TestBackendIds.Any),
                new DialectIntrinsicDirective("unsafe_reflect", false, TestBackendIds.CilSelector)
            ],
            [
                new DialectOptimizerDirective("const_fold", true, TestBackendIds.Any),
                new DialectOptimizerDirective("inline", false, TestBackendIds.InterpreterSelector)
            ],
            DialectSecurityProfile.Restricted,
            [
                new DialectCapabilityDirective("sandbox", true),
                new DialectCapabilityDirective("unsafe-interop", false)
            ],
            version,
            baseDialectName);

    private static DialectBuildPlanBuilder CreateSyntaxGroupedBuilder() => new(CreateGroupExpander());

    private static DialectCompiledDialectBuildPlanBuilder CreateCompiledGroupedBuilder() => new(CreateGroupExpander());

    private static DialectGroupExpander CreateGroupExpander() => new(new CompositeDialectGroupCatalog([new TestDialectGroupProvider()]));

    private static string BuildPlanSignature(DialectBuildPlan plan)
    {
        return string.Join("|", plan.OrderedModules)
               + "::"
               + string.Join("|", plan.Capabilities.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}={x.Value}"))
               + "::"
               + string.Join("|", plan.ValidationResult.Diagnostics.Select(x => $"{x.Code}:{x.Message}:{x.Severity}"));
    }

    private static void AssertBuildPlansEqual(DialectBuildPlan expected, DialectBuildPlan actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Version, Is.EqualTo(expected.Version));
            Assert.That(actual.OrderedModules, Is.EqualTo(expected.OrderedModules));
            Assert.That(actual.EnabledBackends, Is.EqualTo(expected.EnabledBackends));
            Assert.That(actual.DisabledBackends, Is.EqualTo(expected.DisabledBackends));
            Assert.That(actual.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)), Is.EqualTo(expected.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target))));
            Assert.That(actual.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)), Is.EqualTo(expected.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target))));
            Assert.That(actual.SecurityProfile, Is.EqualTo(expected.SecurityProfile));
            Assert.That(actual.Capabilities, Is.EqualTo(expected.Capabilities));
            Assert.That(actual.ValidationResult.IsValid, Is.EqualTo(expected.ValidationResult.IsValid));
            Assert.That(
                actual.ValidationResult.Diagnostics.Select(x => (x.Code, x.Message, x.Severity)),
                Is.EqualTo(expected.ValidationResult.Diagnostics.Select(x => (x.Code, x.Message, x.Severity))));
        });
    }

    private static void AssertDefinitionsEqual(DialectDefinition expected, DialectDefinition actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Version, Is.EqualTo(expected.Version));
            Assert.That(actual.BaseDialectName, Is.EqualTo(expected.BaseDialectName));
            Assert.That(actual.ModulePolicy.IncludedModules, Is.EqualTo(expected.ModulePolicy.IncludedModules));
            Assert.That(actual.ModulePolicy.ExcludedModules, Is.EqualTo(expected.ModulePolicy.ExcludedModules));
            Assert.That(actual.BackendPolicy.EnabledBackends, Is.EqualTo(expected.BackendPolicy.EnabledBackends));
            Assert.That(actual.BackendPolicy.DisabledBackends, Is.EqualTo(expected.BackendPolicy.DisabledBackends));
            Assert.That(actual.IntrinsicPolicy.AllowedIntrinsics, Is.EqualTo(expected.IntrinsicPolicy.AllowedIntrinsics));
            Assert.That(actual.IntrinsicPolicy.ForbiddenIntrinsics, Is.EqualTo(expected.IntrinsicPolicy.ForbiddenIntrinsics));
            Assert.That(actual.OptimizerPolicy.EnabledOptimizers, Is.EqualTo(expected.OptimizerPolicy.EnabledOptimizers));
            Assert.That(actual.OptimizerPolicy.DisabledOptimizers, Is.EqualTo(expected.OptimizerPolicy.DisabledOptimizers));
            Assert.That(actual.SecurityPolicy?.Profile, Is.EqualTo(expected.SecurityPolicy?.Profile));
            Assert.That(actual.CapabilityPolicy.Capabilities, Is.EqualTo(expected.CapabilityPolicy.Capabilities));
            Assert.That(actual.Extensions, Is.EqualTo(expected.Extensions));
            Assert.That(
                actual.OrderRules.Select(x => (x.Kind, x.ModuleName, x.RelatedModuleName)),
                Is.EqualTo(expected.OrderRules.Select(x => (x.Kind, x.ModuleName, x.RelatedModuleName))));
        });
    }

    private sealed class TestDialectGroupProvider : IDialectGroupProvider
    {
        public IReadOnlyList<DialectGroupDescriptor> GetGroups() =>
        [
            new(
                "TestCore",
                ["Core", "Expressions"],
                [new KeyValuePair<string, bool>("test.feature", true)])
        ];
    }
}