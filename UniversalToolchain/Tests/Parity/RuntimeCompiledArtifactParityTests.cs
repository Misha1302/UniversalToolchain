using System.Collections.Specialized;
using System.Reflection.Emit;
using Tests.Infrastructure;

namespace Tests.Parity;

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

        ICompiledArtifact<DynamicMethod> compilerArtifact = RuntimeCompiledArtifactTestFactory.GetCompilerCore(host).Compile("x", declared);
        ICompiledArtifact<IAbstractIR> interpreterArtifact = RuntimeCompiledArtifactTestFactory.GetInterpreterCore(host).Compile("x", declared);

        Assert.That(compilerArtifact.DeclaredBindings.Select(static b => b.Name),
            Is.EqualTo(interpreterArtifact.DeclaredBindings.Select(static b => b.Name)));
        Assert.That(compilerArtifact.SlotsByName, Is.EqualTo(interpreterArtifact.SlotsByName));
    }
}
