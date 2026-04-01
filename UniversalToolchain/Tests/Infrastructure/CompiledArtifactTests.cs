namespace Tests.Infrastructure;

[TestFixture]
public class CompiledArtifactTests
{
    [Test]
    public void Constructor_ShouldCopyDeclaredBindingsSnapshot()
    {
        var sourceBindings = new List<ExternalBinding>
        {
            new() { Name = "x", Type = typeof(int), Value = 10, Kind = ExternalBindingKind.Variable }
        };

        var artifact = new CompiledArtifact<string>("x", sourceBindings, "compiled");
        sourceBindings[0] = new ExternalBinding { Name = "changed", Type = typeof(string), Value = "bad", Kind = ExternalBindingKind.Constant };

        Assert.That(artifact.DeclaredBindings, Has.Count.EqualTo(1));
        Assert.That(artifact.DeclaredBindings[0].Name, Is.EqualTo("x"));
        Assert.That(artifact.DeclaredBindings[0].Type, Is.EqualTo(typeof(int)));
        Assert.That(artifact.DeclaredBindings[0].Value, Is.EqualTo(10));
    }

    [Test]
    public void Constructor_ShouldUseOrdinalSlotsAndAllowCaseDistinctNames()
    {
        var bindings = new List<ExternalBinding>
        {
            new() { Name = "x", Type = typeof(int), Value = 1, Kind = ExternalBindingKind.Variable },
            new() { Name = "X", Type = typeof(int), Value = 2, Kind = ExternalBindingKind.Variable }
        };

        var artifact = new CompiledArtifact<int>("x + X", bindings, 123);

        Assert.That(artifact.SlotsByName["x"], Is.EqualTo(0));
        Assert.That(artifact.SlotsByName["X"], Is.EqualTo(1));
    }

    [Test]
    public void CreateSession_ShouldCreateIndependentExecutionEnvironmentWithDeclaredValues()
    {
        var bindings = new List<ExternalBinding>
        {
            new() { Name = "x", Type = typeof(int), Value = 5, Kind = ExternalBindingKind.Variable }
        };

        var artifact = new CompiledArtifact<string>("x", bindings, "compiled");

        var first = artifact.CreateSession();
        var second = artifact.CreateSession();
        first.SetExternalValue(0, 42);

        Assert.That(first.GetExternalValue(0), Is.EqualTo(42));
        Assert.That(second.GetExternalValue(0), Is.EqualTo(5));
    }

    [Test]
    public void Constructor_WithDuplicateBindingName_ShouldThrow()
    {
        var bindings = new List<ExternalBinding>
        {
            new() { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable },
            new() { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable }
        };

        Assert.Throws<ArgumentException>(() => new CompiledArtifact<string>("x", bindings, "compiled"));
    }
}
