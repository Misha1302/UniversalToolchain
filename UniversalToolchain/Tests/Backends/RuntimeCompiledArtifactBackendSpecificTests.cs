using Tests.Infrastructure;

namespace Tests.Backends;

[TestFixture]
public class RuntimeCompiledArtifactBackendSpecificTests
{
    [Test]
    public void Compile_CilArtifact_ExposesTypedCanonicalBackendOutputAndNativeDelegate()
    {
        using var host = CanonicalWistTestHost.CreateFullNative();
        var program = host.Compile("1", new OrderedDictionary<string, Type>(), "cil");
        var artifact = host.GetCilArtifact(program);

        var created = program.Program.TryCreateNativeDelegate(typeof(Func<int>), out var compiledDelegate);

        Assert.Multiple(() =>
        {
            Assert.That(artifact.Compilation, Is.Not.Null);
            Assert.That(artifact.Compilation.Method, Is.Not.Null);
            Assert.That(program.SlotsByName, Is.Empty);
            Assert.That(created, Is.True);
            Assert.That(compiledDelegate, Is.TypeOf<Func<int>>());
            Assert.That(((Func<int>)compiledDelegate!)(), Is.EqualTo(1));
        });
    }

    [Test]
    public void GetCilArtifact_ForInterpreterBuild_FailsClosed()
    {
        using var host = RuntimeCompiledArtifactTestFactory.CreateHost();
        var program = host.Compile("1", new OrderedDictionary<string, Type>(), "interpreter");

        var exception = Assert.Throws<InvalidOperationException>(() => host.GetCilArtifact(program));

        Assert.That(exception!.Message, Does.Contain("not a CIL artifact"));
    }
}
