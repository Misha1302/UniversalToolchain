namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class SyntaxSurfaceModuleTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void NumbersCommentsAndNewLineSemantics_WorkTogetherDeterministically()
    {
        using var helper = new ModulePipelineTestHelper();
        var result = helper.ExecuteBoth("let x = 2 // inline\n/* block */\nx + 3", _modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(5));
    }
}