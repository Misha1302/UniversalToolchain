namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class NativeUnaryMinusPipelineTests
{
    private static readonly string[] NativeModules =
        ModulePipelineTestHelper.FullUniversalModules
            .Where(x => x is not ("Numbers" or "Arithmetic"))
            .Concat(["NativeTypes"])
            .ToArray();

    private static readonly string[] UniversalModules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void NativeUnaryMinus_LiteralAtExpressionStart_ParsesAndExecutes()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("-5", NativeModules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(-5));
    }

    [Test]
    public void NativeUnaryMinus_AfterBinaryOperator_ParsesAsUnaryMinus()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("2 * -3", NativeModules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(-6));
    }

    [Test]
    public void NativeUnaryMinus_BinarySubtraction_IsNotRewrittenAsUnaryMinus()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("9 - 4", NativeModules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void NativeUnaryMinus_ParenthesizedExpression_ParsesAndExecutes()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("-(2 + 3)", NativeModules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(-5));
    }

    [Test]
    public void NativeUnaryMinus_CompilerAndInterpreter_ProduceSameResult()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("-3 * (4 - 7) + 2", NativeModules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
    }

    [TestCase("-7")]
    [TestCase("2 * -3")]
    [TestCase("-(2 + 5) + 9")]
    [TestCase("10 - -2")]
    public void NativeUnaryMinus_And_UniversalUnaryMinus_ProduceEquivalentResult_OnSameExpression(string code)
    {
        using var h = new ModulePipelineTestHelper();

        var nativeResult = h.ExecuteBoth(code, NativeModules);
        var universalResult = h.ExecuteBoth(code, UniversalModules);

        ModulePipelineTestHelper.AssertParity(nativeResult.Compiler, nativeResult.Interpreter);
        ModulePipelineTestHelper.AssertParity(universalResult.Compiler, universalResult.Interpreter);
        ModulePipelineTestHelper.AssertParity(nativeResult.Compiler, universalResult.Compiler);
        ModulePipelineTestHelper.AssertParity(nativeResult.Interpreter, universalResult.Interpreter);
    }
}
