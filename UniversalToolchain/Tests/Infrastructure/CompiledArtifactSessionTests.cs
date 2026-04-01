namespace Tests.Infrastructure;

[TestFixture]
public class CompiledArtifactSessionTests
{
    [Test]
    public void SetArgument_ShouldThrow_WhenSlotIsOutOfRange()
    {
        var session = CreateSession(out _);

        Assert.Throws<ArgumentOutOfRangeException>(() => session.SetArgument(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.SetArgument(2, 1));
    }

    [Test]
    public void SetArgument_ShouldThrow_WhenValueIsNotAssignable()
    {
        var session = CreateSession(out _);

        Assert.Throws<ArgumentException>(() => session.SetArgument(0, "bad"));
    }

    [Test]
    public void SetArgument_ShouldThrow_WhenNullAssignedToNonNullableValueType()
    {
        var session = CreateSession(out _);

        Assert.Throws<ArgumentException>(() => session.SetArgument(0, null));
    }

    [Test]
    public void SetArgument_ByName_ShouldAssignValueToCorrespondingSlot()
    {
        var session = CreateSession(out var environment);

        session.SetArgument("value", 7);

        Assert.That(environment.GetExternalValue(0), Is.EqualTo(7));
    }

    [Test]
    public void Run_ShouldExecuteViaExecutor()
    {
        var session = CreateSession(out _);

        session.SetArgument(0, 11);
        session.SetArgument(1, "abc");

        var result = session.Run();

        Assert.That(result, Is.EqualTo("compiled:11:abc"));
    }

    [Test]
    public void RunGeneric_ShouldReturnTypedResult()
    {
        var session = CreateSession(out _);

        session.SetArgument(0, 5);
        session.SetArgument(1, "x");

        var result = session.Run<string>();

        Assert.That(result, Is.EqualTo("compiled:5:x"));
    }

    [Test]
    public void Invoke_ShouldAssignByPositionAndRun()
    {
        var session = CreateSession(out _);

        var result = session.Invoke<string, string>(3, "n");

        Assert.That(result, Is.EqualTo("compiled:3:n"));
    }

    [Test]
    public void Invoke_ShouldWork_ThroughInterfaceReference()
    {
        ICompiledArtifactSession session = CreateSession(out _);

        var result = session.Invoke<string>(3, "n");

        Assert.That(result, Is.EqualTo("compiled:3:n"));
    }

    [Test]
    public void InvokeNamed_ShouldAssignByNameAndRun()
    {
        var session = CreateSession(out _);

        var result = session.InvokeNamed<string, string>(new Dictionary<string, object?>
        {
            ["text"] = "q",
            ["value"] = 9
        });

        Assert.That(result, Is.EqualTo("compiled:9:q"));
    }

    [Test]
    public void InvokeNamed_ShouldWork_ThroughInterfaceReference()
    {
        ICompiledArtifactSession session = CreateSession(out _);

        var result = session.InvokeNamed<string>(new Dictionary<string, object?>
        {
            ["text"] = "q",
            ["value"] = 9
        });

        Assert.That(result, Is.EqualTo("compiled:9:q"));
    }

    [Test]
    public void Invoke_ShouldThrow_WhenArgumentCountIsWrong()
    {
        var session = CreateSession(out _);

        Assert.Throws<ArgumentException>(() => session.Invoke<string, string>(1));
    }

    [Test]
    public void Invoke_ShouldThrow_WhenArgumentTypeIsWrong()
    {
        var session = CreateSession(out _);

        Assert.Throws<ArgumentException>(() => session.Invoke<string, string>("bad", "ok"));
    }

    [Test]
    public void InvokeNamed_ShouldThrow_WhenMissingRequiredArgument()
    {
        var session = CreateSession(out _);

        Assert.Throws<ArgumentException>(() => session.InvokeNamed<string, string>(new Dictionary<string, object?>
        {
            ["value"] = 9
        }));
    }

    [Test]
    public void InvokeNamed_ShouldThrow_WhenExtraArgumentIsProvided()
    {
        var session = CreateSession(out _);

        Assert.Throws<ArgumentException>(() => session.InvokeNamed<string, string>(new Dictionary<string, object?>
        {
            ["value"] = 9,
            ["text"] = "q",
            ["extra"] = 42
        }));
    }

    [Test]
    public void Session_ShouldBeReusable_WithDifferentArgumentsAcrossRuns()
    {
        var session = CreateSession(out _);

        var first = session.Invoke<string, string>(3, "a");
        var second = session.Invoke<string, string>(8, "b");

        Assert.That(first, Is.EqualTo("compiled:3:a"));
        Assert.That(second, Is.EqualTo("compiled:8:b"));
    }

    private static CompiledArtifactSession<string> CreateSession(out ExecutionEnvironment environment)
    {
        var bindings = new List<ExternalBinding>
        {
            new() { Name = "value", Type = typeof(int) },
            new() { Name = "text", Type = typeof(string) }
        };

        var artifact = new CompiledArtifact<string>("compiled-source", bindings, "compiled", new FakeExecutor());
        environment = new ExecutionEnvironment(bindings);
        return new CompiledArtifactSession<string>(artifact, new FakeExecutor(), environment);
    }

    private sealed class FakeExecutor : IExecutor<string>
    {
        public object? Execute(string compilation, IExecutionEnvironment environment)
        {
            return $"{compilation}:{environment.GetExternalValue(0)}:{environment.GetExternalValue(1)}";
        }
    }
}
