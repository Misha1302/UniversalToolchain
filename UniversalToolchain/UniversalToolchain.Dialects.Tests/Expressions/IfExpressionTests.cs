using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Features;
using UniversalToolchain.Features.Abstractions;
using UniversalToolchain.Features.Core;
using UniversalToolchain.Modules.Tests;

namespace UniversalToolchain.Dialects.Tests.Expressions;

[TestFixture]
public sealed class IfExpressionTests
{
    private const string IfExpressionDialect = """
                                               dialect IfExpression
                                               use Arithmetic,BooleanConditions,ComparisonConditions,IfExpression,Identifier,Numbers,Scopes,Variables,Whitespaces
                                               backend compiler,interpreter
                                               """;

    private const string WithoutIfExpressionDialect = """
                                                      dialect WithoutIfExpression
                                                      use Arithmetic,BooleanConditions,ComparisonConditions,Identifier,Numbers,Scopes,Variables,Whitespaces
                                                      backend compiler,interpreter
                                                      """;

    [Test]
    public void IfExpression_TrueCondition_ReturnsThenBranch()
    {
        using var host = DialectTestHostInfrastructure.CreateHostFromDialectText(IfExpressionDialect);

        var compilerResult = host.Run("if true then 10 else 20", "compiler");
        var interpreterResult = host.Run("if true then 10 else 20", "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(BackendParityInfrastructure.AsNumber(compilerResult), Is.EqualTo(10d).Within(1e-9));
            Assert.That(BackendParityInfrastructure.AsNumber(interpreterResult), Is.EqualTo(10d).Within(1e-9));
        });
    }

    [Test]
    public void IfExpression_FalseCondition_ReturnsElseBranch()
    {
        using var host = DialectTestHostInfrastructure.CreateHostFromDialectText(IfExpressionDialect);

        var compilerResult = host.Run("if false then 10 else 20", "compiler");
        var interpreterResult = host.Run("if false then 10 else 20", "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(BackendParityInfrastructure.AsNumber(compilerResult), Is.EqualTo(20d).Within(1e-9));
            Assert.That(BackendParityInfrastructure.AsNumber(interpreterResult), Is.EqualTo(20d).Within(1e-9));
        });
    }

    [Test]
    public void IfExpression_NestedInArithmetic_ReturnsExpectedResult()
    {
        using var host = DialectTestHostInfrastructure.CreateHostFromDialectText(IfExpressionDialect);

        var code = "(if price > 100.0 then price * 0.9 else price) + fee";
        var compiler = host.GetArtifactCompiler<System.Reflection.Emit.DynamicMethod>("compiler").Compile(code, CreateDeclaredBindings()).CreateSession();
        var interpreter = host.GetArtifactCompiler<IntermediateRepresentationAbstractions.IAbstractIR>("interpreter").Compile(code, CreateDeclaredBindings()).CreateSession();

        SetArguments(compiler);
        SetArguments(interpreter);

        var compilerResult = compiler.Run();
        var interpreterResult = interpreter.Run();

        Assert.Multiple(() =>
        {
            Assert.That(BackendParityInfrastructure.AsNumber(compilerResult), Is.EqualTo(95d).Within(1e-9));
            Assert.That(BackendParityInfrastructure.AsNumber(interpreterResult), Is.EqualTo(95d).Within(1e-9));
        });
    }

    [Test]
    public void IfExpression_ConditionNumber_ReturnsTypeDiagnostic()
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(IfExpressionDialect, "if 1 then 2 else 3");

        AssertFailureContainsTypeDiagnostic(compilerResult, interpreterResult, "condition must be bool");
    }

    [Test]
    public void IfExpression_BranchTypeMismatch_ReturnsDiagnostic()
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(IfExpressionDialect, "if true then 1 else false");

        AssertFailureContainsTypeDiagnostic(compilerResult, interpreterResult, "branches must both resolve");
    }

    [Test]
    public void IfExpression_NotSelected_ReturnsFeatureDiagnostic()
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(WithoutIfExpressionDialect, "if true then 1 else 2");

        Assert.Multiple(() =>
        {
            Assert.That(compilerResult.IsSuccess, Is.False);
            Assert.That(interpreterResult.IsSuccess, Is.False);
            Assert.That(compilerResult.Exception!.Message, Does.Contain("if").IgnoreCase);
            Assert.That(interpreterResult.Exception!.Message, Does.Contain("if").IgnoreCase);
        });
    }

    [Test]
    public void IfExpression_CompilerAndInterpreterParity()
    {
        using var host = DialectTestHostInfrastructure.CreateHostFromDialectText(IfExpressionDialect);

        var code = "if price > 100.0 then price * 0.9 else price";
        var compiler = host.GetArtifactCompiler<System.Reflection.Emit.DynamicMethod>("compiler").Compile(code, CreateDeclaredBindings()).CreateSession();
        var interpreter = host.GetArtifactCompiler<IntermediateRepresentationAbstractions.IAbstractIR>("interpreter").Compile(code, CreateDeclaredBindings()).CreateSession();

        SetArguments(compiler);
        SetArguments(interpreter);

        var compilerResult = BackendExecutionResult.Success(compiler.Run());
        var interpreterResult = BackendExecutionResult.Success(interpreter.Run());

        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);

        Assert.That(BackendParityInfrastructure.AsNumber(compilerResult.Value), Is.EqualTo(90d).Within(1e-9));
    }

    [Test]
    public void FeatureProjection_ReportsIfExpressionWhenSelected()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(IfExpressionDialect, "if-expression-inline");

        Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        ILanguageFeatureCatalog catalog = new WistLanguageFeatureCatalog();
        var explanation = new DialectFeatureExplanationProjector(catalog).Project(composition);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.AvailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Contain(WistLanguageFeatureIds.IfExpression.Value));
            Assert.That(explanation.UnavailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Not.Contain(WistLanguageFeatureIds.IfExpression.Value));
        });
    }

    private static void AssertFailureContainsTypeDiagnostic(
        BackendExecutionResult compilerResult,
        BackendExecutionResult interpreterResult,
        string messageFragment)
    {
        Assert.Multiple(() =>
        {
            Assert.That(compilerResult.IsSuccess, Is.False);
            Assert.That(interpreterResult.IsSuccess, Is.False);
            Assert.That(compilerResult.Exception!.Message, Does.Contain("WST-TYPE-001").And.Contain(messageFragment).IgnoreCase);
            Assert.That(interpreterResult.Exception!.Message, Does.Contain("WST-TYPE-001").And.Contain(messageFragment).IgnoreCase);
        });
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings()
    {
        return new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };
    }

    private static void SetArguments(BasicCore.Execution.ICompiledArtifactSession session)
    {
        session.SetArgument("price", 100.0d);
        session.SetArgument("fee", 5.0d);
    }
}
