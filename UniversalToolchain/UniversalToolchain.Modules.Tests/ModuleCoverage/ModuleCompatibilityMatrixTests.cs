namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ModuleCompatibilityMatrixTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void CoreExecutionProfiles_CompileAndRunAcrossModuleMatrix()
    {
        using var helper = new ModulePipelineTestHelper();

        var scenarios = new[]
        {
            "1 + 2 * 3",
            "let x = 3; if x > 1 x + 10 else x",
            "let s = 0; for (let i = 1) (i <= 3) (i = i + 1) (s = s + i); s",
            "let a = 1; goto @end; a = 10; @end: a"
        };

        foreach (var scenario in scenarios)
        {
            var result = helper.ExecuteBoth(scenario, _modules);
            ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        }
    }
}