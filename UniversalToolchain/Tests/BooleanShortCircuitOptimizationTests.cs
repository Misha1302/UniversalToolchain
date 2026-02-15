namespace Tests;

[TestFixture]
public class BooleanShortCircuitOptimizationTests : TestBase
{
    [SetUp]
    public void Setup() => BooleanSideEffectProbe.Reset();

    [Test]
    public void Execute_ShortCircuitAnd_SkipsRightOperandSideEffects()
    {
        const string code = "Tests.BooleanSideEffectProbe.FFalse() and Tests.BooleanSideEffectProbe.FTrue()";

        var result = ExecuteCode<bool>(code);
        // ExecuteCode runs expression on each configured core (CoresCount).

        Assert.That(result, Is.False);
        Assert.That(BooleanSideEffectProbe.FalseCalls, Is.EqualTo(CoresCount));
        Assert.That(BooleanSideEffectProbe.TrueCalls, Is.EqualTo(0));
    }

    [Test]
    public void Execute_ShortCircuitOr_SkipsRightOperandSideEffects()
    {
        const string code = "Tests.BooleanSideEffectProbe.FTrue() or Tests.BooleanSideEffectProbe.FFalse()";

        var result = ExecuteCode<bool>(code);
        // Side effects are expected once per core, but right operand must stay at zero.

        Assert.That(result, Is.True);
        Assert.That(BooleanSideEffectProbe.TrueCalls, Is.EqualTo(CoresCount));
        Assert.That(BooleanSideEffectProbe.FalseCalls, Is.EqualTo(0));
    }


    [Test]
    public void Execute_ShortCircuitAnd_WhenLeftTrue_EvaluatesRightOperand()
    {
        const string code = "Tests.BooleanSideEffectProbe.FTrue() and Tests.BooleanSideEffectProbe.FFalse()";

        var result = ExecuteCode<bool>(code);

        Assert.That(result, Is.False);
        Assert.That(BooleanSideEffectProbe.TrueCalls, Is.EqualTo(CoresCount));
        Assert.That(BooleanSideEffectProbe.FalseCalls, Is.EqualTo(CoresCount));
    }

    [Test]
    public void Execute_ShortCircuitOr_WhenLeftFalse_EvaluatesRightOperand()
    {
        const string code = "Tests.BooleanSideEffectProbe.FFalse() or Tests.BooleanSideEffectProbe.FTrue()";

        var result = ExecuteCode<bool>(code);

        Assert.That(result, Is.True);
        Assert.That(BooleanSideEffectProbe.FalseCalls, Is.EqualTo(CoresCount));
        Assert.That(BooleanSideEffectProbe.TrueCalls, Is.EqualTo(CoresCount));
    }

    [Test]
    public void Execute_NotOverShortCircuitExpression_PreservesShortCircuitSideEffects()
    {
        const string code = "not (Tests.BooleanSideEffectProbe.FFalse() and Tests.BooleanSideEffectProbe.FTrue())";

        var result = ExecuteCode<bool>(code);

        Assert.That(result, Is.True);
        Assert.That(BooleanSideEffectProbe.FalseCalls, Is.EqualTo(CoresCount));
        Assert.That(BooleanSideEffectProbe.TrueCalls, Is.EqualTo(0));
    }

    [Test]
    public void Execute_ConstantShortCircuitExpressions_DoNotEvaluateSkippedBranches()
    {
        var leftTrueOr = ExecuteCode<bool>("true or Tests.BooleanSideEffectProbe.FFalse()");
        Assert.That(leftTrueOr, Is.True);
        Assert.That(BooleanSideEffectProbe.FalseCalls, Is.EqualTo(0));
        Assert.That(BooleanSideEffectProbe.TrueCalls, Is.EqualTo(0));

        BooleanSideEffectProbe.Reset();

        var leftFalseAnd = ExecuteCode<bool>("false and Tests.BooleanSideEffectProbe.FTrue()");
        Assert.That(leftFalseAnd, Is.False);
        Assert.That(BooleanSideEffectProbe.FalseCalls, Is.EqualTo(0));
        Assert.That(BooleanSideEffectProbe.TrueCalls, Is.EqualTo(0));
    }

    [Test]
    public void Execute_DeepAndChain_StopsAtFirstFalseWithSideEffects()
    {
        const string code = "Tests.BooleanSideEffectProbe.FTrue() and Tests.BooleanSideEffectProbe.FFalse() and Tests.BooleanSideEffectProbe.FTrue()";

        var result = ExecuteCode<bool>(code);

        Assert.That(result, Is.False);
        Assert.That(BooleanSideEffectProbe.TrueCalls, Is.EqualTo(CoresCount));
        Assert.That(BooleanSideEffectProbe.FalseCalls, Is.EqualTo(CoresCount));
    }

    [Test]
    public void Execute_DeepOrChain_StopsAtFirstTrueWithSideEffects()
    {
        const string code = "Tests.BooleanSideEffectProbe.FFalse() or Tests.BooleanSideEffectProbe.FTrue() or Tests.BooleanSideEffectProbe.FFalse()";

        var result = ExecuteCode<bool>(code);

        Assert.That(result, Is.True);
        Assert.That(BooleanSideEffectProbe.FalseCalls, Is.EqualTo(CoresCount));
        Assert.That(BooleanSideEffectProbe.TrueCalls, Is.EqualTo(CoresCount));
    }

    [Test]
    public void Execute_ComplexBooleanExpression_MatchesCSharpSemanticsOnRandomInputs()
    {
        var random = new Random(12345);

        for (var i = 0; i < 50; i++)
        {
            var x = random.Next(-10, 11);
            var y = random.Next(-10, 11);
            var z = random.Next(-10, 11);

            var code = $"((({x} > {y}) and ({y} > {z})) or (not ({x} > {z}))) and (({x} > 0) or ({z} > 0))";
            var wistResult = ExecuteCode<bool>(code);
            var expected = (x > y && y > z || !(x > z)) && (x > 0 || z > 0);

            Assert.That(wistResult, Is.EqualTo(expected), $"Mismatch for x={x}, y={y}, z={z}");
        }
    }

    [Test]
    public void Execute_DeepBooleanChains_EvaluateCorrectlyWithoutStackIssues()
    {
        const string andCode = "(1 < 2) and (2 < 3) and (3 < 4) and (4 < 5) and (5 < 6)";
        const string orCode = "(1 > 2) or (2 > 3) or (3 < 4) or (4 > 5) or (5 > 6)";

        var andResult = ExecuteCode<bool>(andCode);
        var orResult = ExecuteCode<bool>(orCode);

        Assert.That(andResult, Is.True);
        Assert.That(orResult, Is.True);
    }
}

public static class BooleanSideEffectProbe
{
    public static int TrueCalls { get; private set; }
    public static int FalseCalls { get; private set; }

    public static bool FTrue()
    {
        TrueCalls++;
        return true;
    }

    public static bool FFalse()
    {
        FalseCalls++;
        return false;
    }

    public static void Reset()
    {
        TrueCalls = 0;
        FalseCalls = 0;
    }
}