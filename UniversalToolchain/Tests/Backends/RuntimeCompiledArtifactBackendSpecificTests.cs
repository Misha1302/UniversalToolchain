using System.Reflection.Emit;
using Tests.Infrastructure;

namespace Tests.Backends;

[TestFixture]
public class RuntimeCompiledArtifactBackendSpecificTests
{
    [Test]
    public void GetBackendSpecificArtifactCompiler_WithDynamicMethodOutput_ReturnsWorkingCompilerArtifactPath()
    {
        using var host = RuntimeCompiledArtifactTestFactory.CreateHost();
        var compiler = host.GetBackendSpecificArtifactCompiler<DynamicMethod>("compiler");
        var artifact = compiler.Compile("x", new OrderedDictionary<string, Type> { ["x"] = typeof(object) });
        var session = artifact.CreateSession();

        Assert.Multiple(() =>
        {
            Assert.That(artifact.CompilationOutput, Is.Not.Null);
            Assert.That(artifact.SlotsByName.ContainsKey("x"), Is.True);
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