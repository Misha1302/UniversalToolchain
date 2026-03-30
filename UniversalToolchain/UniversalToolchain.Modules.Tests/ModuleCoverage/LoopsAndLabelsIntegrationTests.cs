namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class LoopsAndLabelsIntegrationTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void LoopAndGotoFlow_ProducesExpectedAccumulator()
    {
        using var helper = new ModulePipelineTestHelper();
        var result = helper.ExecuteBoth("let i = 0; let s = 0; @loop: i = i + 1; s = s + i; if i < 4 goto @loop; s", Modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(10));
    }
}
