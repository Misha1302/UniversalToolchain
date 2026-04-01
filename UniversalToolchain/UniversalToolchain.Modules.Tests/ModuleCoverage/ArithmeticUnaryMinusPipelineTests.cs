namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ArithmeticUnaryMinusPipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void UnaryMinus_LiteralAtExpressionStart_ParsesAndExecutes()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("-2+3", Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(1));
    }

    [Test]
    public void UnaryMinus_DoubleUnaryMinus_ParsesAndExecutes()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("--2", Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(2));
    }

    [Test]
    public void UnaryMinus_AfterBinaryOperator_ParsesAsUnaryMinus()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("2*-3", Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(-6));
    }

    [Test]
    public void UnaryMinus_AfterOpenParenthesis_ParsesAsUnaryMinus()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("(-3)+1", Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(-2));
    }

    [Test]
    public void UnaryMinus_BinarySubtraction_IsNotRewrittenAsUnaryMinus()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("5-3", "2", Modules);
    }

    [Test]
    public void UnaryMinus_InAssignmentContext_ParsesAndExecutes()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("let x=-2; x+5", Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(3));
    }

    [Test]
    public void UnaryMinus_CompilerAndInterpreter_ProduceSameResult()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("let x=-2; x+5", Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
    }
}
