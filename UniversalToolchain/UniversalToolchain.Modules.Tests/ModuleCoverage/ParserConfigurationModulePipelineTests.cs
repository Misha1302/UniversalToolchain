using UniversalToolchain.Wist;

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
    public void DialectComposition_WithUnresolvedLegacyModuleAlias_FailsClosed()
    {
        using var h = new ModulePipelineTestHelper();
        var dialect = h.BuildDialectText(
            "UnknownParserConfiguration",
            ModulePipelineTestHelper.FullUniversalModules.Concat(["ParserConfiguration"]),
            backends: ["interpreter"]);
        var options = WistEngineOptions.FromDialectText(dialect, "parser-configuration-legacy-alias-test");
        options.BackendId = "interpreter";
        options.AllowedAssemblies = [typeof(int).Assembly, typeof(ParserConfigurationModulePipelineTests).Assembly];

        var exception = Assert.Catch(() => WistEngine.Create(options));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.ToString(), Does.Contain("ParserConfiguration"));
    }

    [Test]
    public void DialectComposition_WithAliasAccessibleModules_PreservesBackendParity()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("let x=2; x+3", ModulePipelineTestHelper.FullUniversalModules);
        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
    }
}
