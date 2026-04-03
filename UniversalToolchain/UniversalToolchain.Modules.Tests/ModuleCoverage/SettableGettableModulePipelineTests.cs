namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class SettableGettableModulePipelineTests
{
    [Test]
    public void SettableGettable_SetThenGet_ReturnsStoredValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("let x=2; x=5; x", ModulePipelineTestHelper.FullUniversalModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void SettableGettable_MultipleSetOperations_LastWriteWins()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("let x=1; x=2; x=3; x", ModulePipelineTestHelper.FullUniversalModules);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(3));
    }

    [Test]
    public void SettableGettable_GetBeforeSet_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertCompilerAndInterpreterFailSameWay("x", ModulePipelineTestHelper.FullUniversalModules);
    }

    [Test]
    public void SettableGettable_SetOnUnsupportedTarget_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("(2+3)=7", ModulePipelineTestHelper.FullUniversalModules, string.Empty);
    }

    [Test]
    public void SettableGettable_ModuleDisabled_SameProgramFailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        var composition = h.Compose(ModulePipelineTestHelper.FullUniversalModules.Concat(["SettableGettable"]));
        Assert.That(composition.IsSuccess, Is.False);
    }
}