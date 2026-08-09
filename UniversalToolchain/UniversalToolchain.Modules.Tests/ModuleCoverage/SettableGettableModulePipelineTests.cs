using UniversalToolchain.Wist;

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
    public void SettableGettable_LegacyAlias_IsRejectedByCanonicalDsl()
    {
        using var h = new ModulePipelineTestHelper();
        var dialect = h.BuildDialectText(
            "LegacySettableGettable",
            ModulePipelineTestHelper.FullUniversalModules.Concat(["SettableGettable"]),
            backends: ["interpreter"]);
        var options = WistEngineOptions.FromDialectText(dialect, "legacy-settable-gettable-test");
        options.BackendId = "interpreter";
        options.AllowedAssemblies = [typeof(int).Assembly, typeof(SettableGettableModulePipelineTests).Assembly];

        var exception = Assert.Catch(() => WistEngine.Create(options));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ToString(), Does.Contain("SettableGettable"));
    }
}
