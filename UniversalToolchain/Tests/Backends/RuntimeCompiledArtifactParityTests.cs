using System.Collections.Specialized;
using Tests.Infrastructure;

namespace Tests.Backends;

[TestFixture]
public class RuntimeCompiledArtifactParityTests
{
    [Test]
    public void CompilerAndInterpreter_ShouldKeepDeclaredBindingsParity()
    {
        using var host = RuntimeCompiledArtifactTestFactory.CreateHost();
        var declared = new OrderedDictionary<string, Type>
        {
            ["x"] = typeof(object),
            ["y"] = typeof(object)
        };

        var compilerArtifact = ParityBackendExecutionAdapter.CompileSnapshot(host, "compiler", "x", declared);
        var interpreterArtifact = ParityBackendExecutionAdapter.CompileSnapshot(host, "interpreter", "x", declared);

        Assert.That(compilerArtifact.DeclaredBindingNames,
            Is.EqualTo(interpreterArtifact.DeclaredBindingNames));
        Assert.That(compilerArtifact.SlotsByName, Is.EqualTo(interpreterArtifact.SlotsByName));
    }
}
