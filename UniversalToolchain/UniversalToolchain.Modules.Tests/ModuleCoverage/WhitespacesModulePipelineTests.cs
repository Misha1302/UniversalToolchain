namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class WhitespacesModulePipelineTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Whitespaces_NoSpacesAroundBinaryOperator_IsEquivalentToSpacedVersion()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("2+3", "2 + 3", _modules);
    }

    [Test]
    public void Whitespaces_MultipleSpacesBetweenTokens_AreIgnored()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("let   x   =   2; x + 3", "let x = 2; x + 3", _modules);
    }

    [Test]
    public void Whitespaces_TabsAndSpacesMixed_AreHandledDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("let\tx\t=\t2;\tx +\t3", "let x = 2; x + 3", _modules);
    }

    [Test]
    public void Whitespaces_LeadingAndTrailingWhitespace_DoNotChangeResult()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("  \n\t let x = 2; x + 3 \n\t", "let x = 2; x + 3", _modules);
    }

    [Test]
    public void Whitespaces_OmittedExplicitly_AreRestoredBySelectedFeatureDependencies()
    {
        using var h = new ModulePipelineTestHelper();
        var withoutExplicitWhitespaces = _modules.Where(static module => module != "Whitespaces").ToArray();

        var result = h.ExecuteBoth("let x = 2; x + 3", withoutExplicitWhitespaces);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(5));
    }
}
