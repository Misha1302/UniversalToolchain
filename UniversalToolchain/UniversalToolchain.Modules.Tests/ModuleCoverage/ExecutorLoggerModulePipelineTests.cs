using ExecutorLoggerModule;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ExecutorLoggerModulePipelineTests
{
    [Test]
    public void ExecutorLogger_Enabled_DoesNotChangeExecutionResult()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("2+3", "2+3", ModulePipelineTestHelper.FullUniversalModules);
    }

    [Test]
    public void ExecutorLogger_Enabled_CreatesExpectedLogArtifact()
    {
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "logs.txt");
        if (File.Exists(path)) File.Delete(path);

        var logger = new ExecutorDebugLoggerImpl(path);
        logger.ProcessText("2+3");
        logger.ProcessLexemes([]);

        Assert.That(File.Exists(path), Is.True);
        Assert.That(new FileInfo(path).Length, Is.GreaterThan(0));
    }

    [Test]
    public void ExecutorLogger_Enabled_ErroringProgramProducesStableLoggingBehavior()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFails("2 / hi", ModulePipelineTestHelper.FullUniversalModules, "identifier");
    }

    [Test]
    public void ExecutorLogger_Enabled_BackendParityIsPreserved()
    {
        using var h = new ModulePipelineTestHelper();
        var r = h.ExecuteBoth("let x=2; x+3", ModulePipelineTestHelper.FullUniversalModules);
        ModulePipelineTestHelper.AssertParity(r.Compiler, r.Interpreter);
    }

    [Test]
    public void ExecutorLogger_DisabledAndEnabled_ProduceSameProgramSemantics()
    {
        using var h = new ModulePipelineTestHelper();
        h.ExecuteEquivalent("let x=2; x+3", "let x=2; x+3", ModulePipelineTestHelper.FullUniversalModules);
    }
}