namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ExecutionSemanticParityTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [TestCase("2 + 2")]
    [TestCase("let x = 2; x * 3")]
    [TestCase("let i = 0; for (let j = 0) (j < 3) (j = j + 1) (i = i + 1); i")]
    [TestCase("let x = 4; if x == 4 1 else 0")]
    public void CompilerAndInterpreter_StaySemanticallyAligned(string code)
    {
        using var helper = new ModulePipelineTestHelper();
        var result = helper.ExecuteBoth(code, Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
    }
}