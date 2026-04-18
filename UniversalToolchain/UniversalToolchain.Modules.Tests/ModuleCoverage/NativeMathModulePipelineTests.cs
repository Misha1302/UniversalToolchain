namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class NativeMathModulePipelineTests
{
    private static readonly string[] _nativeModules =
        ModulePipelineTestHelper.FullUniversalModules
            .Where(x => x is not ("Numbers" or "Arithmetic"))
            .Concat(["NativeTypes"])
            .ToArray();

    private static readonly string[] _universalModules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void NativeMath_SimpleAddition_ReturnsExpectedValueOnBothBackends()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 + 3", _nativeModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void NativeMath_PrecedenceMatchesExpectedArithmeticRules()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("2 + 3 * 4", _nativeModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(14));
    }

    [Test]
    public void NativeMath_LongArithmeticChain_HasBackendParity()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("1 + 2 * 3 - 4 + 5 * 6 - 7", _nativeModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
    }

    [Test]
    public void NativeMath_BasicSubtraction_ProducesExpectedValue()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("3 - 1", _nativeModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(r.Compiler), Is.EqualTo(2));
    }

    [Test]
    public void NativeMath_DivisionByZero_IsHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        try
        {
            var r = h.ExecuteBoth("2/0", _nativeModules);
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

        var universalComposition = h.Compose(_universalModules, backends: ["compiler", "interpreter"]);
        Assert.That(
            universalComposition.IsSuccess,
            Is.True,
            "Universal profile composition failed: " + string.Join("\n", universalComposition.SemanticDiagnostics.Concat(universalComposition.ResolutionDiagnostics).Select(static d => d.Message)));

        var nativeComposition = h.Compose(_nativeModules, backends: ["compiler", "interpreter"]);
        Assert.That(
            nativeComposition.IsSuccess,
            Is.True,
            "Native profile composition failed: " + string.Join("\n", nativeComposition.SemanticDiagnostics.Concat(nativeComposition.ResolutionDiagnostics).Select(static d => d.Message)));

        var universalResult = h.ExecuteBoth(code, _universalModules);
        var nativeResult = h.ExecuteBoth(code, _nativeModules);

        ModulePipelineTestHelper.AssertParity(universalResult.Compiler, universalResult.Interpreter);
        ModulePipelineTestHelper.AssertParity(nativeResult.Compiler, nativeResult.Interpreter);

        Assert.That(ModulePipelineTestHelper.AsNumber(universalResult.Compiler), Is.EqualTo(ModulePipelineTestHelper.AsNumber(nativeResult.Compiler)).Within(1e-9));
    }
}