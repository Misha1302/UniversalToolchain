namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class NativeMathModulePipelineTests
{
    private static readonly string[] NativeModules =
        ModulePipelineTestHelper.FullUniversalModules
            .Where(x => x is not ("Numbers" or "Arithmetic"))
            .Concat(["NativeTypes"])
            .ToArray();

    private static readonly string[] UniversalModules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void NativeMath_SimpleAddition_ReturnsExpectedValueOnBothBackends()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 + 3", NativeModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void NativeMath_PrecedenceMatchesExpectedArithmeticRules()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 + 3 * 4", NativeModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(14));
    }

    [Test]
    public void NativeMath_LongArithmeticChain_HasBackendParity()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("1 + 2 * 3 - 4 + 5 * 6 - 7", NativeModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
    }

    [Test]
    public void NativeMath_BasicSubtraction_ProducesExpectedValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("3 - 1", NativeModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(2));
    }

    [Test]
    public void NativeMath_DivisionByZero_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        try
        {
            var r = h.ExecuteBoth("2/0", NativeModules);
            ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        }
        catch (Exception ex)
        {
            Assert.That(ex.Message, Is.Not.Empty);
        }
    }

    [TestCase("7 + 8")]
    [TestCase("12 - 5 + 3")]
    [TestCase("2 * (3 + 4)")]
    public void NativeMath_UniversalAndNativeProfiles_AgreeOnSimpleIntegerScenarios(string code)
    {
        using var h = new ModulePipelineTestHelper();

        var universalComposition = h.Compose(UniversalModules, backends: ["compiler", "interpreter"]);
        Assert.That(
            universalComposition.IsSuccess,
            Is.True,
            "Universal profile composition failed: " + string.Join("\n", universalComposition.SemanticDiagnostics.Concat(universalComposition.ResolutionDiagnostics).Select(static d => d.Message)));

        var nativeComposition = h.Compose(NativeModules, backends: ["compiler", "interpreter"]);
        Assert.That(
            nativeComposition.IsSuccess,
            Is.True,
            "Native profile composition failed: " + string.Join("\n", nativeComposition.SemanticDiagnostics.Concat(nativeComposition.ResolutionDiagnostics).Select(static d => d.Message)));

        var universalResult = h.ExecuteBoth(code, UniversalModules);
        var nativeResult = h.ExecuteBoth(code, NativeModules);

        ModulePipelineTestHelper.AssertParity(universalResult.Compiler, universalResult.Interpreter);
        ModulePipelineTestHelper.AssertParity(nativeResult.Compiler, nativeResult.Interpreter);

        Assert.That(ModulePipelineTestHelper.AsNumber(universalResult.Compiler), Is.EqualTo(ModulePipelineTestHelper.AsNumber(nativeResult.Compiler)).Within(1e-9));
    }
}
