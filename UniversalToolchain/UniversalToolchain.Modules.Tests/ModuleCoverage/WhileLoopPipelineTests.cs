namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class WhileLoopPipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    private static readonly string[] StableErrorFragments =
    [
        "Tree is invalid",
        "Assertion failed",
        "Invalid token",
        "Index was out of range",
        "violates the constraint"
    ];

    [Test]
    public void While_SimpleCounterAndSum_ProducesExpectedNumber()
    {
        using var helper = new ModulePipelineTestHelper();
        var result = helper.ExecuteBoth(
            """
            let sum = 0
            let i = 1

            while (i <= 5) (
                sum = sum + i
                i = i + 1
            )

            sum
            """,
            Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(15));
    }

    [Test]
    public void While_ZeroIterations_LeavesStateUnchanged()
    {
        using var helper = new ModulePipelineTestHelper();
        var result = helper.ExecuteBoth(
            """
            let marker = 17
            let i = 10

            while (i < 10) (
                marker = marker + 100
                i = i + 1
            )

            marker
            """,
            Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(17));
    }

    [Test]
    public void While_NestedLoops_ProduceExpectedAggregate()
    {
        using var helper = new ModulePipelineTestHelper();
        var result = helper.ExecuteBoth(
            """
            let total = 0
            let outer = 1

            while (outer <= 3) (
                let inner = 1
                while (inner <= 2) (
                    total = total + (outer * inner)
                    inner = inner + 1
                )
                outer = outer + 1
            )

            total
            """,
            Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(18));
    }

    [Test]
    public void While_ConditionBasedOnComparisonModules_BehavesDeterministically()
    {
        using var helper = new ModulePipelineTestHelper();
        var result = helper.ExecuteBoth(
            """
            let i = 1
            let product = 1

            while (i <= 4) (
                product = product * i
                i = i + 1
            )

            product
            """,
            Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(24));
    }

    [Test]
    public void While_MutatingLoopVariable_StopsAtExactBoundary()
    {
        using var helper = new ModulePipelineTestHelper();
        var result = helper.ExecuteBoth(
            """
            let i = 0

            while (i < 7) (
                i = i + 2
            )

            i
            """,
            Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(8));
    }

    [Test]
    public void While_MalformedCase_ProducesDeterministicError()
    {
        using var helper = new ModulePipelineTestHelper();

        var compilerException = Assert.Throws(Is.InstanceOf<Exception>(), () =>
            helper.ExecuteCompiler(
                """
                let i = 0
                while
                i
                """,
                Modules));

        var interpreterException = Assert.Throws(Is.InstanceOf<Exception>(), () =>
            helper.ExecuteInterpreter(
                """
                let i = 0
                while
                i
                """,
                Modules));

        Assert.That(compilerException, Is.Not.Null);
        Assert.That(interpreterException, Is.Not.Null);

        var compilerFragment = ExtractStableErrorFragment(compilerException!.Message);
        var interpreterFragment = ExtractStableErrorFragment(interpreterException!.Message);

        Assert.That(compilerFragment, Is.Not.Null);
        Assert.That(interpreterFragment, Is.EqualTo(compilerFragment));
    }

    [Test]
    public void While_ParityBetweenCompilerAndInterpreter_OnSameScenario()
    {
        using var helper = new ModulePipelineTestHelper();
        var result = helper.ExecuteBoth(
            """
            let sum = 0
            let i = 0

            while (i < 6) (
                sum = sum + i
                i = i + 1
            )

            sum
            """,
            Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
    }

    private static string? ExtractStableErrorFragment(string message)
        => StableErrorFragments.FirstOrDefault(message.Contains);
}