using Tests.Infrastructure;

namespace Tests.Backends;

[TestFixture]
public class RuntimeCompiledArtifactBackendSpecificTests
{
    [Test]
    public void GetBackendSpecificArtifactCompiler_WithCilCompilationOutput_ReturnsWorkingCompilerArtifactPath()
    {
        using var host = RuntimeCompiledArtifactTestFactory.CreateHost();
        var compiler = host.GetBackendSpecificArtifactCompiler<CilCompilationOutput>("compiler");
        var artifact = compiler.Compile("1", new OrderedDictionary<string, Type>());
        var session = artifact.CreateSession();

        Assert.Multiple(() =>
        {
            Assert.That(artifact.CompilationOutput, Is.Not.Null);
            Assert.That(artifact.CompilationOutput.Method, Is.Not.Null);
            Assert.That(artifact.SlotsByName, Is.Empty);
            Assert.That(session, Is.Not.Null);
        });
    }

    [Test]
    public void GetBackendSpecificArtifactCompiler_WithMismatchedCompilationOutput_ThrowsInvalidOperationException()
    {
        using var host = RuntimeCompiledArtifactTestFactory.CreateHost();

        Assert.Throws<InvalidOperationException>(() => host.GetBackendSpecificArtifactCompiler<IAbstractIR>("compiler"));
    }
}
