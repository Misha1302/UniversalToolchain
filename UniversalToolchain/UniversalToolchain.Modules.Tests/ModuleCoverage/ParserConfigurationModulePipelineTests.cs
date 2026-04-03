namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ParserConfigurationModulePipelineTests
{
    [Test]
    public void DialectComposition_WithAliasAccessibleModules_ComposesAndExecutesProgram()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("2+3", ModulePipelineTestHelper.FullUniversalModules);
        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void DialectComposition_WithUnresolvedModuleAlias_ReportsExpectedCompositionDiagnostic()
    {
        using var h = new ModulePipelineTestHelper();
        var composition = h.Compose(ModulePipelineTestHelper.FullUniversalModules.Concat(["ParserConfiguration"]));

        Assert.That(composition.IsSuccess, Is.False);

        var diagnostics = composition.SemanticDiagnostics
            .Concat(composition.ResolutionDiagnostics)
            .Select(static d => d.Message)
            .ToArray();

        Assert.That(diagnostics, Is.Not.Empty);
        Assert.That(
            diagnostics.Any(static message => message.Contains("module descriptor", StringComparison.OrdinalIgnoreCase)),
            Is.True);
        Assert.That(
            diagnostics.Any(static message => message.Contains("not registered", StringComparison.OrdinalIgnoreCase)),
            Is.True);
        Assert.That(diagnostics.Any(static message => message.Contains("ParserConfiguration", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void DialectComposition_WithAliasAccessibleModules_PreservesBackendParity()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("let x=2; x+3", ModulePipelineTestHelper.FullUniversalModules);
        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
    }
}