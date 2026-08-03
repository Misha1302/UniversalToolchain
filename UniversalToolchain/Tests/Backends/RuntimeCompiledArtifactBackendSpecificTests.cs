using System.Runtime.Loader;
using Tests.Infrastructure;

namespace Tests.Backends;

[TestFixture]
public class RuntimeCompiledArtifactBackendSpecificTests
{
    [Test]
    public void Compile_WithImplementationOwnedCilOutput_UsesUntypedContractAndKeepsOutputIsolated()
    {
        using var host = RuntimeCompiledArtifactTestFactory.CreateHost();
        var artifact = host.Compile("1", new OrderedDictionary<string, Type>(), "cil");
        var session = artifact.CreateSession();
        var output = BackendArtifactIntrospection.GetCompilationOutput(artifact);

        Assert.Multiple(() =>
        {
            Assert.That(output, Is.Not.Null);
            Assert.That(BackendArtifactIntrospection.GetDynamicMethod(artifact), Is.Not.Null);
            Assert.That(BackendArtifactIntrospection.GetOutputLoadContextName(artifact),
                Is.EqualTo("UniversalToolchain.Runtime.Isolated"));
            Assert.That(artifact.SlotsByName, Is.Empty);
            Assert.That(session, Is.Not.Null);
            Assert.Throws<InvalidOperationException>(() =>
                host.GetBackendSpecificArtifactCompiler<CilCompilationOutput>("cil"));
        });
    }

    [Test]
    public void GetBackendSpecificArtifactCompiler_WithMismatchedCompilationOutput_ThrowsInvalidOperationException()
    {
        using var host = RuntimeCompiledArtifactTestFactory.CreateHost();

        Assert.Throws<InvalidOperationException>(() => host.GetBackendSpecificArtifactCompiler<IAbstractIR>("cil"));
    }
}
