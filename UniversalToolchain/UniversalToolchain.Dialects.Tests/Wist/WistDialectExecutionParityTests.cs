using System.Globalization;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using ConditionsModule.Creators;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist;

public class WistDialectExecutionParityTests
{
    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForSimpleArithmeticDialect()
        => AssertParity(CreateFullDialect(), "2 + 3 * 4", 14d);

    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForConditionsDialect()
        => AssertParity(CreateFullDialect(), "let x = 15\nlet result = 0\nif x > 10 (\n    if x < 20\n        result = 1\n    else\n        result = 2\n)\nelse\n    result = 3\nresult", 1d);

    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForIfExpressionDialect()
        => AssertParity(CreateFullDialect(), "let x = 15\nif x > 10 then 100 else 200", 100d);

    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForIfExpression_WithLocalFalseBranchValue()
        => AssertParity(CreateFullDialect(), "let x = 10.0\nif x < 0.0 then 0.0 else x", 10d);

    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForIfExpression_WithLocalTrueBranchLiteral()
        => AssertParity(CreateFullDialect(), "let x = -10.0\nif x < 0.0 then 0.0 else x", 0d);

    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForIfExpression_WithIntermediateResultLocal()
        => AssertParity(CreateFullDialect(), "let result = 255.0\nif result < 0.0 then 0.0 else result", 255d);

    [Test]
    public void IfExpressionNodeCreator_ShouldPreserveSiblingsAfterIfExpression()
    {
        var ifNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("If");
        var thenNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Then");
        var elseNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Else");
        var expressionNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Expression");
        var statementNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Statement");
        var scopeNodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Scope");

        var condition = new AstNode(expressionNodeType, null, []);
        var trueBranch = new AstNode(expressionNodeType, null, []);
        var falseBranch = new AstNode(expressionNodeType, null, []);
        var sibling1 = new AstNode(statementNodeType, null, []);
        var sibling2 = new AstNode(statementNodeType, null, []);

        var scope = new AstNode(
            scopeNodeType,
            null,
            [
                new AstNode(ifNodeType, null, []),
                condition,
                new AstNode(thenNodeType, null, []),
                trueBranch,
                new AstNode(elseNodeType, null, []),
                falseBranch,
                sibling1,
                sibling2
            ]);

        var creator = new IfExpressionNodeCreator();
        var wasCreated = creator.TryCreateNode(scope, 0);

        Assert.Multiple(() =>
        {
            Assert.That(wasCreated, Is.True);
            Assert.That(scope.Children.Count, Is.EqualTo(3));
            Assert.That(scope.Children[1], Is.SameAs(sibling1));
            Assert.That(scope.Children[2], Is.SameAs(sibling2));
        });
    }

    [Test]
    public void IfExpression_WhenFalseBranchIsMissing_ThrowsClearDiagnostic()
    {
        using var host = ComposeAndCreateHost(CreateFullDialect());

        var interpreterException = Assert.Throws<InvalidOperationException>(
            () => host.Run("if 1.0 < 2.0 then 10.0 else", "interpreter"));

        var compilerException = Assert.Throws<InvalidOperationException>(
            () => host.Run("if 1.0 < 2.0 then 10.0 else", "compiler"));

        Assert.Multiple(() =>
        {
            Assert.That(interpreterException, Is.Not.Null);
            Assert.That(compilerException, Is.Not.Null);
            Assert.That(interpreterException?.Message, Does.Contain("if-expression false branch"));
            Assert.That(compilerException?.Message, Does.Contain("if-expression false branch"));
        });
    }

    [Test]
    public void IfExpression_WhenConditionIsNotBool_ReturnsOrThrowsClearDiagnostic()
    {
        using var host = ComposeAndCreateHost(CreateFullDialect());

        var interpreterException = Assert.Throws<InvalidOperationException>(() => host.Run("if 1.0 then 10.0 else 20.0", "interpreter"));
        var compilerException = Assert.Throws<InvalidOperationException>(() => host.Run("if 1.0 then 10.0 else 20.0", "compiler"));

        Assert.Multiple(() =>
        {
            Assert.That(interpreterException!.Message, Does.Contain("condition must be boolean"));
            Assert.That(compilerException!.Message, Does.Contain("condition must be boolean"));
        });
    }

    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForVariablesAndScopesDialect()
        => AssertParity(CreateFullDialect(), "let x = 7\nlet y = x + 2\ny", 9d);

    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForLoopsDialect()
        => AssertParity(CreateFullDialect(), "let sum = 0\nlet i = 1\n@start:\nif i > 4 goto @end\nsum = sum + i\ni = i + 1\ngoto @start\n@end:\nsum", 10d);

    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForEqualityAndComparisonDialect()
        => AssertParity(CreateFullDialect(), "let score = 85\nlet grade = 0\nif score >= 90\n    (grade = 5)\nelif score >= 80\n    (grade = 4)\nelse\n    (grade = 1)\ngrade", 4d);

    [Test]
    public void InterpreterAndCompiler_ShouldMatch_ForLabelsScenario()
        => AssertParity(CreateFullDialect(), "let counter = 0\nlet total = 0\n@loop:\nif counter >= 5 goto @end\ncounter = counter + 1\ntotal = total + counter\ngoto @loop\n@end:\ntotal", 15d);

    [Test]
    public void ComposeText_ShouldReturnSuccessfulResult_ForValidDialect()
    {
        using var provider = CreateWorkflowProviderWithCilAndInterpreter();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var result = workflow.ComposeText(CreateFullDialect(), "inline");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(result)));
            Assert.That(result.RuntimeSelection, Is.Not.Null);
        });
    }

    [Test]
    public void CreateHost_ShouldRejectUnsuccessfulCompositionResult()
    {
        using var provider = CreateWorkflowProviderWithCilAndInterpreter();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect Broken\nuse MissingModule\nbackend interpreter", "broken-inline");

        var ex = Assert.Throws<ArgumentException>(() => workflow.CreateHost(composition));

        Assert.That(ex!.Message, Does.Contain("must be successful"));
    }

    private static void AssertParity(string dialect, string program, double expected)
    {
        using var host = ComposeAndCreateHost(dialect);
        var interpreter = ToDouble(host.Run(program, "interpreter"));
        var compiler = ToDouble(host.Run(program, "compiler"));

        Assert.Multiple(() =>
        {
            Assert.That(interpreter, Is.EqualTo(compiler).Within(1e-9));
            Assert.That(interpreter, Is.EqualTo(expected).Within(1e-9));
        });
    }

    private static WistDialectExecutionHost ComposeAndCreateHost(string dialect)
    {
        using var provider = CreateWorkflowProviderWithCilAndInterpreter();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(dialect, "inline-dialect");
        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        return workflow.CreateHost(composition);
    }

    private static ServiceProvider CreateWorkflowProviderWithCilAndInterpreter()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static string CreateFullDialect() => """
                                                 dialect D
                                                 use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
                                                 enable LocalVariablesOptimization
                                                 backend compiler,interpreter
                                                 """;

    private static double ToDouble(object? value)
    {
        return value switch
        {
            RealNumberImpl number => number.GetValue(),
            int intValue => intValue,
            long longValue => longValue,
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            decimal decimalValue => (double)decimalValue,
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };
    }
}
