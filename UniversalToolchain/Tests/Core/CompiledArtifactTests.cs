namespace Tests.Core;

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

        var artifact = new CompiledArtifact<string>("x", sourceBindings, "compiled", new NoOpExecutor<string>());
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

        var artifact = new CompiledArtifact<int>("x + X", bindings, 123, new NoOpExecutor<int>());

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

        var artifact = new CompiledArtifact<string>("x", bindings, "compiled", new SlotZeroExecutor<string>());

        var first = artifact.CreateSession();
        var second = artifact.CreateSession();
        first.SetArgument(0, 42);
        second.SetArgument(0, 7);

        Assert.That(first.Run(), Is.EqualTo(42));
        Assert.That(second.Run(), Is.EqualTo(7));
    }

    [Test]
    public void CreateSession_ShouldReturnCompiledArtifactSession()
    {
        var artifact = new CompiledArtifact<string>(
            "x",
            [new ExternalBinding { Name = "x", Type = typeof(int), Value = 1, Kind = ExternalBindingKind.Variable }],
            "compiled",
            new NoOpExecutor<string>());

        var session = artifact.CreateSession();

        Assert.That(session, Is.TypeOf<CompiledArtifactSession<string>>());
    }

    [Test]
    public void Constructor_ShouldPreserveReferenceForMutableBindingValue()
    {
        var mutableValue = new List<int> { 1 };
        var bindings = new List<ExternalBinding>
        {
            new() { Name = "x", Type = typeof(List<int>), Value = mutableValue, Kind = ExternalBindingKind.Variable }
        };

        var artifact = new CompiledArtifact<string>("x", bindings, "compiled", new NoOpExecutor<string>());
        mutableValue.Add(2);

        var valueFromArtifact = artifact.DeclaredBindings[0].Value;
        Assert.That(valueFromArtifact, Is.SameAs(mutableValue));
        Assert.That((List<int>)valueFromArtifact!, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Constructor_WithDuplicateBindingName_ShouldThrow()
    {
        var bindings = new List<ExternalBinding>
        {
            new() { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable },
            new() { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable }
        };

        Assert.Throws<ArgumentException>(() => new CompiledArtifact<string>("x", bindings, "compiled", new NoOpExecutor<string>()));
    }

    private sealed class NoOpExecutor<TCompilationOutput> : IExecutor<TCompilationOutput>
    {
        public object? Execute(TCompilationOutput compilation, IExecutionEnvironment environment) => null;
    }

    private sealed class SlotZeroExecutor<TCompilationOutput> : IExecutor<TCompilationOutput>
    {
        public object? Execute(TCompilationOutput compilation, IExecutionEnvironment environment) => environment.GetExternalValue(0);
    }
}
