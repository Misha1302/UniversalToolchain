using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Tests;

public class SemanticValidationTests
{
    [Test]
    public void BuildPlan_SuccessfulTopologicalOrdering_UsesDeterministicOrder()
    {
        var parser = new DialectDefinitionParser();
        var builder = new DialectBuildPlanBuilder();
        var parsed = parser.Parse("""
                                  dialect Ordered
                                  use C
                                  use A
                                  use B
                                  before A -> C
                                  before B -> C
                                  """);

        var plan = builder.Build(parsed.Document!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.IsSuccess, Is.True);
            Assert.That(plan.CanBuild, Is.True);
            Assert.That(plan.OrderedModules, Is.EqualTo(new[] { "A", "B", "C" }));
        });
    }

    [Test]
    public void BuildPlan_CycleDetection_ReportsSemanticError()
    {
        var parser = new DialectDefinitionParser();
        var builder = new DialectBuildPlanBuilder();
        var parsed = parser.Parse("""
                                  dialect Cyclic
                                  use A
                                  use B
                                  before A -> B
                                  before B -> A
                                  """);

        var plan = builder.Build(parsed.Document!);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanBuild, Is.False);
            Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S007"), Is.True);
            Assert.That(plan.OrderedModules, Is.Empty);
        });
    }

    [Test]
    public void BuildPlan_RequiresMissingModule_ReportsSemanticError()
    {
        var parser = new DialectDefinitionParser();
        var builder = new DialectBuildPlanBuilder();
        var parsed = parser.Parse("""
                                  dialect Missing
                                  use A
                                  requires A -> B
                                  """);

        var plan = builder.Build(parsed.Document!);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanBuild, Is.False);
            Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S002"), Is.True);
        });
    }

    [Test]
    public void BuildPlan_ContradictoryIntrinsicRule_ReportsSemanticError()
    {
        var document = new DialectSyntaxDocument(
            "ContradictoryIntrinsic",
            "1.0",
            ["A"],
            [],
            [],
            [],
            [
                new IntrinsicDirectiveSyntax("intrinsic-x", true, TestBackendIds.Any),
                new IntrinsicDirectiveSyntax("intrinsic-x", false, TestBackendIds.Any)
            ],
            [],
            null,
            []);

        var builder = new DialectBuildPlanBuilder();
        var plan = builder.Build(document);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanBuild, Is.False);
            Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S004"), Is.True);
        });
    }

    [Test]
    public void BuildPlan_ContradictoryOptimizerRule_ReportsSemanticError()
    {
        var document = new DialectSyntaxDocument(
            "ContradictoryOptimizer",
            "1.0",
            ["A"],
            [],
            [],
            [],
            [],
            [
                new OptimizerDirectiveSyntax("opt-x", true, TestBackendIds.Any),
                new OptimizerDirectiveSyntax("opt-x", false, TestBackendIds.Any)
            ],
            null,
            []);

        var builder = new DialectBuildPlanBuilder();
        var plan = builder.Build(document);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanBuild, Is.False);
            Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S005"), Is.True);
        });
    }

    [Test]
    public void BuildPlan_ContradictoryBackendRule_ReportsSemanticError()
    {
        var document = new DialectSyntaxDocument(
            "ContradictoryBackend",
            "1.0",
            ["A"],
            [],
            [],
            [
                new BackendDirectiveSyntax(TestBackendIds.Interpreter, true),
                new BackendDirectiveSyntax(TestBackendIds.Interpreter, false)
            ],
            [],
            [],
            null,
            []);

        var builder = new DialectBuildPlanBuilder();
        var plan = builder.Build(document);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanBuild, Is.False);
            Assert.That(plan.ValidationResult.Diagnostics.Any(x => x.Code == "S003"), Is.True);
        });
    }

    [Test]
    public void BuildPlan_NormalizesAndCreatesPlan_FromValidInput()
    {
        var parser = new DialectDefinitionParser();
        var builder = new DialectBuildPlanBuilder();

        var parsed = parser.Parse("""
                                  dialect Valid version "1"
                                  use B
                                  use A
                                  backend cil enable
                                  allow intrinsic "x" for any
                                  enable optimizer O1 for any
                                  security trusted
                                  capability c1 = true
                                  """);

        var plan = builder.Build(parsed.Document!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.IsSuccess, Is.True);
            Assert.That(plan.CanBuild, Is.True);
            Assert.That(plan.Name, Is.EqualTo("Valid"));
            Assert.That(plan.OrderedModules, Is.EqualTo(new[] { "B", "A" }));
            Assert.That(plan.EnabledBackends, Is.EqualTo(new[] { TestBackendIds.Cil }));
            Assert.That(plan.IntrinsicDirectives, Has.Count.EqualTo(1));
            Assert.That(plan.OptimizerDirectives, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void SyntaxAndSemanticDiagnostics_AreSeparatedByCodes()
    {
        var parser = new DialectDefinitionParser();
        var builder = new DialectBuildPlanBuilder();

        var syntaxFailure = parser.Parse("dialect A\nrequires A - B\n");
        var semanticInput = parser.Parse("dialect A\nuse A\nrequires A -> B\n");
        var semanticPlan = builder.Build(semanticInput.Document!);

        Assert.Multiple(() =>
        {
            Assert.That(syntaxFailure.Diagnostics.Any(x => x.Code.StartsWith("P", StringComparison.Ordinal)), Is.True);
            Assert.That(semanticPlan.ValidationResult.Diagnostics.Any(x => x.Code.StartsWith("S", StringComparison.Ordinal)), Is.True);
        });
    }
}
