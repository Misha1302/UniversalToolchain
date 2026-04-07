namespace Tests.Legacy;

[TestFixture]
public class LegacyTestBaseContractsTests : LegacyTestBase
{
    override protected IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICoreRunnable>(new FixedResultCore("string-result"));
        var provider = services.BuildServiceProvider();
        typeof(LegacyTestBase).GetField("_serviceProvider", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(this, provider);
        return provider;
    }

    [Test]
    public void ExecuteCodeGeneric_WhenResultCannotBeConverted_ShouldThrowInvalidCastException()
    {
        var ex = Assert.Throws<InvalidCastException>(() => ExecuteCode<int>("ignored"));

        Assert.That(ex, Is.TypeOf<InvalidCastException>());
        Assert.That(ex!.Message, Does.Contain("Cannot convert test result from type"));
    }

    private sealed class FixedResultCore(object result) : ICoreRunnable
    {
        public object? Run(string code, Dictionary<string, object>? parameters = null) => result;

        public void PrepareToRun(string code, OrderedDictionary<string, Type>? parameters = null)
        {
        }

        public object? RunPrepared() => result;

        public void PrepareToRun(CompilationInput input)
        {
        }
    }
}
