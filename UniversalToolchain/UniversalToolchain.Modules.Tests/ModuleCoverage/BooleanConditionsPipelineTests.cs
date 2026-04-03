namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class BooleanConditionsPipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [TestCase("true", true)]
    [TestCase("false", false)]
    public void BooleanConditions_Literals_ReturnExpectedValue(string code, bool expected)
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(code, Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(r.Compiler), Is.EqualTo(expected));
    }

    [TestCase("not true", false)]
    [TestCase("not false", true)]
    public void BooleanConditions_UnaryNot_ReturnsExpectedValue(string code, bool expected)
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(code, Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(r.Compiler), Is.EqualTo(expected));
    }

    [TestCase("true and true", true)]
    [TestCase("true and false", false)]
    [TestCase("false and true", false)]
    [TestCase("false and false", false)]
    [TestCase("true or true", true)]
    [TestCase("true or false", true)]
    [TestCase("false or true", true)]
    [TestCase("false or false", false)]
    public void BooleanConditions_BinaryAndOr_ReturnExpectedValue(string code, bool expected)
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth(code, Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(r.Compiler), Is.EqualTo(expected));
    }

    [Test]
    public void BooleanConditions_NestedWithComparison_ReturnsExpectedValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("(2 < 3 and 1 < 2) or false", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(r.Compiler), Is.True);
    }

    [Test]
    public void BooleanConditions_AndShortCircuit_DoesNotEvaluateRightHandSide()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("false and UniversalToolchain.Modules.Tests.ModuleCoverage.BooleanConditionsPipelineTests.Dangerous()", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(r.Compiler), Is.False);
    }

    [Test]
    public void BooleanConditions_OrShortCircuit_DoesNotEvaluateRightHandSide()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("true or UniversalToolchain.Modules.Tests.ModuleCoverage.BooleanConditionsPipelineTests.Dangerous()", Modules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsBool(r.Compiler), Is.True);
    }

    [Test]
    public void BooleanConditions_InvalidRightOperand_FailsCompilerAndInterpreterSameWay()
    {
        using var h = new ModulePipelineTestHelper();

        var compilerException = Assert.Catch(() => h.ExecuteCompiler("true and 5", Modules));
        var interpreterException = Assert.Catch(() => h.ExecuteInterpreter("true and 5", Modules));

        Assert.That(compilerException, Is.Not.Null);
        Assert.That(interpreterException, Is.Not.Null);

        var compilerMessage = compilerException!.Message.ToLowerInvariant();
        var interpreterMessage = interpreterException!.Message.ToLowerInvariant();

        Assert.That(compilerMessage, Is.Not.Empty);
        Assert.That(interpreterMessage, Is.Not.Empty);
        Assert.That(compilerMessage.Contains("bool") || compilerMessage.Contains("boolean") || compilerMessage.Contains("type") || compilerMessage.Contains("valid"), Is.True);
        Assert.That(interpreterMessage.Contains("bool") || interpreterMessage.Contains("boolean") || interpreterMessage.Contains("type") || interpreterMessage.Contains("valid"), Is.True);
    }

    public static bool Dangerous() => throw new InvalidOperationException("This call must be short-circuited.");
}