namespace Tests.Infrastructure;

[TestFixture]
public class CompiledArtifactSessionTests
{
    [Test]
    public void SetArgument_ShouldThrow_WhenSlotIsOutOfRange()
    {
        var session = CreateSession();

        Assert.Throws<ArgumentOutOfRangeException>(() => session.SetArgument(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.SetArgument(2, 1));
    }

    [Test]
    public void SetArgument_ShouldThrow_WhenValueIsNotAssignable()
    {
        var session = CreateSession();

        Assert.Throws<ArgumentException>(() => session.SetArgument(0, "bad"));
    }

    [Test]
    public void SetArgument_ShouldThrow_WhenNullAssignedToNonNullableValueType()
    {
        var session = CreateSession();

        Assert.Throws<ArgumentException>(() => session.SetArgument(0, null));
    }

    [Test]
    public void SetArgument_ByName_ShouldAssignValueToCorrespondingSlot()
    {
        var session = CreateSession();

        session.SetArgument("value", 7);

        Assert.That(session.Environment.GetExternalValue(0), Is.EqualTo(7));
    }

    [Test]
    public void Run_ShouldExecuteViaExecutor()
    {
        var session = CreateSession();

        session.SetArgument(0, 11);
        session.SetArgument(1, "abc");

        var result = session.Run();

        Assert.That(result, Is.EqualTo("compiled:11:abc"));
    }

    [Test]
    public void RunGeneric_ShouldReturnTypedResult()
    {
        var session = CreateSession();

        session.SetArgument(0, 5);
        session.SetArgument(1, "x");

        var result = session.Run<string>();

        Assert.That(result, Is.EqualTo("compiled:5:x"));
    }

    [Test]
    public void Invoke_ShouldAssignByPositionAndRun()
    {
        var session = CreateSession();

        var result = session.Invoke<string, string>(3, "n");

        Assert.That(result, Is.EqualTo("compiled:3:n"));
    }

    [Test]
    public void InvokeNamed_ShouldAssignByNameAndRun()
    {
        var session = CreateSession();

        var result = session.InvokeNamed<string, string>(new Dictionary<string, object?>
        {
            ["text"] = "q",
            ["value"] = 9
        });

        Assert.That(result, Is.EqualTo("compiled:9:q"));
    }

    private static SessionContext CreateSession()
    {
        var bindings = new List<ExternalBinding>
        {
            new() { Name = "value", Type = typeof(int) },
            new() { Name = "text", Type = typeof(string) }
        };

        var environment = new ExecutionEnvironment(bindings);
        var executor = new FakeExecutor();
        var session = new CompiledArtifactSession<string>("compiled", executor, environment, bindings);

        return new SessionContext(session, environment);
    }

    private sealed class SessionContext(
        CompiledArtifactSession<string> session,
        ExecutionEnvironment environment)
    {
        public CompiledArtifactSession<string> Session { get; } = session;

        public ExecutionEnvironment Environment { get; } = environment;

        public static implicit operator CompiledArtifactSession<string>(SessionContext context) => context.Session;
    }

    private sealed class FakeExecutor : IExecutor<string>
    {
        public object? Execute(string compilation, IExecutionEnvironment environment)
        {
            return $"{compilation}:{environment.GetExternalValue(0)}:{environment.GetExternalValue(1)}";
        }
    }
}
